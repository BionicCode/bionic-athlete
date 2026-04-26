namespace FitToCsvConverter.Reporting;

/// <summary>
/// Classifies how a metric shown in a human-readable report relates to the source FIT data.
/// </summary>
public enum ActivityReportFieldClassification
{
    /// <summary>
    /// The metric is not available in the current source activity.
    /// </summary>
    Unavailable = 0,

    /// <summary>
    /// The metric comes directly from a public standard FIT field.
    /// </summary>
    DirectStandardFit = 1,

    /// <summary>
    /// The metric comes directly from a FIT developer field.
    /// </summary>
    DirectDeveloperField = 2,

    /// <summary>
    /// The metric is formula-derived from one or more FIT fields.
    /// </summary>
    DerivedFromFit = 3,

    /// <summary>
    /// The metric is mapped from preserved unknown FIT data and is not publicly named by the standard FIT profile.
    /// </summary>
    MappedFromUnmappedFitField = 4,

    /// <summary>
    /// The metric is visible in Garmin Connect or another reference but is not confirmed from the exported FIT file.
    /// </summary>
    GarminConnectOnlyOrUnconfirmed = 5
}
