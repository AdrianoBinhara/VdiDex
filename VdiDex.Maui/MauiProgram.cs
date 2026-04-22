using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VdiDex.Maui.Interfaces;
using VdiDex.Maui.Models;
using VdiDex.Maui.Services;
using VdiDex.Maui.ViewModels;
using VdiDex.Maui.Views;

namespace VdiDex.Maui;

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
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        RegisterConfiguration(builder);
        RegisterServices(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void RegisterConfiguration(MauiAppBuilder builder)
    {
        using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();
        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        builder.Configuration.AddConfiguration(config);
        builder.Services.Configure<ApiSettings>(config.GetSection(ApiSettings.SectionName));
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IDexFileProvider, DexFileProvider>();

        services.AddHttpClient<IDexApiClient, DexApiClient>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainPage>();
    }
}
