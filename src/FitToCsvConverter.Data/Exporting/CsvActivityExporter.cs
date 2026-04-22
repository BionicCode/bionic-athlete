namespace FitToCsvConverter.Data.Exporting;

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using FitToCsvConverter.Data.Activities;
using FitToCsvConverter.Data.Fields;

/// <summary>
/// Writes decoded activity data to CSV files defined by <see cref="CsvExportRequest"/>.
/// </summary>
/// <remarks>
/// Array-valued FIT fields are written as a single CSV cell by joining element values with <c> | </c> in source order.
/// This keeps one selected FIT field mapped to one CSV column in the current export step.
/// </remarks>
public sealed class CsvActivityExporter : ICsvActivityExporter
{
    private const string ArrayValueSeparator = " | ";
    private const double FeetPerMeter = 3.28083989501312;
    private const double KilometersPerMeter = 0.001;
    private const double KilometersPerHourPerMeterPerSecond = 3.6;
    private const double MilesPerMeter = 0.000621371192237334;
    private const double MilesPerHourPerMeterPerSecond = 2.2369362920544;

    // Garmin FIT SDK 21.195.0 exposes one invalid sentinel per base type through Dynastream.Fit.Fit.BaseType.
    // The exporter mirrors those sentinels here so invalid placeholders are blanked before structured CSV is written.
    private static readonly FrozenDictionary<string, decimal> s_numericInvalidValuesByBaseTypeName =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["enum"] = 255m,
            ["sint8"] = 127m,
            ["uint8"] = 255m,
            ["sint16"] = 32767m,
            ["uint16"] = 65535m,
            ["sint32"] = 2147483647m,
            ["uint32"] = 4294967295m,
            ["uint8z"] = 0m,
            ["uint16z"] = 0m,
            ["uint32z"] = 0m,
            ["byte"] = 255m,
            ["sint64"] = 9223372036854775807m,
            ["uint64"] = 18446744073709551615m,
            ["uint64z"] = 0m,
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public async Task<CsvExportResult> ExportAsync(CsvExportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureStructuredCsvTarget(request.Options.Target);

        ImmutableArray<ExportedArtifact>.Builder exportedArtifacts = ImmutableArray.CreateBuilder<ExportedArtifact>(request.NodeRequests.Length);

        // Respect the request order so callers can keep the generated artifact list stable across export and archive flows.
        foreach (CsvNodeExportRequest nodeRequest in request.NodeRequests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int rowCount = await ExportNodeAsync(
                request.SourceActivity,
                nodeRequest,
                request.Encoding,
                request.Delimiter,
                request.Options,
                cancellationToken).ConfigureAwait(false);
            exportedArtifacts.Add(new ExportedArtifact(nodeRequest.NodeType, nodeRequest.DestinationFilePath, rowCount));
        }

        return new CsvExportResult(exportedArtifacts.ToImmutable());
    }

