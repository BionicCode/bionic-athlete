namespace FitToCsvConverter.Data.Exporting;

using System.Collections.Immutable;

internal enum FitExportFieldClassification
{
    DirectStandardFit = 0,
    DirectDeveloperField = 1,
    DerivedFromFit = 2,
    DerivedFromRestoredFitMessages = 3,
    GarminConnectOnlyOrUnconfirmed = 4,
    Unavailable = 5
}

internal sealed class CsvExportManifest
{
    public required string ExportSchemaVersion { get; init; }

    public required string ExporterVersion { get; init; }

    public required string SourceDisplayName { get; init; }

    public string? SourceFilePath { get; init; }

    public required CsvExportTimezoneSemantics TimezoneSemantics { get; init; }

    public required ImmutableArray<CsvExportMessageFamilyManifestEntry> IncludedMessageFamilies { get; init; }

    public required ImmutableArray<CsvExportMessageFamilyManifestEntry> OmittedMessageFamilies { get; init; }

    public bool HasDeveloperFields { get; init; }

    public bool HasUnknownOrVendorFields { get; init; }

    public required ImmutableArray<CsvExportFieldDictionaryEntry> FieldDictionary { get; init; }
}

internal sealed class CsvExportTimezoneSemantics
{
    public required string CanonicalTimestampColumns { get; init; }

    public bool IncludesLocalTimeColumns { get; init; }

    public string? LocalTimeZoneId { get; init; }

    public required string DurationColumns { get; init; }
}

internal sealed class CsvExportMessageFamilyManifestEntry
{
    public required string MessageFamily { get; init; }

    public ushort MessageNumber { get; init; }

    public required string ArtifactName { get; init; }

    public required string ArtifactFileName { get; init; }

    public required ExportedArtifactKind ArtifactKind { get; init; }

    public required string NodeType { get; init; }

    public int RowCount { get; init; }

    public bool ContainsDeveloperFields { get; init; }

    public bool ContainsUnknownOrVendorFields { get; init; }

    public string? OmissionReason { get; init; }
}

internal sealed class CsvExportFieldDictionaryEntry
{
    public required string ExportName { get; init; }

    public required string NodeType { get; init; }

    public required string SourceMessageFamily { get; init; }

    public ushort? SourceMessageNumber { get; init; }

    public string? SourceFieldName { get; init; }

    public required FitExportFieldClassification Classification { get; init; }

    public string? Unit { get; init; }

    public string? Alias { get; init; }

    public string? DerivationFormula { get; init; }

    public bool IsExported { get; init; }

    public string? ArtifactName { get; init; }

    public string? Notes { get; init; }
}
