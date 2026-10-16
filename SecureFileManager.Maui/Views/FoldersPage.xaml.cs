using Microsoft.Maui.Controls;
using SecureFileManager.Maui.Services;
using SecureFileManager.Application.DTOs;

namespace SecureFileManager.Maui.Views;

public partial class FoldersPage : ContentPage
{
    private readonly IApiClient _api;

    public FoldersPage()
    {
        InitializeComponent();
        _api = App.Current.Services.GetService<IApiClient>();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var folders = await _api.GetRootFoldersAsync();
        FoldersList.ItemsSource = folders;
    }

    private async void OnNewFolderClicked(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("New Folder", "Folder name:");
        if (string.IsNullOrWhiteSpace(name)) return;
        var res = await _api.CreateFolderAsync(name, null);
        if (!res.Success) await DisplayAlert("Error", res.Message ?? "Failed to create folder", "OK");
        else await LoadAsync();
    }

    private async void OnFolderSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is FolderDto f)
        {
            await Navigation.PushAsync(new FilesPage(f.Id, f.Name));
        }
        ((CollectionView)sender).SelectedItem = null;
    }
}
