using Microsoft.Maui.Controls;
using SecureFileManager.Maui.Services;

namespace SecureFileManager.Maui.Views;

public partial class OtpPage : ContentPage
{
    private readonly IApiClient _api;
    public OtpPage()
    {
        InitializeComponent();
        _api = App.Current.Services.GetService<IApiClient>();
    }

    private async void OnVerifyClicked(object sender, EventArgs e)
    {
        var code = OtpEntry.Text ?? string.Empty;
        var res = await _api.VerifyOtpAsync("user@example.com", code);
        if (res.Success)
        {
            await Navigation.PushAsync(new Views.DashboardPage());
        }
        else
        {
            await DisplayAlert("OTP failed", res.Message ?? "Invalid code", "OK");
        }
    }
}
