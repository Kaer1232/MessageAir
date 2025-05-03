using System.Diagnostics;
using МessageAir.VIew;

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
        }
    }
}