    private static async Task<int> ExportNodeAsync(
        FitActivity sourceActivity,
        CsvNodeExportRequest nodeRequest,
        Encoding encoding,
        char delimiter,
        FitExportOptions exportOptions,
        CancellationToken cancellationToken)
    {
        string destinationDirectoryPath = Path.GetDirectoryName(nodeRequest.DestinationFilePath)
            ?? throw new InvalidOperationException($"Unable to determine a destination directory for '{nodeRequest.DestinationFilePath}'.");
        _ = Directory.CreateDirectory(destinationDirectoryPath);

        ImmutableArray<FitNode> nodes = EnumerateNodes(sourceActivity, nodeRequest.NodeType).ToImmutableArray();
        ImmutableArray<CsvColumnSelection> orderedColumns = nodeRequest.Columns
            .OrderBy(column => column.Order)
            .ThenBy(column => column.SourceName, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

        FrozenDictionary<FitExportColumnKey, FitField> referenceFieldLookup = nodes
            .SelectMany(node => node.Fields)
            .GroupBy(field => field.Original.ExportColumnKey)
            .ToFrozenDictionary(group => group.Key, group => group.First());
        ImmutableArray<ProjectedColumn> projectedColumns = BuildProjectedColumns(orderedColumns, referenceFieldLookup, exportOptions);

        await using var fileStream = new FileStream(
            nodeRequest.DestinationFilePath,
            new FileStreamOptions
            {
                Access = FileAccess.Write,
                Mode = FileMode.Create,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                Share = FileShare.None
            });
        await using var writer = new StreamWriter(fileStream, encoding);

        await WriteLineAsync(writer, projectedColumns.Select(projectedColumn => projectedColumn.Header), delimiter, cancellationToken).ConfigureAwait(false);

        int rowCount = 0;
        foreach (FitNode node in nodes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyDictionary<FitExportColumnKey, FitField> fieldLookup = node.Fields.ToDictionary(
                field => field.Original.ExportColumnKey,
                field => field);

            IEnumerable<string> cellValues = projectedColumns.Select(projectedColumn =>
                fieldLookup.TryGetValue(projectedColumn.Selection.ColumnKey, out FitField? field)
                    ? FormatFieldValues(field, projectedColumn, exportOptions)
                    : RenderMissingValue(exportOptions));

            await WriteLineAsync(writer, cellValues, delimiter, cancellationToken).ConfigureAwait(false);
            rowCount++;
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        return rowCount;
    }

    private static IEnumerable<FitNode> EnumerateNodes(FitActivity sourceActivity, FitNodeType nodeType) => nodeType switch
    {
        FitNodeType.Activity => [sourceActivity],
        FitNodeType.Session => sourceActivity.Sessions,
        FitNodeType.Lap => sourceActivity.Sessions.SelectMany(session => session.Laps),
        FitNodeType.Record => sourceActivity.Sessions.SelectMany(session => session.Records),
        FitNodeType.Ancillary => throw new NotSupportedException("Ancillary messages are not exported through the node CSV writer."),
        _ => throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, "Unsupported FIT node type.")
    };

    private static ImmutableArray<ProjectedColumn> BuildProjectedColumns(
        ImmutableArray<CsvColumnSelection> orderedColumns,
        FrozenDictionary<FitExportColumnKey, FitField> referenceFieldLookup,
        FitExportOptions exportOptions)
    {
        ImmutableArray<ProjectedColumn>.Builder projectedColumns = ImmutableArray.CreateBuilder<ProjectedColumn>(orderedColumns.Length * 2);
        foreach (CsvColumnSelection orderedColumn in orderedColumns)
        {
            if (!referenceFieldLookup.TryGetValue(orderedColumn.ColumnKey, out FitField? referenceField))
            {
                throw new InvalidOperationException($"Unable to resolve export metadata for selected column '{orderedColumn.ColumnName}'.");
            }

            projectedColumns.Add(new ProjectedColumn(
                orderedColumn,
                BuildHeader(orderedColumn.ColumnName, referenceField, exportOptions, isLocalTimeDuplicate: false),
                IsLocalTimeDuplicate: false));

            // Keep the local-time duplicate immediately next to the canonical timestamp column so downstream
            // readers can pair them without scanning the whole header set.
            if (exportOptions.IncludeLocalTimeColumns && IsTimestampField(referenceField))
            {
                projectedColumns.Add(new ProjectedColumn(
                    orderedColumn,
                    BuildHeader(orderedColumn.ColumnName, referenceField, exportOptions, isLocalTimeDuplicate: true),
                    IsLocalTimeDuplicate: true));
            }
        }

        return projectedColumns.ToImmutable();
    }

    private static string BuildHeader(
        string baseHeader,
        FitField referenceField,
        FitExportOptions exportOptions,
        bool isLocalTimeDuplicate)
    {
        if (IsTimestampField(referenceField))
        {
            string timestampQualifier = isLocalTimeDuplicate ? "Local" : "UTC";
            return exportOptions.IncludeUnitSuffixInHeaders
                ? $"{baseHeader} [{timestampQualifier}]"
                : $"{baseHeader} {timestampQualifier}";
        }

        string? normalizedUnit = GetNormalizedUnit(referenceField, exportOptions.UnitSystem);
        if (!exportOptions.IncludeUnitSuffixInHeaders || string.IsNullOrWhiteSpace(normalizedUnit))
        {
            return baseHeader;
        }

        return $"{baseHeader} [{normalizedUnit}]";
    }

    private static string FormatFieldValues(FitField field, ProjectedColumn projectedColumn, FitExportOptions exportOptions)
    {
        ImmutableArray<object?> values = GetExportValues(field);
        if (values.IsDefaultOrEmpty)
        {
            return RenderMissingValue(exportOptions);
        }

        if (values.Length == 1)
        {
            return FormatNormalizedValue(field, values[0], projectedColumn.IsLocalTimeDuplicate, exportOptions);
        }

        ImmutableArray<string> formattedValues = values
            .Select(value => FormatNormalizedValue(field, value, projectedColumn.IsLocalTimeDuplicate, exportOptions))
            .ToImmutableArray();

        return formattedValues.All(string.IsNullOrEmpty)
            ? RenderMissingValue(exportOptions)
            : string.Join(ArrayValueSeparator, formattedValues);
    }

    private static ImmutableArray<object?> GetExportValues(FitField field)
    {
        if (field.State.HasEditedDecodedValues)
        {
            return field.State.EditedDecodedValues;
        }

        return field.Original.OriginalValues
            .Select(originalValue => IsInvalidOriginalValue(field.Original.BaseTypeName, originalValue.RawValue, originalValue.DecodedValue)
                ? null
                : originalValue.DecodedValue)
            .ToImmutableArray();
    }

    private static bool IsInvalidOriginalValue(string baseTypeName, object? rawValue, object? decodedValue)
    {
        if (decodedValue is float singleValue && float.IsNaN(singleValue))
        {
            return true;
        }

        if (decodedValue is double doubleValue && double.IsNaN(doubleValue))
        {
            return true;
        }

        if (string.Equals(baseTypeName, "string", StringComparison.OrdinalIgnoreCase))
        {
            return IsInvalidStringValue(rawValue);
        }

        return s_numericInvalidValuesByBaseTypeName.TryGetValue(baseTypeName, out decimal invalidValue)
            && TryConvertToDecimal(rawValue, out decimal numericRawValue)
            && numericRawValue == invalidValue;
    }

    private static bool IsInvalidStringValue(object? rawValue) => rawValue switch
    {
        null => true,
        byte byteValue => byteValue == 0,
        sbyte signedByteValue => signedByteValue == 0,
        short shortValue => shortValue == 0,
        ushort unsignedShortValue => unsignedShortValue == 0,
        int integerValue => integerValue == 0,
        uint unsignedIntegerValue => unsignedIntegerValue == 0,
        long longValue => longValue == 0,
        ulong unsignedLongValue => unsignedLongValue == 0,
        ImmutableArray<byte> immutableByteArrayValue => immutableByteArrayValue.All(byteValue => byteValue == 0),
        byte[] byteArrayValue => byteArrayValue.All(byteValue => byteValue == 0),
        _ => false
    };

    private static bool TryConvertToDecimal(object? value, out decimal numericValue)
    {
        try
        {
            switch (value)
            {
                case decimal decimalValue:
                    numericValue = decimalValue;
                    return true;
                case IConvertible convertibleValue:
                    numericValue = convertibleValue.ToDecimal(CultureInfo.InvariantCulture);
                    return true;
                default:
                    numericValue = default;
                    return false;
            }
        }
        catch (FormatException)
        {
            numericValue = default;
            return false;
        }
        catch (InvalidCastException)
        {
            numericValue = default;
            return false;
        }
        catch (OverflowException)
        {
            numericValue = default;
            return false;
        }
    }

    private static string FormatNormalizedValue(
        FitField field,
        object? value,
        bool isLocalTimeDuplicate,
        FitExportOptions exportOptions)
    {
        object? normalizedValue = NormalizeValue(field, value, isLocalTimeDuplicate, exportOptions);
        return normalizedValue is null
            ? RenderMissingValue(exportOptions)
            : FormatSingleValue(normalizedValue);
    }

    private static object? NormalizeValue(
        FitField field,
        object? value,
        bool isLocalTimeDuplicate,
        FitExportOptions exportOptions)
    {
        if (value is null)
        {
            return null;
        }

        if (value is float singleValue && float.IsNaN(singleValue))
        {
            return null;
        }

        if (value is double doubleValue && double.IsNaN(doubleValue))
        {
            return null;
        }

        if (IsTimestampField(field))
        {
            return NormalizeTimestampValue(value, isLocalTimeDuplicate, exportOptions.LocalTimeZone);
        }

        if (TryNormalizeDurationValue(value, field.Original.Units, out double normalizedDurationValue))
        {
            return normalizedDurationValue;
        }

        if (TryNormalizeDistanceValue(value, field.Original.Units, exportOptions.UnitSystem, out double normalizedDistanceValue))
        {
            return normalizedDistanceValue;
        }

        if (TryNormalizeSpeedValue(value, field.Original.Units, exportOptions.UnitSystem, out double normalizedSpeedValue))
        {
            return normalizedSpeedValue;
        }

        return value;
    }

    private static object? NormalizeTimestampValue(object value, bool isLocalTimeDuplicate, TimeZoneInfo localTimeZone)
    {
        if (!TryConvertToDateTimeOffset(value, out DateTimeOffset timestampValue))
        {
            return value;
        }

        DateTimeOffset utcTimestamp = timestampValue.ToUniversalTime();
        return isLocalTimeDuplicate
            ? TimeZoneInfo.ConvertTime(utcTimestamp, localTimeZone)
            : utcTimestamp;
    }

    private static bool TryConvertToDateTimeOffset(object value, out DateTimeOffset timestampValue)
    {
        switch (value)
        {
            case DateTimeOffset dateTimeOffsetValue:
                timestampValue = dateTimeOffsetValue;
                return true;

            case DateTime dateTimeValue:
                DateTime normalizedDateTime = dateTimeValue.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc)
                    : dateTimeValue;
                timestampValue = new DateTimeOffset(normalizedDateTime);
                return true;

            default:
                timestampValue = default;
                return false;
        }
    }

