using Microsoft.Maui.Controls;
using SecureFileManager.Maui.Services;
using SecureFileManager.Application.DTOs;

namespace SecureFileManager.Maui.Views;

public partial class FilesPage : ContentPage
{
    private readonly IApiClient _api;
    private readonly Guid _folderId;

    public FilesPage(Guid folderId, string folderName)
    {
        InitializeComponent();
        _api = App.Current.Services.GetService<IApiClient>();
        _folderId = folderId;
        FolderTitle.Text = folderName;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var files = await _api.GetFilesInFolderAsync(_folderId);
        FilesList.ItemsSource = files;
    }

    private async void OnUploadClicked(object sender, EventArgs e)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Select file to upload" });
        if (result == null) return;
        var file = await result.OpenReadAsync();
        var localPath = Path.Combine(FileSystem.CacheDirectory, result.FileName);
        using (var fs = File.Create(localPath))
        {
            await file.CopyToAsync(fs);
        }

        var prog = new Progress<double>(p => {
            // Optionally show progress
        });
        var uploadRes = await _api.UploadFileAsync(localPath, _folderId, prog);
        if (!uploadRes.Success) await DisplayAlert("Upload failed", uploadRes.Message ?? "", "OK");
        else await LoadAsync();
    }

    private async void OnDownloadClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Guid id)
        {
            var target = Path.Combine(FileSystem.CacheDirectory, $"download_{id}.bin");
            var prog = new Progress<double>(p => { /* show in UI */ });
            var res = await _api.DownloadFileAsync(id, target, prog);
            if (!res.Success) await DisplayAlert("Download failed", res.Message ?? "", "OK");
            else await Launcher.OpenAsync(new OpenFileRequest { File = new ReadOnlyFile(res.LocalPath) });
        }
    }
}
