namespace FaceLiveness.Net.Maui;

/// <summary>
/// One active-liveness challenge. The key is the wire value shared with the native side and with
/// the Flutter, React Native, Ionic and Cordova apps.
/// </summary>
public enum ActiveCheckKey
{
    Blink,
    Smile,
    TurnLeft,
    TurnRight,
    TurnUp,
    TurnDown,
    Perspective,
}

/// <summary>What a challenge is called and what it asks for.</summary>
/// <param name="Key">The challenge.</param>
/// <param name="WireKey">The string the native enum stores it under.</param>
/// <param name="Title">Label shown on the challenge card and in the result list.</param>
/// <param name="Hint">One-line instruction shown under the title.</param>
public readonly record struct ActiveCheckInfo(
    ActiveCheckKey Key,
    string WireKey,
    string Title,
    string Hint);

public static class ActiveChecks
{
    /// <summary>Every challenge, in the canonical order the native enum declares.</summary>
    public static readonly IReadOnlyList<ActiveCheckInfo> All =
    [
        new(ActiveCheckKey.Blink, "blink", "Blink", "Blink your eyes a couple of times"),
        new(ActiveCheckKey.Smile, "smile", "Smile", "Give the camera a clear smile"),
        new(ActiveCheckKey.TurnLeft, "turn_left", "Turn left", "Turn your head to your left"),
        new(ActiveCheckKey.TurnRight, "turn_right", "Turn right", "Turn your head to your right"),
        new(ActiveCheckKey.TurnUp, "turn_up", "Look up", "Tilt your head up, keep your eyes on the screen"),
        new(ActiveCheckKey.TurnDown, "turn_down", "Look down", "Tilt your head down slowly"),
        new(ActiveCheckKey.Perspective, "perspective", "Move closer", "Bring the phone a little closer to your face"),
    ];

    /// <summary>Every challenge but "move closer", which not every placement of the phone allows.</summary>
    public static readonly IReadOnlyList<ActiveCheckKey> DefaultPool =
    [
        ActiveCheckKey.Blink,
        ActiveCheckKey.Smile,
        ActiveCheckKey.TurnLeft,
        ActiveCheckKey.TurnRight,
        ActiveCheckKey.TurnUp,
        ActiveCheckKey.TurnDown,
    ];

    public static ActiveCheckInfo Info(ActiveCheckKey key) =>
        All.FirstOrDefault(c => c.Key == key, All[0]);

    public static ActiveCheckKey? FromWire(string wireKey)
    {
        foreach (var info in All)
        {
            if (info.WireKey == wireKey) return info.Key;
        }
        return null;
    }
}

/// <summary>The engine's own return codes (<c>FaceRecognitionSDK.SDK_*</c>).</summary>
public static class SdkCode
{
    public const int Success = 0;
    public const int LicenseInvalid = 1;
    public const int LicenseExpired = 2;
    public const int NotActivated = 3;
    public const int InitFailed = 4;
}

public enum EngineState
{
    Loading,
    Ready,
    NotActivated,
    LicenseInvalid,
    LicenseExpired,
    Failed,
}

/// <summary>
/// License capabilities from the engine. The liveness screens unlock when <see cref="Liveness"/>
/// is granted — a "Liveness only" or "Recognition + Liveness" key.
/// </summary>
public readonly record struct LicenseStatus(
    bool Licensed,
    int Level,
    string LevelName,
    bool Recognition,
    bool Liveness,
    string Label)
{
    public static readonly LicenseStatus NotLicensed =
        new(false, -1, "None", false, false, "Not licensed");
}

/// <summary>
/// How one active-liveness attempt is run.
/// <para>
/// Every attempt begins with the passive anti-spoofing gate, which the face has to clear at
/// <see cref="LivenessThreshold"/> before any challenge is asked. The angles are degrees, as the
/// engine reports head pose. The defaults are the native <c>ActiveLivenessOptions</c> defaults
/// exactly, so every app judges an attempt the same way.
/// </para>
/// </summary>
public sealed record ActiveLivenessOptions
{
    public IReadOnlyList<ActiveCheckKey> Pool { get; init; } = ActiveChecks.DefaultPool;
    public int Count { get; init; } = 3;
    public bool Randomize { get; init; } = true;

