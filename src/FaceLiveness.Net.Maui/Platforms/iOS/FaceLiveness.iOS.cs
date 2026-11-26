using FaceLivenessBridge;

namespace FaceLiveness.Net.Maui;

/*
 * The iOS half.
 *
 * On Android this app binds the two libraries the native app is built from and launches its
 * Activity. iOS has no Java-visible API to bind: the screens are Swift, and .NET for iOS reads
 * Objective-C. So Face-LivenessDetection-iOS now publishes `FaceLivenessBridge`, a framework whose
 * only job is to put an `@objc` face on the same screens — `FPLEngine`, `FPLActiveOptions`,
 * `FPLActiveResult`, `FPLLiveness` — built by its `scripts/build_bridge.sh`. This file is the thin
 * translation between that facade and the shared C# model.
 *
 * The screens behind it are the native ones, as on Android: the camera preview, the anti-spoofing
 * gate, the challenge sequence and the result card are the FaceLivenessSDK app's own code, and the
 * wire values match Android's, which is why `LivenessModels.cs` is shared.
 */

public static partial class FaceLivenessEngine
{
    private static partial Task<int> PlatformStartAsync(string license)
    {
        var started = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        // The bridge activates and loads the models on the engine's worker and calls back on the
        // main queue.
        FPLEngine.Start(license, code => started.TrySetResult((int)code));
        return started.Task;
    }

    private static partial Task<LicenseStatus> PlatformLicenseStatusAsync()
    {
        var read = new TaskCompletionSource<LicenseStatus>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        FPLEngine.LicenseStatus(status => read.TrySetResult(new LicenseStatus(
            status.Licensed,
            (int)status.Level,
            status.LevelName,
            status.Recognition,
            status.Liveness,
            status.Label)));
        return read.Task;
    }

    private static partial Task<string> PlatformLastLicenseErrorAsync() =>
        Task.FromResult(FPLEngine.LastLicenseError ?? string.Empty);

    private static partial Task<string> PlatformMachineCodeAsync()
    {
        var read = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        FPLEngine.MachineCode(code => read.TrySetResult(code?.ToString() ?? string.Empty));
        return read.Task;
    }
}

public static partial class FaceLivenessScreens
{
    public static partial Task<ActiveLivenessResult?> StartActiveLivenessAsync(
        ActiveLivenessOptions options)
    {
        var attempt = new TaskCompletionSource<ActiveLivenessResult?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        FPLLiveness.PresentActiveLiveness(ToNative(options), result =>
            // A closed screen produces no result, which the UI reads as null.
            attempt.TrySetResult(result is null ? null : FromNative(result)));
        return attempt.Task;
    }

    public static partial Task StartPassiveLivenessAsync(
        bool useFrontCamera,
        float livenessThreshold)
    {
        FPLLiveness.PresentPassiveLiveness(useFrontCamera, livenessThreshold);
        return Task.CompletedTask;
    }

    static FPLActiveOptions ToNative(ActiveLivenessOptions options)
    {
        // Keep the canonical order so the pool is stable when randomize is off.
        var pool = ActiveChecks.All
            .Where(info => options.Pool.Contains(info.Key))
            .Select(info => info.WireKey)
            .ToArray();
        if (pool.Length == 0)
        {
            pool = ActiveChecks.All
                .Where(info => ActiveChecks.DefaultPool.Contains(info.Key))
                .Select(info => info.WireKey)
                .ToArray();
        }

        return new FPLActiveOptions
        {
            Pool = pool,
            Count = options.Count,
            Randomize = options.Randomize,
            LivenessThreshold = options.LivenessThreshold,
            PassiveSamples = options.PassiveSamples,
            FramesWait = options.FramesWait,
            YawThreshold = options.YawThreshold,
            PitchThreshold = options.PitchThreshold,
            SmileThreshold = options.SmileThreshold,
            BlinkThreshold = options.BlinkThreshold,
            BlinksNumber = options.BlinksNumber,
            PerspectiveThreshold = options.PerspectiveThreshold,
            AlignAngle = options.AlignAngle,
            MinFaceFraction = options.MinFaceFraction,
            QualityThreshold = options.QualityThreshold,
            InvertTurn = options.InvertTurn,
            InvertTilt = options.InvertTilt,
            UseFrontCamera = options.UseFrontCamera,
        };
    }

    static ActiveLivenessResult FromNative(FPLActiveResult result)
    {
        var challenges = new List<ActiveCheckKey>();
        foreach (var key in result.Challenges ?? [])
        {
            if (ActiveChecks.FromWire(key) is { } mapped) challenges.Add(mapped);
        }

        var outcomes = new List<StepState>();
        foreach (var name in result.Outcomes ?? []) outcomes.Add(StepFor(name));

        return new ActiveLivenessResult(
            result.Passed,
            FailureFor(result.Failure),
            result.PassiveScore,
            result.PassivePassed,
            challenges,
            outcomes,
            result.DurationMs);
    }

    static StepState StepFor(string name) => name switch
    {
        "PASSED" => StepState.Passed,
        "FAILED" => StepState.Failed,
        "ACTIVE" => StepState.Active,
        _ => StepState.Pending,
    };

    static ActiveLivenessFailure FailureFor(string name) => name switch
    {
        "SPOOF" => ActiveLivenessFailure.Spoof,
        "CHECK" => ActiveLivenessFailure.Check,
        "TIMEOUT" => ActiveLivenessFailure.Timeout,
        "CANCELLED" => ActiveLivenessFailure.Cancelled,
        _ => ActiveLivenessFailure.None,
    };
}
