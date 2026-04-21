namespace FitToCsvConverter.Data.Exporting;

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

    /// <inheritdoc/>
    public async Task<CsvExportResult> ExportAsync(CsvExportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ImmutableArray<ExportedArtifact>.Builder exportedArtifacts = ImmutableArray.CreateBuilder<ExportedArtifact>(request.NodeRequests.Length);

        // Respect the request order so callers can keep the generated artifact list stable across export and archive flows.
        foreach (CsvNodeExportRequest nodeRequest in request.NodeRequests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int rowCount = await ExportNodeAsync(request.SourceActivity, nodeRequest, request.Encoding, request.Delimiter, cancellationToken).ConfigureAwait(false);
            exportedArtifacts.Add(new ExportedArtifact(nodeRequest.NodeType, nodeRequest.DestinationFilePath, rowCount));
        }

        return new CsvExportResult(exportedArtifacts.ToImmutable());
    }

    private static async Task<int> ExportNodeAsync(
        FitActivity sourceActivity,
        CsvNodeExportRequest nodeRequest,
        Encoding encoding,
        char delimiter,
        CancellationToken cancellationToken)
    {
        string destinationDirectoryPath = Path.GetDirectoryName(nodeRequest.DestinationFilePath)
            ?? throw new InvalidOperationException($"Unable to determine a destination directory for '{nodeRequest.DestinationFilePath}'.");
        _ = Directory.CreateDirectory(destinationDirectoryPath);

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

        ImmutableArray<CsvColumnSelection> orderedColumns = nodeRequest.Columns
            .OrderBy(column => column.Order)
            .ThenBy(column => column.SourceName, StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();

        await WriteLineAsync(writer, orderedColumns.Select(column => column.ColumnName), delimiter, cancellationToken).ConfigureAwait(false);

        int rowCount = 0;
        foreach (FitNode node in EnumerateNodes(sourceActivity, nodeRequest.NodeType))
        {
            cancellationToken.ThrowIfCancellationRequested();

            IReadOnlyDictionary<FitExportColumnKey, FitField> fieldLookup = node.Fields.ToDictionary(
                field => field.Original.ExportColumnKey,
                field => field);

            IEnumerable<string> cellValues = orderedColumns.Select(column =>
                fieldLookup.TryGetValue(column.ColumnKey, out FitField? field)
                    ? FormatFieldValues(field.GetEffectiveDecodedValues())
                    : string.Empty);

            await WriteLineAsync(writer, cellValues, delimiter, cancellationToken).ConfigureAwait(false);
            rowCount++;
        }

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        return rowCount;
    }

    private static IEnumerable<FitNode> EnumerateNodes(FitActivity sourceActivity, FitNodeType nodeType)
    {
        return nodeType switch
        {
            FitNodeType.Activity => [sourceActivity],
            FitNodeType.Session => sourceActivity.Sessions,
            FitNodeType.Lap => sourceActivity.Sessions.SelectMany(session => session.Laps),
            FitNodeType.Record => sourceActivity.Sessions.SelectMany(session => session.Records),
            FitNodeType.Ancillary => throw new NotSupportedException("Ancillary messages are not exported through the node CSV writer."),
            _ => throw new ArgumentOutOfRangeException(nameof(nodeType), nodeType, "Unsupported FIT node type.")
        };
    }

    private static string FormatFieldValues(ImmutableArray<object?> values)
    {
        if (values.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        if (values.Length == 1)
        {
            return FormatSingleValue(values[0]);
        }

        return string.Join(ArrayValueSeparator, values.Select(FormatSingleValue));
    }

    private static string FormatSingleValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTimeOffset dateTimeOffsetValue => dateTimeOffsetValue.ToString("O", CultureInfo.InvariantCulture),
            DateTime dateTimeValue => dateTimeValue.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattableValue => formattableValue.ToString(format: null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

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
}
