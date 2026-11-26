namespace FaceLiveness.Net.Maui;

/// <summary>
/// Settings — camera lens, the passive score threshold, and the full active-liveness configuration.
/// <para>
/// Mirrors Android <c>ui/SettingsActivity</c> + <c>xml/root_preferences</c> and iOS
/// <c>SettingsViewController</c>. The page only writes preferences;
/// <see cref="SettingsStore.ToOptions"/> turns them into what the native screen runs with.
/// </para>
/// </summary>
public class SettingsPage : ContentPage
{
    readonly SettingsStore _settings = SettingsStore.Current;

    public SettingsPage()
    {
        Title = "Settings";
        BackgroundColor = Palette.Bg;
        Content = Build();
    }

    View Build()
    {
        var layout = new VerticalStackLayout { Padding = new Thickness(16, 8, 16, 28) };

        layout.Add(Chrome.Section("CAMERA"));
        layout.Add(Group(
            Choice("Camera lens", ["Front", "Back"],
                _settings.UseFrontCamera ? 0 : 1,
                index => _settings.UseFrontCamera = index == 0)));

        layout.Add(Chrome.Section("PASSIVE LIVENESS"));
        layout.Add(Group(
            Threshold("Score threshold",
                "Minimum score to count as live (0.0–1.0). The engine’s own operating point is "
                    + "0.5, and the active check uses the same gate.",
                _settings.LivenessThreshold,
                value => _settings.LivenessThreshold = value)));

        layout.Add(Chrome.Section("ACTIVE LIVENESS"));
        var challenges = new VerticalStackLayout();
        challenges.Add(new Label
        {
            Text = "Challenges — pool the sequence is drawn from",
            TextColor = Palette.Muted,
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 8),
        });
        foreach (var check in ActiveChecks.All)
        {
            challenges.Add(PoolSwitch(check));
        }
        challenges.Add(Choice("Challenges per attempt", ["1", "2", "3", "4", "5", "6"],
            Math.Clamp(_settings.Count - 1, 0, 5),
            index => _settings.Count = index + 1));
        challenges.Add(Toggle("Random order",
            "Shuffle the sequence on every attempt so a recording cannot be replayed",
            _settings.Randomize,
            value => _settings.Randomize = value));
        layout.Add(Group(challenges));

        layout.Add(Chrome.Section("ACTIVE LIVENESS TUNING"));
        var tuning = new VerticalStackLayout();
        tuning.Add(Stepped("Frames allowed per stage",
            "About 15 frames a second. Higher is more forgiving, lower fails faster (30–600)",
            _settings.FramesWait, 30, 600, 10, value => _settings.FramesWait = value));
        tuning.Add(Stepped("Head turn angle (°)", null,
            (int)_settings.Yaw, 5, 60, 1, value => _settings.Yaw = value));
        tuning.Add(Stepped("Head tilt angle (°)", null,
            (int)_settings.Pitch, 5, 60, 1, value => _settings.Pitch = value));
        tuning.Add(Threshold("Smile confidence", null,
            _settings.Smile, value => _settings.Smile = value));
        tuning.Add(Threshold("Eye-closed confidence", null,
            _settings.Blink, value => _settings.Blink = value));
        tuning.Add(Stepped("Blinks required", null,
            _settings.BlinksNumber, 1, 6, 1, value => _settings.BlinksNumber = value));
        tuning.Add(Threshold("Move-closer growth", null,
            _settings.Perspective, value => _settings.Perspective = value));
        tuning.Add(Stepped("Start alignment tolerance (°)", null,
            (int)_settings.AlignAngle, 1, 45, 1, value => _settings.AlignAngle = value));
        tuning.Add(Threshold("Minimum face size",
            "How much of the frame the face has to fill before it can be scored",
            _settings.MinFace, value => _settings.MinFace = value));
        tuning.Add(Threshold("Minimum image quality",
            "Below this the frame is called too dark or blurry to judge",
            _settings.Quality, value => _settings.Quality = value));
        tuning.Add(Toggle("Invert left / right",
            "Flip this if the turn challenges accept the opposite direction on your device",
            _settings.InvertTurn, value => _settings.InvertTurn = value));
        tuning.Add(Toggle("Invert up / down",
            "Flip this if the look-up and look-down challenges are the wrong way round",
            _settings.InvertTilt, value => _settings.InvertTilt = value));
        layout.Add(Group(tuning));

