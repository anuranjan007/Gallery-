using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SecureFileManager.Maui.Services;
using SecureFileManager.Application.DTOs;

namespace SecureFileManager.Maui;

public class DashboardViewModel : BindableObject
{
    private readonly IApiClient _api;

    public ObservableCollection<FolderDto> Folders { get; } = new();

    public DashboardViewModel(IApiClient api)
    {
        _api = api;
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        var folders = await _api.GetRootFoldersAsync();
        Folders.Clear();
        foreach (var f in folders)
            Folders.Add(f);
    }
}
