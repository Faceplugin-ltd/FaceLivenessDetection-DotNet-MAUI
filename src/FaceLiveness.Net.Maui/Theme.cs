namespace FaceLiveness.Net.Maui;

/// <summary>
/// The native app's palette, from
/// <c>Face-LivenessDetection-Android/app/src/main/res/values/colors.xml</c>.
/// Kept as exact hex so the MAUI screens match the Android and iOS ones.
/// </summary>
public static class Palette
{
    public static readonly Color Bg = Color.FromArgb("#0B0B12");
    public static readonly Color Surface = Color.FromArgb("#14141D");
    public static readonly Color SurfaceAlt = Color.FromArgb("#1B1B27");
    public static readonly Color Text = Color.FromArgb("#F2F0F7");
    public static readonly Color Muted = Color.FromArgb("#9B96AC");
    public static readonly Color Accent = Color.FromArgb("#A48CFF");
    public static readonly Color AccentDim = Color.FromArgb("#2A2140");
    public static readonly Color Danger = Color.FromArgb("#FF6B6B");
    public static readonly Color Stroke = Color.FromArgb("#262635");

    public static readonly Color BrandViolet = Color.FromArgb("#7C5CFF");
    public static readonly Color BrandVioletDeep = Color.FromArgb("#4A2BB5");

    public static readonly Color StatusError = Color.FromArgb("#E5484D");
    public static readonly Color StatusOk = Color.FromArgb("#2EE6A6");
    public static readonly Color StatusInfo = Color.FromArgb("#5B8DEF");

    /// <summary>bg_hero.xml — the 135° gradient behind the home header.</summary>
    public static readonly Color HeroFrom = Color.FromArgb("#241B46");
    public static readonly Color HeroTo = Color.FromArgb("#101019");
}

/// <summary>The handful of shapes the native layouts repeat, as MAUI views.</summary>
public static class Chrome
{
    /// <summary>bg_card.xml / bg_tile_surface.xml — a surface card with a hairline stroke.</summary>
    public static Border Card(View content, double radius = 20, Thickness? padding = null) => new()
    {
        Content = content,
        Padding = padding ?? new Thickness(16),
        BackgroundColor = Palette.Surface,
        Stroke = Palette.Stroke,
        StrokeThickness = 1,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = radius },
    };

    /// <summary>A 135° gradient panel, as <c>bg_hero.xml</c> and the Active card draw it.</summary>
    public static Brush Gradient(Color from, Color to) => new LinearGradientBrush(
        [new GradientStop(from, 0f), new GradientStop(to, 1f)],
        new Point(0, 0),
        new Point(1, 1));

    /// <summary>A section caption: small, spaced, muted — the native <c>textAllCaps</c> label.</summary>
    public static Label Section(string text) => new()
    {
        Text = text,
        TextColor = Palette.Muted,
        FontSize = 12,
        FontAttributes = FontAttributes.Bold,
        CharacterSpacing = 0.6,
        Margin = new Thickness(0, 22, 0, 12),
    };

    /// <summary>The status pill: a rounded chip tinted by the state it reports.</summary>
    public static Border Pill(Label content, Color tint) => new()
    {
        Content = content,
        Padding = new Thickness(14, 8),
        BackgroundColor = tint.WithAlpha(0.15f),
        Stroke = tint.WithAlpha(0.4f),
        StrokeThickness = 1,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 999 },
        HorizontalOptions = LayoutOptions.Start,
    };

    /// <summary>The round glyph bubble each row and tile leads with.</summary>
    public static Border Bubble(string glyph, Color background, Color foreground) => new()
    {
        Content = new Label
        {
            Text = glyph,
            TextColor = foreground,
            FontSize = 20,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        },
        WidthRequest = 44,
        HeightRequest = 44,
        BackgroundColor = background,
        StrokeThickness = 0,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 22 },
    };
}