        layout.Add(Chrome.Section("RESET"));
        var reset = new Button
        {
            Text = "Restore default settings",
            TextColor = Palette.Accent,
            BackgroundColor = Palette.Surface,
            BorderColor = Palette.Stroke,
            BorderWidth = 1,
            CornerRadius = 20,
            HeightRequest = 52,
        };
        reset.Clicked += (_, _) =>
        {
            _settings.RestoreDefaults();
            // Rebuilding is the simplest way to show every control at its default.
            Content = Build();
        };
        layout.Add(reset);

        return new ScrollView { Content = layout };
    }

    static Border Group(View content) => Chrome.Card(content);

    View PoolSwitch(ActiveCheckInfo check)
    {
        var pool = new HashSet<ActiveCheckKey>(_settings.Pool);
        return Toggle(check.Title, check.Hint, pool.Contains(check.Key), value =>
        {
            var wanted = new HashSet<ActiveCheckKey>(_settings.Pool);
            if (value) wanted.Add(check.Key);
            else wanted.Remove(check.Key);
            // The native screen needs at least one challenge.
            if (wanted.Count == 0) return;
            _settings.Pool = [.. wanted];
        });
    }

    View Toggle(string title, string? note, bool value, Action<bool> onChange)
    {
        var toggle = new Switch
        {
            IsToggled = value,
            OnColor = Palette.Accent,
            ThumbColor = Palette.Text,
            VerticalOptions = LayoutOptions.Center,
        };
        toggle.Toggled += (_, e) => onChange(e.Value);

        var text = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center };
        text.Add(new Label { Text = title, TextColor = Palette.Text, FontSize = 15 });
        if (note is not null)
        {
            text.Add(new Label
            {
                Text = note,
                TextColor = Palette.Muted,
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 0),
            });
        }

        var grid = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
            ColumnSpacing = 12,
            Margin = new Thickness(0, 6),
        };
        grid.Add(text, 0, 0);
        grid.Add(toggle, 1, 0);
        return grid;
    }

    View Choice(string title, IReadOnlyList<string> options, int selected, Action<int> onChange)
    {
        var picker = new Picker
        {
            TextColor = Palette.Text,
            TitleColor = Palette.Muted,
            FontSize = 15,
        };
        foreach (var option in options) picker.Items.Add(option);
        picker.SelectedIndex = selected;
        picker.SelectedIndexChanged += (_, _) =>
        {
            if (picker.SelectedIndex >= 0) onChange(picker.SelectedIndex);
        };

        return new VerticalStackLayout
        {
            Margin = new Thickness(0, 6),
            Children =
            {
                new Label { Text = title, TextColor = Palette.Text, FontSize = 15 },
                picker,
            },
        };
    }

    /// <summary>A 0.0–1.0 threshold, the shape most engine settings take.</summary>
    View Threshold(string title, string? note, float value, Action<float> onChange) =>
        Slider(title, note, value, 0, 1, 0.05, v => onChange((float)v), v => v.ToString("0.00"));

    /// <summary>An integer setting with its own range.</summary>
    View Stepped(string title, string? note, int value, int min, int max, int step,
        Action<int> onChange) =>
        Slider(title, note, value, min, max, step, v => onChange((int)Math.Round(v)),
            v => ((int)Math.Round(v)).ToString());

    View Slider(
        string title,
        string? note,
        double value,
        double min,
        double max,
        double step,
        Action<double> onChange,
        Func<double, string> format)
    {
        var caption = new Label
        {
            Text = $"{title} — {format(value)}",
            TextColor = Palette.Text,
            FontSize = 15,
        };
        var slider = new Microsoft.Maui.Controls.Slider(min, max, Math.Clamp(value, min, max))
        {
            MinimumTrackColor = Palette.Accent,
            MaximumTrackColor = Palette.Stroke,
            ThumbColor = Palette.Accent,
        };
        slider.ValueChanged += (_, e) =>
        {
            // MAUI's slider is continuous; snap to the step the engine expects.
            var snapped = Math.Clamp(Math.Round(e.NewValue / step) * step, min, max);
            caption.Text = $"{title} — {format(snapped)}";
            onChange(snapped);
        };

        var layout = new VerticalStackLayout { Margin = new Thickness(0, 6) };
        layout.Add(caption);
        if (note is not null)
        {
            layout.Add(new Label
            {
                Text = note,
                TextColor = Palette.Muted,
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 0),
            });
        }
        layout.Add(slider);
        return layout;
    }
}
