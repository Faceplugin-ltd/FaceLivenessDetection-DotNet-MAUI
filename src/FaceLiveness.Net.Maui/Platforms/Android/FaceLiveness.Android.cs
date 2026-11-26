using Android.Content;
using AndroidX.Camera.Core;
using Com.Faceplugin.Faceliveness.Active;
using Com.Faceplugin.Faceliveness.Kit;

using NativeCheck = Com.Faceplugin.Faceliveness.Active.ActiveCheck;
using NativeEngine = Com.Faceplugin.Faceliveness.Kit.FaceEngine;
using NativeOptions = Com.Faceplugin.Faceliveness.Active.ActiveLivenessOptions;
using NativeResult = Com.Faceplugin.Faceliveness.Active.ActiveLivenessResult;
using NativeSession = Com.Faceplugin.Faceliveness.Active.ActiveLivenessSession;

namespace FaceLiveness.Net.Maui;

/// <summary>
/// The Android half of <see cref="FaceLivenessEngine"/>.
/// <para>
/// Everything here goes through <c>face-liveness-kit</c>'s <c>FaceEngine</c> — the same object the
/// native app uses — so activation, the call lock and the engine thread are shared with it rather
/// than reimplemented.
/// </para>
/// </summary>
public static partial class FaceLivenessEngine
{
    static Context Context => Android.App.Application.Context;

    private static partial async Task<int> PlatformStartAsync(string license)
    {
        // FaceEngine.start takes a Kotlin function for its callback, which does not cross into C#
        // cleanly, so this waits on the state it publishes instead. It runs once per process.
        NativeEngine.Start(Context, license);
        return await Task.Run(() =>
        {
            var engine = NativeEngine.Instance;
            // Activation and model loading; generous, because a cold start reads ~80 MB of models.
            for (var waited = 0; waited < 60_000; waited += 100)
            {
                var state = engine.GetState();
                if (state != NativeEngine.State.Loading) return CodeFor(state);
                Thread.Sleep(100);
            }
            return SdkCode.InitFailed;
        }).ConfigureAwait(false);
    }

    private static partial async Task<LicenseStatus> PlatformLicenseStatusAsync() =>
        await Task.Run(() =>
        {
            var status = NativeEngine.Instance.LicenseStatus();
            return new LicenseStatus(
                status.Licensed,
                status.Level,
                status.LevelName,
                status.Recognition,
                status.Liveness,
                status.Label);
        }).ConfigureAwait(false);

    private static partial Task<string> PlatformLastLicenseErrorAsync() =>
        Task.Run(() => NativeEngine.Instance.LastLicenseError());

    private static partial Task<string> PlatformMachineCodeAsync() =>
        // The engine's machine code is read during start(); the kit does not re-expose it, and the
        // engine AAR is packaged unbound, so there is nothing to call here.
        Task.FromResult(string.Empty);

    static int CodeFor(NativeEngine.State state)
    {
        if (state == NativeEngine.State.Ready) return SdkCode.Success;
        if (state == NativeEngine.State.NotActivated) return SdkCode.NotActivated;
        if (state == NativeEngine.State.LicenseInvalid) return SdkCode.LicenseInvalid;
        if (state == NativeEngine.State.LicenseExpired) return SdkCode.LicenseExpired;
        return SdkCode.InitFailed;
    }
}

/// <summary>
/// The Android half of <see cref="FaceLivenessScreens"/>.
/// <para>
/// The active screen is <c>ActiveLivenessActivity</c> from <c>face-liveness-active</c>, launched
/// for a result exactly as the native app launches it, so the camera preview, the anti-spoofing
/// gate, the challenge sequence and the result card are that code and not a copy of it. The
/// passive screen is <see cref="PassiveLivenessActivity"/>, a port of the native demo's screen
/// that runs on the same kit.
/// </para>
/// </summary>
public static partial class FaceLivenessScreens
{
    public static partial async Task<ActiveLivenessResult?> StartActiveLivenessAsync(
        ActiveLivenessOptions options)
    {
        var host = MainActivity.Current
            ?? throw new InvalidOperationException("The screen needs a foreground Activity.");
        var lens = options.UseFrontCamera
            ? CameraSelector.LensFacingFront
            : CameraSelector.LensFacingBack;
        var intent = ActiveLivenessActivity.InvokeIntent(host, ToNative(options), lens);
        var data = await host.StartForResultAsync(intent).ConfigureAwait(false);
        // A closed screen produces no result, which the UI reads as null.
        var result = ActiveLivenessActivity.Result(data);
        return result is null ? null : FromNative(result);
    }

