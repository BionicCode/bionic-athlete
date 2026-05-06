namespace BionicAthlete.Training.Application.Reporting;

using BionicAthlete.Application.Reporting;
using BionicAthlete.Training.Domain.Activities;

/// <summary>
/// Projects decoded FIT activity data into the neutral View C report model.
/// </summary>
public interface IActivityReportProjector
{
    /// <summary>
    /// Creates a semantic report model for one decoded activity.
    /// </summary>
    /// <param name="activity">The decoded activity source.</param>
    /// <param name="options">Deterministic report options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The projected report model.</returns>
    Task<Report> ProjectAsync(
        FitActivity activity,
        ReportExportOptions options,
        CancellationToken cancellationToken = default);
}
