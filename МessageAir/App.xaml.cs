using МessageAir.Services;
using МessageAir.VIew;
using МessageAir.Views;

namespace МessageAir
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

//            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
//            {
//                Exception ex = (Exception)args.ExceptionObject;
//                Console.WriteLine($"Unhandled exception: {ex}");
//            };
//#if DEBUG
//            // Только для отладки без прерываний
//            System.Diagnostics.Debugger.Launch();
//#endif




            Routing.RegisterRoute(nameof(LoginView), typeof(LoginView));
            Routing.RegisterRoute(nameof(ChatView), typeof(ChatView));
            Routing.RegisterRoute(nameof(PrivateChatView), typeof(PrivateChatView));
            Routing.RegisterRoute(nameof(UsersView), typeof(UsersView));

            MainPage = new AppShell();
        }
        protected override async void OnStart()
        {
            var authService = Handler.MauiContext.Services.GetService<AuthService>();

            if (!string.IsNullOrEmpty(authService.Token))
            {
                await Shell.Current.GoToAsync("//ChatView");
            }
            // Убедитесь, что Debugger.Break() не срабатывает
            base.OnStart();
        }
    }
}
