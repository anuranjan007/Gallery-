using Microsoft.Maui.Controls;
using SecureFileManager.Maui.Services;

namespace SecureFileManager.Maui.Views;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
        BindingContext = App.Current.Services.GetService<DashboardViewModel>();
    }
}
