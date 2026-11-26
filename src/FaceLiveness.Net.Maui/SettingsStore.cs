namespace FaceLiveness.Net.Maui;

/// <summary>
/// Every preference this demo stores, in one place.
/// <para>
/// The native screens take their configuration as arguments, so this is the only part of the app
/// that knows about persistence: it reads what the user chose and builds the options the native
/// active-liveness screen runs with.
/// </para>
/// <para>
/// The fields and defaults mirror Android <c>data/AppSettings</c> and iOS <c>AppSettings</c>, so
/// the demos behave the same. Defaults are the engine's own operating points.
/// </para>
/// </summary>
public sealed class SettingsStore
{
    static readonly ActiveLivenessOptions Defaults = new();

    public static SettingsStore Current { get; } = new();

    public bool UseFrontCamera
    {
        get => Preferences.Get(nameof(UseFrontCamera), true);
        set => Preferences.Set(nameof(UseFrontCamera), value);
    }

    public float LivenessThreshold
    {
        get => Read(nameof(LivenessThreshold), Defaults.LivenessThreshold);
        set => Write(nameof(LivenessThreshold), value);
    }

    /// <summary>The pool the challenge sequence is drawn from, in canonical order.</summary>
    public IReadOnlyList<ActiveCheckKey> Pool
    {
        get
        {
            var stored = Preferences.Get(nameof(Pool), string.Empty);
            if (string.IsNullOrEmpty(stored)) return ActiveChecks.DefaultPool;
            var wanted = new HashSet<string>(stored.Split(',', StringSplitOptions.RemoveEmptyEntries));
            var pool = ActiveChecks.All
                .Where(info => wanted.Contains(info.WireKey))
                .Select(info => info.Key)
                .ToList();
            return pool.Count > 0 ? pool : ActiveChecks.DefaultPool;
        }
        set
        {
            var keys = ActiveChecks.All
                .Where(info => value.Contains(info.Key))
                .Select(info => info.WireKey);
            Preferences.Set(nameof(Pool), string.Join(',', keys));
        }
    }

    public int Count
    {
        get => Preferences.Get(nameof(Count), Defaults.Count);
        set => Preferences.Set(nameof(Count), value);
    }

    public bool Randomize
    {
        get => Preferences.Get(nameof(Randomize), Defaults.Randomize);
        set => Preferences.Set(nameof(Randomize), value);
    }

    public int FramesWait
    {
        get => Preferences.Get(nameof(FramesWait), Defaults.FramesWait);
        set => Preferences.Set(nameof(FramesWait), value);
    }

    public float Yaw
    {
        get => Read(nameof(Yaw), Defaults.YawThreshold);
        set => Write(nameof(Yaw), value);
    }

    public float Pitch
    {
        get => Read(nameof(Pitch), Defaults.PitchThreshold);
        set => Write(nameof(Pitch), value);
    }

    public float Smile
    {
        get => Read(nameof(Smile), Defaults.SmileThreshold);
        set => Write(nameof(Smile), value);
    }

    public float Blink
    {
        get => Read(nameof(Blink), Defaults.BlinkThreshold);
        set => Write(nameof(Blink), value);
    }

    public int BlinksNumber
    {
        get => Preferences.Get(nameof(BlinksNumber), Defaults.BlinksNumber);
        set => Preferences.Set(nameof(BlinksNumber), value);
    }

    public float Perspective
    {
        get => Read(nameof(Perspective), Defaults.PerspectiveThreshold);
        set => Write(nameof(Perspective), value);
    }

    public float AlignAngle
    {
        get => Read(nameof(AlignAngle), Defaults.AlignAngle);
        set => Write(nameof(AlignAngle), value);
    }

    public float MinFace
    {
        get => Read(nameof(MinFace), Defaults.MinFaceFraction);
        set => Write(nameof(MinFace), value);
    }

    public float Quality
    {
        get => Read(nameof(Quality), Defaults.QualityThreshold);
        set => Write(nameof(Quality), value);
    }

    public bool InvertTurn
    {
        get => Preferences.Get(nameof(InvertTurn), Defaults.InvertTurn);
        set => Preferences.Set(nameof(InvertTurn), value);
    }

    public bool InvertTilt
    {
        get => Preferences.Get(nameof(InvertTilt), Defaults.InvertTilt);
        set => Preferences.Set(nameof(InvertTilt), value);
    }

    public void RestoreDefaults() => Preferences.Clear();

    /// <summary>What the native active-liveness screen should run, as the user has it configured.</summary>
    public ActiveLivenessOptions ToOptions() => new()
    {
        Pool = Pool,
        Count = Count,
        Randomize = Randomize,
        LivenessThreshold = LivenessThreshold,
        FramesWait = FramesWait,
        YawThreshold = Yaw,
        PitchThreshold = Pitch,
        SmileThreshold = Smile,
        BlinkThreshold = Blink,
        BlinksNumber = BlinksNumber,
        PerspectiveThreshold = Perspective,
        AlignAngle = AlignAngle,
        MinFaceFraction = MinFace,
        QualityThreshold = Quality,
        InvertTurn = InvertTurn,
        InvertTilt = InvertTilt,
        UseFrontCamera = UseFrontCamera,
    };

    // Preferences stores doubles; the engine's thresholds are floats.
    static float Read(string key, float fallback) => (float)Preferences.Get(key, (double)fallback);

    static void Write(string key, float value) => Preferences.Set(key, (double)value);
}
