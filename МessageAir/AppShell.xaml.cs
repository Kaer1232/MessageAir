using МessageAir.VIew;
using МessageAir.ViewModels;
using МessageAir.Views;

namespace МessageAir
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            RegisterRoutes();

        }

        private void RegisterRoutes()
        {
            Routing.RegisterRoute(nameof(LoginView), typeof(LoginView));
            Routing.RegisterRoute(nameof(ChatView), typeof(ChatView));
            Routing.RegisterRoute(nameof(UsersView), typeof(UsersView));
            Routing.RegisterRoute(nameof(PrivateChatView), typeof(PrivateChatView));
        }
    }
}
