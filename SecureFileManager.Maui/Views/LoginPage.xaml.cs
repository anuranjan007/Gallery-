using Microsoft.Maui.Controls;
using SecureFileManager.Maui.Services;

namespace SecureFileManager.Maui.Views;

public partial class LoginPage : ContentPage
{
    private readonly IApiClient _api;

    public LoginPage()
    {
        InitializeComponent();
        _api = App.Current.Services.GetService<IApiClient>();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var user = UsernameEntry.Text ?? string.Empty;
        var pass = PasswordEntry.Text ?? string.Empty;
        var res = await _api.LoginAsync(user, pass);
        if (res.Success)
        {
            await Navigation.PushAsync(new Views.DashboardPage());
        }
        else
        {
            await DisplayAlert("Login failed", res.Message ?? "Invalid credentials", "OK");
        }
    }

    private async void OnOtpClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Views.OtpPage());
    }
}
