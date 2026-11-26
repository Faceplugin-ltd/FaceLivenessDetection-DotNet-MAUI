namespace FaceLiveness.Net.Maui;

/// <summary>
/// About — company, product, what the licence unlocks, and contact.
/// <para>Mirrors Android <c>ui/AboutActivity</c> and iOS <c>AboutViewController</c>.</para>
/// </summary>
public class AboutPage : ContentPage
{
    public AboutPage()
    {
        Title = "About";
        BackgroundColor = Palette.Bg;

        var license = FaceLivenessEngine.License;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16, 24, 16, 28),
                Children =
                {
                    new Image
                    {
                        Source = "fp_logo.png",
                        HeightRequest = 44,
                        Aspect = Aspect.AspectFit,
                    },
                    new Label
                    {
                        Text = "FacePlugin",
                        TextColor = Palette.Text,
                        FontSize = 22,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalTextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 0),
                    },
                    new Label
                    {
                        Text = "Face Liveness SDK",
                        TextColor = Palette.Accent,
                        FontSize = 15,
                        HorizontalTextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 4, 0, 16),
                    },
                    Card($"License: {license.Label}", center: true, size: 13),
                    Card(
                        "FacePlugin builds on-device identity technology — face recognition, "
                        + "liveness, and document reading — so biometric data never has to leave "
                        + "the phone."),
                    Card(
                        "This app demos face liveness on .NET MAUI: passive anti-spoofing from a "
                        + "single frame, and active liveness as an anti-spoofing gate followed by "
                        + "a guided challenge sequence. Both screens are the native FacePlugin "
                        + "implementations, and everything runs fully on-device."),
                    Link("faceplugin.com", "https://faceplugin.com"),
                    new Label
                    {
                        Text = "© 2026 FacePlugin. All rights reserved.",
                        TextColor = Palette.Muted,
                        FontSize = 12,
                        HorizontalTextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 8, 0, 0),
                    },
                },
            },
        };
    }

    static Border Card(string text, bool center = false, double size = 14)
    {
        var card = Chrome.Card(new Label
        {
            Text = text,
            TextColor = Palette.Text,
            FontSize = size,
            HorizontalTextAlignment = center ? TextAlignment.Center : TextAlignment.Start,
        });
        card.Margin = new Thickness(0, 0, 0, 16);
        return card;
    }

    static Label Link(string text, string url)
    {
        var label = new Label
        {
            Text = text,
            TextColor = Palette.Accent,
            FontSize = 14,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0),
        };
        var gesture = new TapGestureRecognizer();
        gesture.Tapped += async (_, _) => await Launcher.OpenAsync(url);
        label.GestureRecognizers.Add(gesture);
        return label;
    }
}