    private static bool TryNormalizeDurationValue(object value, string? sourceUnit, out double normalizedValue)
    {
        string normalizedUnit = NormalizeUnit(sourceUnit);
        switch (normalizedUnit)
        {
            case "s":
                if (!TryConvertToDouble(value, out double numericValueInSeconds))
                {
                    normalizedValue = default;
                    return false;
                }

                normalizedValue = numericValueInSeconds;
                return true;
            case "ms":
                if (!TryConvertToDouble(value, out double numericValueInMilliseconds))
                {
                    normalizedValue = default;
                    return false;
                }

                normalizedValue = numericValueInMilliseconds / 1000d;
                return true;
            case "min":
                if (!TryConvertToDouble(value, out double numericValueInMinutes))
                {
                    normalizedValue = default;
                    return false;
                }

                normalizedValue = numericValueInMinutes * 60d;
                return true;
            case "h":
                if (!TryConvertToDouble(value, out double numericValueInHours))
                {
                    normalizedValue = default;
                    return false;
                }

                normalizedValue = numericValueInHours * 3600d;
                return true;
            default:
                normalizedValue = default;
                return false;
        }
    }

    private static bool TryNormalizeDistanceValue(
        object value,
        string? sourceUnit,
        FitExportUnitSystem unitSystem,
        out double normalizedValue)
    {
        string normalizedUnit = NormalizeUnit(sourceUnit);
        if (!IsDistanceUnit(normalizedUnit) || !TryConvertToDouble(value, out double numericValue))
        {
            normalizedValue = default;
            return false;
        }

        double distanceInMeters = normalizedUnit switch
        {
            "m" => numericValue,
            "km" => numericValue / KilometersPerMeter,
            "ft" => numericValue / FeetPerMeter,
            "mi" => numericValue / MilesPerMeter,
            _ => double.NaN
        };

        if (double.IsNaN(distanceInMeters))
        {
            normalizedValue = default;
            return false;
        }

        normalizedValue = unitSystem == FitExportUnitSystem.Metric
            ? distanceInMeters * KilometersPerMeter
            : distanceInMeters * MilesPerMeter;
        return true;
    }