    public static partial Task StartPassiveLivenessAsync(
        bool useFrontCamera,
        float livenessThreshold)
    {
        var host = MainActivity.Current
            ?? throw new InvalidOperationException("The screen needs a foreground Activity.");
        host.StartActivity(PassiveLivenessActivity.NewIntent(host, useFrontCamera, livenessThreshold));
        return Task.CompletedTask;
    }

    static NativeOptions ToNative(ActiveLivenessOptions options)
    {
        // Keep the canonical enum order so the pool is stable when randomize is off.
        var pool = new List<NativeCheck>();
        foreach (var info in ActiveChecks.All)
        {
            if (options.Pool.Contains(info.Key) && NativeCheckFor(info.Key) is { } native)
            {
                pool.Add(native);
            }
        }
        if (pool.Count == 0)
        {
            foreach (var key in ActiveChecks.DefaultPool)
            {
                if (NativeCheckFor(key) is { } native) pool.Add(native);
            }
        }

        return new NativeOptions(
            pool,
            options.Count,
            options.Randomize,
            options.LivenessThreshold,
            options.PassiveSamples,
            options.FramesWait,
            options.YawThreshold,
            options.PitchThreshold,
            options.SmileThreshold,
            options.BlinkThreshold,
            options.BlinksNumber,
            options.PerspectiveThreshold,
            options.AlignAngle,
            options.MinFaceFraction,
            options.QualityThreshold,
            options.InvertTurn,
            options.InvertTilt);
    }

    static NativeCheck? NativeCheckFor(ActiveCheckKey key) => key switch
    {
        ActiveCheckKey.Blink => NativeCheck.Blink,
        ActiveCheckKey.Smile => NativeCheck.Smile,
        ActiveCheckKey.TurnLeft => NativeCheck.TurnLeft,
        ActiveCheckKey.TurnRight => NativeCheck.TurnRight,
        ActiveCheckKey.TurnUp => NativeCheck.TurnUp,
        ActiveCheckKey.TurnDown => NativeCheck.TurnDown,
        ActiveCheckKey.Perspective => NativeCheck.Perspective,
        _ => null,
    };

    static ActiveLivenessResult FromNative(NativeResult result)
    {
        var challenges = new List<ActiveCheckKey>(result.Challenges.Count);
        foreach (var check in result.Challenges)
        {
            if (ActiveChecks.FromWire(check.Key) is { } key) challenges.Add(key);
        }

        var outcomes = new List<StepState>(result.Outcomes.Count);
        foreach (var step in result.Outcomes) outcomes.Add(StepFor(step));

        return new ActiveLivenessResult(
            result.Passed,
            FailureFor(result.Failure),
            result.PassiveScore,
            result.PassivePassed,
            challenges,
            outcomes,
            result.DurationMs);
    }

    static StepState StepFor(NativeSession.StepState step)
    {
        if (step == NativeSession.StepState.Passed) return StepState.Passed;
        if (step == NativeSession.StepState.Failed) return StepState.Failed;
        if (step == NativeSession.StepState.Active) return StepState.Active;
        return StepState.Pending;
    }

    static ActiveLivenessFailure FailureFor(NativeSession.Failure failure)
    {
        if (failure == NativeSession.Failure.Spoof) return ActiveLivenessFailure.Spoof;
        if (failure == NativeSession.Failure.Check) return ActiveLivenessFailure.Check;
        if (failure == NativeSession.Failure.Timeout) return ActiveLivenessFailure.Timeout;
        if (failure == NativeSession.Failure.Cancelled) return ActiveLivenessFailure.Cancelled;
        return ActiveLivenessFailure.None;
    }
}
