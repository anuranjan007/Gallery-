using Microsoft.Maui.Controls;

namespace SecureFileManager.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new NavigationPage(new Views.LoginPage());
    }
}