    private static bool TryNormalizeSpeedValue(
        object value,
        string? sourceUnit,
        FitExportUnitSystem unitSystem,
        out double normalizedValue)
    {
        string normalizedUnit = NormalizeUnit(sourceUnit);
        if (!IsSpeedUnit(normalizedUnit) || !TryConvertToDouble(value, out double numericValue))
        {
            normalizedValue = default;
            return false;
        }

        double speedInMetersPerSecond = normalizedUnit switch
        {
            "m/s" => numericValue,
            "km/h" => numericValue / KilometersPerHourPerMeterPerSecond,
            "mph" => numericValue / MilesPerHourPerMeterPerSecond,
            _ => double.NaN
        };

        if (double.IsNaN(speedInMetersPerSecond))
        {
            normalizedValue = default;
            return false;
        }

        normalizedValue = unitSystem == FitExportUnitSystem.Metric
            ? speedInMetersPerSecond * KilometersPerHourPerMeterPerSecond
            : speedInMetersPerSecond * MilesPerHourPerMeterPerSecond;
        return true;
    }

    private static bool TryConvertToDouble(object value, out double numericValue)
    {
        // Structured CSV normalization only applies to true numeric payloads.
        // Edited/export values can intentionally be text labels, and those should round-trip unchanged.
        switch (value)
        {
            case byte byteValue:
                numericValue = byteValue;
                return true;
            case sbyte signedByteValue:
                numericValue = signedByteValue;
                return true;
            case short shortValue:
                numericValue = shortValue;
                return true;
            case ushort unsignedShortValue:
                numericValue = unsignedShortValue;
                return true;
            case int integerValue:
                numericValue = integerValue;
                return true;
            case uint unsignedIntegerValue:
                numericValue = unsignedIntegerValue;
                return true;
            case long longValue:
                numericValue = longValue;
                return true;
            case ulong unsignedLongValue:
                numericValue = unsignedLongValue;
                return true;
            case float singleValue:
                numericValue = singleValue;
                return true;
            case double doubleValue:
                numericValue = doubleValue;
                return true;
            case decimal decimalValue:
                numericValue = Convert.ToDouble(decimalValue, CultureInfo.InvariantCulture);
                return true;
            default:
                numericValue = default;
                return false;
        }
    }

