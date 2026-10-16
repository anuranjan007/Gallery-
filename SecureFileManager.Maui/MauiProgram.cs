using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using SecureFileManager.Maui.Services;

namespace SecureFileManager.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // load appsettings
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        var apiBase = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:5001/";

        // Services
        builder.Services.AddSingleton<IApiClient, HttpApiClient>();
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();

        // HttpClient with token handler
        builder.Services.AddSingleton<TokenHandler>();
        builder.Services.AddHttpClient<HttpApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(100);
        }).AddHttpMessageHandler<TokenHandler>();

        return builder.Build();
    }
}
