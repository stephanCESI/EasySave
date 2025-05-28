using EasySave.Maui.Localizations;
using EasySave.Maui.Logging;
using EasySave.Maui.Services;
using EasySave.Maui.Utils;
using EasySave.Maui.ViewModels;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

namespace EasySave.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Charger les paramètres
            var settings = AppSettings.Load();

            // Enregistrer le Logger avec le répertoire de logs et le type de fichier
            builder.Services.AddSingleton(sp =>
                new Logger(settings.LogDirectory, settings.LogFileType));

            // Enregistrer les autres services
            builder.Services.AddSingleton<LocalizationService>();

            // Enregistrer le BackupService en injectant le Logger et le LocalizationService
            builder.Services.AddSingleton(sp =>
                new BackupService(
                    sp.GetRequiredService<Logger>(),
                    sp.GetRequiredService<LocalizationService>()));

            // Enregistrer le ViewModel
            builder.Services.AddTransient<MainViewModel>();

            // Enregistrer la MainPage
            builder.Services.AddTransient<MainPage>();

            builder.Services.AddSingleton<WebSocketService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            var socketService = app.Services.GetRequiredService<WebSocketService>();
            socketService.StartServer();

            return app;
        }
    }
}
