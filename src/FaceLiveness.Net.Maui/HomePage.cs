namespace FaceLiveness.Net.Maui;

/// <summary>
/// Home: engine status, the two liveness demos, settings and about.
/// <para>
/// Mirrors Android <c>ui/MainActivity</c> + <c>activity_main.xml</c> and iOS
/// <c>ViewController</c> — hero header with the status pill, an Active-liveness and a
/// Passive-liveness card, Settings and About tiles, and a row that opens the integration guide.
/// </para>
/// </summary>
public class HomePage : ContentPage
{
    readonly Label _status = new()
    {
        Text = "Loading on-device models…",
        TextColor = Palette.StatusInfo,
        FontSize = 13,
        FontAttributes = FontAttributes.Bold,
    };

    Border _statusPill = null!;
    Border _activeCard = null!;
    Border _passiveCard = null!;
    bool _livenessAllowed;
    bool _started;

    public HomePage()
    {
        Title = "Face Liveness";
        BackgroundColor = Palette.Bg;
        NavigationPage.SetHasNavigationBar(this, false);
        Content = BuildContent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started) return;
        _started = true;
        await StartEngineAsync();
    }

    async Task StartEngineAsync()
    {
        try
        {
            await FaceLivenessEngine.StartAsync(LicenseKeys.Demo);
            var message = await FaceLivenessEngine.StatusMessageAsync();
            _livenessAllowed = FaceLivenessEngine.License.Liveness;
            SetStatus(message, FaceLivenessEngine.IsReady ? Palette.StatusOk : Palette.StatusError);
        }
        catch (Exception error)
        {
            // A platform with no liveness build says so rather than looking broken.
            SetStatus(error.Message, Palette.StatusError);
        }
        _activeCard.Opacity = _livenessAllowed ? 1 : 0.45;
        _passiveCard.Opacity = _livenessAllowed ? 1 : 0.45;
    }

    void SetStatus(string message, Color tint)
    {
        _status.Text = message;
        _status.TextColor = tint;
        _statusPill.BackgroundColor = tint.WithAlpha(0.15f);
        _statusPill.Stroke = tint.WithAlpha(0.4f);
    }

    View BuildContent()
    {
        _statusPill = Chrome.Pill(_status, Palette.StatusInfo);

        var hero = new Border
        {
            Padding = new Thickness(20, 22),
            Background = Chrome.Gradient(Palette.HeroFrom, Palette.HeroTo),
            Stroke = Palette.Stroke,
            StrokeThickness = 1,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 28 },
            Content = new VerticalStackLayout
            {
                Children =
                {
                    new Image
                    {
                        Source = "fp_logo.png",
                        HeightRequest = 40,
                        HorizontalOptions = LayoutOptions.Start,
                        Aspect = Aspect.AspectFit,
                    },
                    new Label
                    {
                        Text = "On-device face liveness",
                        TextColor = Palette.Text,
                        FontSize = 26,
                        FontAttributes = FontAttributes.Bold,
                        Margin = new Thickness(0, 16, 0, 0),
                    },
                    new Label
                    {
                        Text = "Passive anti-spoofing and guided active checks. Every frame is "
                             + "processed on this phone — no biometric data leaves the device.",
                        TextColor = Palette.Muted,
                        FontSize = 14,
                        Margin = new Thickness(0, 10, 0, 14),
                    },
                    _statusPill,
                },
            },
        };

        _activeCard = Tile(
            glyph: "⚡",
            title: "Active liveness",
            subtitle: "Anti-spoofing gate, then blink, smile, turn your head",
            background: Chrome.Gradient(Palette.BrandViolet, Palette.BrandVioletDeep),
            titleColor: Colors.White,
            subtitleColor: Colors.White.WithAlpha(0.85f),
            bubbleBackground: Colors.White.WithAlpha(0.18f),
            bubbleForeground: Colors.White,
            onTap: OpenActiveAsync);

        _passiveCard = Tile(
            glyph: "◎",
            title: "Passive liveness",
            subtitle: "Single-frame anti-spoofing with a live score HUD",
            background: new SolidColorBrush(Palette.Surface),
            titleColor: Palette.Text,
            subtitleColor: Palette.Muted,
            bubbleBackground: Palette.AccentDim,
            bubbleForeground: Palette.Accent,
            onTap: OpenPassiveAsync);

        var more = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)],
            ColumnSpacing = 12,
        };
        more.Add(SmallTile("⚙", "Settings", "Camera, thresholds, challenge sequence",
            () => Navigation.PushAsync(new SettingsPage())), 0, 0);
        more.Add(SmallTile("i", "About", "SDK, license and contact",
            () => Navigation.PushAsync(new AboutPage())), 1, 0);

        var guide = Chrome.Card(new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            ],
            ColumnSpacing = 14,
            Children =
            {
                Cell(new Label { Text = "▤", TextColor = Palette.Accent, FontSize = 20, VerticalTextAlignment = TextAlignment.Center }, 0),
                Cell(new Label { Text = "Open the integration guide", TextColor = Palette.Text, FontSize = 15, VerticalTextAlignment = TextAlignment.Center }, 1),
                Cell(new Label { Text = "›", TextColor = Palette.Muted, FontSize = 24, VerticalTextAlignment = TextAlignment.Center }, 2),
            },
        });
        guide.Margin = new Thickness(0, 12, 0, 0);
        Tap(guide, () => Launcher.OpenAsync("https://doc.faceplugin.com"));

        return new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 16, 16, 28),
                Children =
                {
                    hero,
                    Chrome.Section("LIVENESS CHECKS"),
                    _activeCard,
                    _passiveCard,
                    Chrome.Section("MORE"),
                    more,
                    guide,
                },
            },
        };
    }

    static View Cell(View view, int column)
    {
        Grid.SetColumn(view, column);
        return view;
    }

    Border Tile(
        string glyph,
        string title,
        string subtitle,
        Brush background,
        Color titleColor,
        Color subtitleColor,
        Color bubbleBackground,
        Color bubbleForeground,
        Func<Task> onTap)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            ],
            ColumnSpacing = 14,
            VerticalOptions = LayoutOptions.Center,
        };
        grid.Add(Chrome.Bubble(glyph, bubbleBackground, bubbleForeground), 0, 0);
        grid.Add(new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = title, TextColor = titleColor, FontSize = 18, FontAttributes = FontAttributes.Bold },
                new Label { Text = subtitle, TextColor = subtitleColor, FontSize = 13, Margin = new Thickness(0, 4, 0, 0) },
            },
        }, 1, 0);
        grid.Add(new Label
        {
            Text = "›",
            TextColor = titleColor,
            FontSize = 24,
            VerticalTextAlignment = TextAlignment.Center,
        }, 2, 0);

        var card = new Border
        {
            Content = grid,
            Padding = new Thickness(16, 18),
            Margin = new Thickness(0, 0, 0, 12),
            Background = background,
            Stroke = background is SolidColorBrush ? Palette.Stroke : Colors.Transparent,
            StrokeThickness = background is SolidColorBrush ? 1 : 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
        };
        Tap(card, async () => await onTap());
        return card;
    }

    Border SmallTile(string glyph, string title, string subtitle, Func<Task> onTap)
    {
        var card = Chrome.Card(new VerticalStackLayout
        {
            Children =
            {
                Chrome.Bubble(glyph, Palette.AccentDim, Palette.Accent),
                new Label
                {
                    Text = title,
                    TextColor = Palette.Text,
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    Margin = new Thickness(0, 10, 0, 0),
                },
                new Label
                {
                    Text = subtitle,
                    TextColor = Palette.Muted,
                    FontSize = 12,
                    Margin = new Thickness(0, 4, 0, 0),
                },
            },
        }, radius: 24);
        Tap(card, async () => await onTap());
        return card;
    }

    static void Tap(View view, Func<Task> onTap)
    {
        var gesture = new TapGestureRecognizer();
        gesture.Tapped += async (_, _) => await onTap();
        view.GestureRecognizers.Add(gesture);
    }

    bool EnsureAllowed()
    {
        if (!FaceLivenessEngine.IsReady)
        {
            Toast("SDK not ready");
            return false;
        }
        if (!_livenessAllowed)
        {
            Toast("Liveness is not available on this license");
            return false;
        }
        return true;
    }

    async Task OpenActiveAsync()
    {
        if (!EnsureAllowed()) return;
        var result = await FaceLivenessScreens.StartActiveLivenessAsync(
            SettingsStore.Current.ToOptions());
        // The demo only reports the verdict; a real integration would act on it.
        if (result is not null) Toast(LivenessText.Describe(result));
    }

    async Task OpenPassiveAsync()
    {
        if (!EnsureAllowed()) return;
        await FaceLivenessScreens.StartPassiveLivenessAsync(
            SettingsStore.Current.UseFrontCamera,
            SettingsStore.Current.LivenessThreshold);
    }

    void Toast(string message) => _ = DisplayAlert("Face Liveness", message, "OK");
}
