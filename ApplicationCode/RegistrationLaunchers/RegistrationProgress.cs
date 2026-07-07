using System;
using System.Threading;

namespace Registration.ApplicationCode.RegistrationLaunchers;

/// <summary>
/// Optional progress/cancellation hooks for a registration run. All callbacks are
/// invoked from the launcher's worker threads, so listeners must marshal to their
/// UI thread themselves. Passing <c>null</c> (the default) disables reporting.
/// </summary>
public sealed class RegistrationProgress
{
    /// <summary>Total number of high-level stages a registration run reports.</summary>
    public const int TotalStages = 8;

    /// <summary>Invoked when a new stage begins: (stageNumber, totalStages, stageName).</summary>
    public Action<int, int, string>? OnStageChanged { get; init; }

    /// <summary>Invoked once per feature-vector batch with the batch size (to size a progress bar).</summary>
    public Action<int>? OnFeatureBatchStart { get; init; }

    /// <summary>Invoked once per computed feature vector.</summary>
    public Action? OnFeatureComputed { get; init; }

    /// <summary>Cancels the run; the launcher checks it between and within stages.</summary>
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;

    internal void ReportStage(int stageNumber, string stageName)
        => OnStageChanged?.Invoke(stageNumber, TotalStages, stageName);
}
