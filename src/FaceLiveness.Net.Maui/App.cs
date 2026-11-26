namespace FaceLiveness.Net.Maui;

public class App : Application
{
    public App()
    {
        MainPage = new NavigationPage(new HomePage())
        {
            BarBackgroundColor = Palette.Bg,
            BarTextColor = Palette.Text,
        };
    }
}
