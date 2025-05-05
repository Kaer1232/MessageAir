using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using МessageAir.Interfaces;
using МessageAir.Services;
using МessageAir.VIew;
using МessageAir.ViewModels;
using МessageAir.Views;

namespace МessageAir
{
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

            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = (Exception)args.ExceptionObject;
                HandleException(exception);
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                HandleException(args.Exception);
                args.SetObserved();
            };



            builder.Services
                .AddSingleton<IAuthService, AuthService>()  // Интерфейс + реализация
                .AddSingleton<AuthService>()                // Конкретный тип (на всякий случай)
                .AddSingleton<HttpClient>()
                .AddSingleton<IConnectivity>(Connectivity.Current);



            builder.Services.AddTransient<ChatViewModel>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<PrivateChatViewModel>();
            builder.Services.AddTransient<UsersViewModel>();

            builder.Services.AddTransient<LoginView>();
            builder.Services.AddTransient<ChatView>();
            builder.Services.AddTransient<PrivateChatView>();
            builder.Services.AddTransient<UsersView>();

            builder.Services.AddSingleton<AppShell>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
        private static void HandleException(Exception ex)
        {
            // Здесь можно добавить логирование
            Console.WriteLine($"Unhandled exception: {ex}");

            // Показываем пользователю сообщение об ошибке
            Application.Current?.Dispatcher.Dispatch(() =>
            {
                Application.Current.MainPage?.DisplayAlert("Error",
                    $"An unexpected error occurred: {ex.Message}", "OK");
            });
        }
    }
}
