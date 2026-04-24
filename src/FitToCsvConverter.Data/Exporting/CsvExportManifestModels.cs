namespace FitToCsvConverter.Data.Exporting;

using System.Collections.Immutable;

internal enum FitExportFieldClassification
{
    DirectStandardFit = 0,
    DirectDeveloperField = 1,
    DerivedFromFit = 2,
    DerivedFromRestoredFitMessages = 3,
    GarminConnectOnlyOrUnconfirmed = 4,
    Unavailable = 5,
    RawPreservedField = 6,
    UnmappedField = 7,
    UnknownMessageFamily = 8,
    VendorOrFutureField = 9
}

internal enum CsvExportArtifactLayer
{
    RawLosslessArchive = 0,
    ConsolidatedMachineExport = 1,
    Manifest = 2
}

internal enum CsvExportDataView
{
    RawCanonicalFitView = 0,
    StructuredMachineView = 1,
    Manifest = 2
}

internal enum FitProfileCoverageClassification
{
    MatchedPublicStandardProfile = 0,
    DeveloperField = 1,
    UnknownOrUnmappedPreservedField = 2
}

internal enum CsvExportArtifactGroup
{
    Core = 0,
    Metadata = 1,
    Analytics = 2,
    RawUnmapped = 3,
    RawLossless = 4,
    Manifest = 5
}

internal enum CsvExportAliasKind
{
    DirectFieldAlias = 0,
    DerivedFieldAlias = 1,
    SectionLabel = 2,
    HumanFriendlyAlias = 3
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

    public required ImmutableArray<CsvExportArtifactManifestEntry> Artifacts { get; init; }

    public required ImmutableArray<CsvExportFieldDictionaryEntry> FieldDictionary { get; init; }

    public required CsvExportProfileCoverage ProfileCoverage { get; init; }
}

internal sealed class CsvExportTimezoneSemantics
{
    public required string CanonicalTimestampColumns { get; init; }

    public bool IncludesLocalTimeColumns { get; init; }

    public string? LocalTimeZoneId { get; init; }

    public required string DurationColumns { get; init; }
}

internal sealed class CsvExportArtifactManifestEntry
{
    public required string ArtifactName { get; init; }

    public required string ArtifactFileName { get; init; }

    public required ExportedArtifactKind ArtifactKind { get; init; }

    public required CsvExportArtifactLayer ArtifactLayer { get; init; }

    public required CsvExportDataView DataView { get; init; }

    public required CsvExportArtifactGroup ArtifactGroup { get; init; }

    public required string NodeType { get; init; }

    public int RowCount { get; init; }

    public required ImmutableArray<string> MessageFamilies { get; init; }
}

internal sealed class CsvExportMessageFamilyManifestEntry
{
    public required string MessageFamily { get; init; }

    public ushort MessageNumber { get; init; }

    public required string ArtifactName { get; init; }

    public required string ArtifactFileName { get; init; }

    public required ExportedArtifactKind ArtifactKind { get; init; }

    public required CsvExportArtifactLayer ArtifactLayer { get; init; }

    public required CsvExportDataView DataView { get; init; }

    public required CsvExportArtifactGroup ArtifactGroup { get; init; }

    public required string NodeType { get; init; }

    public int RowCount { get; init; }

    public bool ContainsDeveloperFields { get; init; }

    public bool ContainsUnknownOrVendorFields { get; init; }

    public string? OmissionReason { get; init; }
}

internal sealed class CsvExportFieldDictionaryEntry
{
    // The actual column name written to a specific artifact.
    // This can repeat across node families such as session.total_work and lap.total_work.
    public required string ExportName { get; init; }

    // The stable machine identifier for cross-artifact lookups and alias mapping.
    // This remains qualified by message family so consumers can disambiguate repeated export names.
    public required string CanonicalName { get; init; }

    public required string CanonicalMessageFamily { get; init; }

    public string? CanonicalFieldName { get; init; }

    public required string NodeType { get; init; }

    public required string SourceMessageFamily { get; init; }

    public ushort? SourceMessageNumber { get; init; }

    public string? SourceFieldName { get; init; }

    public required FitExportFieldClassification Classification { get; init; }

    public string? Unit { get; init; }

    public string? Alias { get; init; }

    public CsvExportFieldAliasMetadata? AliasMetadata { get; init; }

    public string? DerivationFormula { get; init; }

    public bool IsExported { get; init; }

    public string? ArtifactName { get; init; }

    public bool IsArray { get; init; }

    public string? ValueShape { get; init; }

    public string? ValueSeparator { get; init; }

    public string? ValueOrdering { get; init; }

    public string? Notes { get; init; }
}

internal sealed class CsvExportProfileCoverage
{
    public required string CatalogSource { get; init; }

    public int MatchedPublicStandardProfileFieldCount { get; init; }

    public int DeveloperFieldCount { get; init; }

    public int UnknownOrUnmappedPreservedFieldCount { get; init; }

    public required ImmutableArray<CsvExportProfileCoverageEntry> Entries { get; init; }
}

internal sealed class CsvExportProfileCoverageEntry
{
    public required string CanonicalName { get; init; }

    public required string SourceMessageFamily { get; init; }

    public ushort? SourceMessageNumber { get; init; }

    public string? SourceFieldName { get; init; }

    public required FitProfileCoverageClassification Classification { get; init; }

    public string? Notes { get; init; }
}

internal sealed class CsvExportFieldAliasMetadata
{
    public string? DisplayAliasDefault { get; init; }

    public string? DisplayAliasSource { get; init; }

    public string? DisplayAliasLocale { get; init; }

    public double? DisplayAliasConfidence { get; init; }

    public CsvExportAliasKind? AliasKind { get; init; }

    public bool IsDirectAlias { get; init; }

    public bool IsDerivedAlias { get; init; }

    public string? Notes { get; init; }
}
