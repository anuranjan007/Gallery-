using SecureFileManager.Maui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace SecureFileManager.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new NavigationPage(new Views.LoginPage());
    }
}