    private static bool IsTimestampField(FitField field)
    {
        if (field.Original.ProfileTypeName.Equals("DateTime", StringComparison.OrdinalIgnoreCase)
            || field.Original.ProfileTypeName.Equals("LocalDateTime", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return field.Original.OriginalValues.Any(fieldValue => fieldValue.DecodedValue is DateTimeOffset or DateTime);
    }

    private static string? GetNormalizedUnit(FitField field, FitExportUnitSystem unitSystem)
    {
        if (IsTimestampField(field))
        {
            return null;
        }

        if (TryNormalizeDurationUnit(field.Original.Units, out string durationUnit))
        {
            return durationUnit;
        }

        if (TryNormalizeDistanceUnit(field.Original.Units, unitSystem, out string distanceUnit))
        {
            return distanceUnit;
        }

        if (TryNormalizeSpeedUnit(field.Original.Units, unitSystem, out string speedUnit))
        {
            return speedUnit;
        }

        return string.IsNullOrWhiteSpace(field.Original.Units) ? null : field.Original.Units;
    }

    private static bool TryNormalizeDurationUnit(string? sourceUnit, out string normalizedUnit)
    {
        switch (NormalizeUnit(sourceUnit))
        {
            case "s":
            case "ms":
            case "min":
            case "h":
                // Structured CSV keeps durations numeric and deterministic by normalizing all time spans to seconds.
                normalizedUnit = "s";
                return true;
            default:
                normalizedUnit = string.Empty;
                return false;
        }
    }

    private static bool TryNormalizeDistanceUnit(string? sourceUnit, FitExportUnitSystem unitSystem, out string normalizedUnit)
    {
        switch (NormalizeUnit(sourceUnit))
        {
            case "m":
            case "km":
            case "ft":
            case "mi":
                normalizedUnit = unitSystem == FitExportUnitSystem.Metric ? "km" : "mi";
                return true;
            default:
                normalizedUnit = string.Empty;
                return false;
        }
    }

    private static bool TryNormalizeSpeedUnit(string? sourceUnit, FitExportUnitSystem unitSystem, out string normalizedUnit)
    {
        switch (NormalizeUnit(sourceUnit))
        {
            case "m/s":
            case "km/h":
            case "mph":
                normalizedUnit = unitSystem == FitExportUnitSystem.Metric ? "km/h" : "mph";
                return true;
            default:
                normalizedUnit = string.Empty;
                return false;
        }
    }

    private static string NormalizeUnit(string? unit)
        => string.IsNullOrWhiteSpace(unit)
            ? string.Empty
            : unit.Trim().ToLowerInvariant();

    private static bool IsDistanceUnit(string normalizedUnit)
        => normalizedUnit is "m" or "km" or "ft" or "mi";

    private static bool IsSpeedUnit(string normalizedUnit)
        => normalizedUnit is "m/s" or "km/h" or "mph";

    private static string RenderMissingValue(FitExportOptions exportOptions)
        => exportOptions.MissingValueStyle == FitExportMissingValueStyle.Literal
            ? exportOptions.MissingValueLiteral
            : string.Empty;

    private static void EnsureStructuredCsvTarget(FitExportTarget target)
    {
        if (target != FitExportTarget.StructuredCsv)
        {
            throw new NotSupportedException(
                $"The current CSV exporter only supports '{FitExportTarget.StructuredCsv}'. Requested target: '{target}'.");
        }
    }

    private static string FormatSingleValue(object? value) => value switch
    {
        null => string.Empty,
        DateTimeOffset dateTimeOffsetValue => dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture),
        DateTime dateTimeValue => dateTimeValue.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattableValue => formattableValue.ToString(format: null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty
    };

    private static async Task WriteLineAsync(
        StreamWriter writer,
        IEnumerable<string> values,
        char delimiter,
        CancellationToken cancellationToken)
    {
        string line = string.Join(delimiter, values.Select(value => EscapeValue(value, delimiter)));
        await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    private static string EscapeValue(string value, char delimiter)
    {
        bool requiresEscaping = value.Contains(delimiter)
            || value.Contains('"')
            || value.Contains('\r')
            || value.Contains('\n');

        if (!requiresEscaping)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private sealed record ProjectedColumn(
        CsvColumnSelection Selection,
        string Header,
        bool IsLocalTimeDuplicate);
}