    /// <summary>The engine's own passive-liveness operating point.</summary>
    public float LivenessThreshold { get; init; } = 0.5f;

    /// <summary>Frames the passive gate averages before it decides.</summary>
    public int PassiveSamples { get; init; } = 3;

    /// <summary>About 15 analyzed frames a second on a mid-range phone.</summary>
    public int FramesWait { get; init; } = 150;

    public float YawThreshold { get; init; } = 25f;
    public float PitchThreshold { get; init; } = 15f;
    public float SmileThreshold { get; init; } = 0.5f;
    public float BlinkThreshold { get; init; } = 0.3f;
    public int BlinksNumber { get; init; } = 2;
    public float PerspectiveThreshold { get; init; } = 0.15f;
    public float AlignAngle { get; init; } = 12f;

    /// <summary>Smallest the face may be, as a fraction of the frame's short side, to be scored.</summary>
    public float MinFaceFraction { get; init; } = 0.3f;

    /// <summary>Lowest image quality the engine's own score may report and still be judged.</summary>
    public float QualityThreshold { get; init; } = 0.3f;

    public bool InvertTurn { get; init; }
    public bool InvertTilt { get; init; }
    public bool UseFrontCamera { get; init; } = true;
}

/// <summary>Outcome of one challenge, or of the anti-spoofing gate.</summary>
public enum StepState
{
    Pending,
    Active,
    Passed,
    Failed,
}

/// <summary>Why an attempt did not pass.</summary>
public enum ActiveLivenessFailure
{
    None,
    Spoof,
    Check,
    Timeout,
    Cancelled,
}

/// <summary>
/// What one attempt came to, handed back by the native active-liveness screen.
/// <para>
/// <see cref="Passed"/> is the only thing most callers need: it is true when the passive
/// anti-spoofing gate cleared <em>and</em> every challenge was completed.
/// </para>
/// </summary>
/// <param name="PassiveScore">
/// Averaged passive anti-spoofing score, or -1 when the engine never returned one.
/// </param>
/// <param name="Challenges">The challenges asked, in the order they were asked.</param>
/// <param name="Outcomes">One outcome per entry of <paramref name="Challenges"/>.</param>
public sealed record ActiveLivenessResult(
    bool Passed,
    ActiveLivenessFailure Failure,
    float PassiveScore,
    bool PassivePassed,
    IReadOnlyList<ActiveCheckKey> Challenges,
    IReadOnlyList<StepState> Outcomes,
    long DurationMs)
{
    /// <summary>The challenge that ended the attempt, when one did.</summary>
    public ActiveCheckKey? FailedCheck
    {
        get
        {
            for (var i = 0; i < Outcomes.Count && i < Challenges.Count; i++)
            {
                if (Outcomes[i] == StepState.Failed) return Challenges[i];
            }
            return null;
        }
    }
}

public static class LivenessText
{
    /// <summary>The short message the native home screen shows in its status pill.</summary>
    public static string MessageForCode(int code) => code switch
    {
        SdkCode.LicenseInvalid => "Invalid license!",
        SdkCode.LicenseExpired => "License expired!",
        SdkCode.NotActivated => "No activated!",
        SdkCode.InitFailed => "Init error!",
        _ => $"SDK not ready: {code}",
    };

    /// <summary>The word the native result card shows for an outcome.</summary>
    public static string StepLabel(StepState step) => step switch
    {
        StepState.Passed => "Passed",
        StepState.Failed => "Failed",
        _ => "Not reached",
    };

    /// <summary>What the demo reports after an attempt. Shared with the other apps word for word.</summary>
    public static string Describe(ActiveLivenessResult result)
    {
        if (result.Passed) return "Liveness confirmed — live person";
        if (!result.PassivePassed) return "Anti-spoofing check failed — no challenges were asked";
        var failed = result.FailedCheck;
        if (failed is { } key)
        {
            return $"Liveness not confirmed — “{ActiveChecks.Info(key).Title}” was not completed";
        }
        return "Liveness not confirmed";
    }
}
