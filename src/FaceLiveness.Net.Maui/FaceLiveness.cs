namespace FaceLiveness.Net.Maui;

/// <summary>
/// Process-wide engine owner: activation, and the licence state the UI reads.
/// <para>
/// The engine is <c>FaceRecognitionSDK</c> — the same build the native Android and iOS liveness
/// apps run, loaded natively. Call <see cref="StartAsync"/> once.
/// </para>
/// <para>Mirrors Android <c>face-liveness-kit/FaceEngine</c> and iOS <c>FaceEngine</c>.</para>
/// </summary>
public static partial class FaceLivenessEngine
{
    static EngineState _state = EngineState.Loading;
    static LicenseStatus _license = LicenseStatus.NotLicensed;
    static int _lastCode = SdkCode.InitFailed;

    public static EngineState State => _state;

    public static bool IsReady => _state == EngineState.Ready;

    /// <summary>What the last <see cref="StartAsync"/> found the key to unlock.</summary>
    public static LicenseStatus License => _license;

    public static int LastCode => _lastCode;

    /// <summary>Raised whenever the state changes, on the thread that changed it.</summary>
    public static event Action<EngineState>? StateChanged;

    /// <summary>
    /// Activate with <paramref name="license"/> and load the models. Safe to call more than once;
    /// a ready engine returns immediately.
    /// </summary>
    public static async Task<int> StartAsync(string license)
    {
        if (IsReady) return SdkCode.Success;
        Publish(EngineState.Loading);
        _lastCode = await PlatformStartAsync(license).ConfigureAwait(false);
        Publish(StateForCode(_lastCode));
        _license = await PlatformLicenseStatusAsync().ConfigureAwait(false);
        return _lastCode;
    }

    public static async Task<LicenseStatus> RefreshLicenseAsync()
    {
        _license = await PlatformLicenseStatusAsync().ConfigureAwait(false);
        return _license;
    }

    public static Task<string> LastLicenseErrorAsync() => PlatformLastLicenseErrorAsync();

    /// <summary>This device's machine code, for issuing a node-locked key.</summary>
    public static Task<string> MachineCodeAsync() => PlatformMachineCodeAsync();

    /// <summary>The message the native home screen shows in its status pill.</summary>
    public static async Task<string> StatusMessageAsync() => _state switch
    {
        EngineState.Loading => "Loading on-device models…",
        EngineState.Ready => $"Ready · {_license.Label}",
        EngineState.Failed => (await LastLicenseErrorAsync().ConfigureAwait(false)).Trim() is { Length: > 0 } detail
            ? $"Init error! {detail}"
            : "Init error!",
        _ => LivenessText.MessageForCode(_lastCode),
    };

    static EngineState StateForCode(int code) => code switch
    {
        SdkCode.Success => EngineState.Ready,
        SdkCode.LicenseInvalid => EngineState.LicenseInvalid,
        SdkCode.LicenseExpired => EngineState.LicenseExpired,
        SdkCode.NotActivated => EngineState.NotActivated,
        _ => EngineState.Failed,
    };

    static void Publish(EngineState next)
    {
        _state = next;
        StateChanged?.Invoke(next);
    }

    private static partial Task<int> PlatformStartAsync(string license);
    private static partial Task<LicenseStatus> PlatformLicenseStatusAsync();
    private static partial Task<string> PlatformLastLicenseErrorAsync();
    private static partial Task<string> PlatformMachineCodeAsync();
}

/// <summary>
/// Launches the native liveness screens.
/// <para>
/// <see cref="FaceLivenessEngine.StartAsync"/> has to have succeeded first; the screens refuse to
/// run on an engine that is not ready.
/// </para>
/// </summary>
public static partial class FaceLivenessScreens
{
    /// <summary>
    /// Opens the guided active-liveness screen and waits for the attempt to finish.
    /// <para>
    /// One attempt runs in three stages: the face is found and aligned, the <b>passive
    /// anti-spoofing gate</b> scores it, and only then are the challenges asked — so a printed
    /// photo or a replayed video is turned away before it is ever invited to blink.
    /// </para>
    /// <para>Returns <c>null</c> when the user closed the screen before an attempt produced a result.</para>
    /// </summary>
    public static partial Task<ActiveLivenessResult?> StartActiveLivenessAsync(
        ActiveLivenessOptions options);

    /// <summary>
    /// Opens the passive-liveness screen: every camera frame is scored on its own, with no action
    /// asked of the user, and the HUD shows liveness, quality and head pose.
    /// </summary>
    public static partial Task StartPassiveLivenessAsync(
        bool useFrontCamera = true,
        float livenessThreshold = 0.5f);
}
