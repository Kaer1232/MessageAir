using System.Diagnostics;
using МessageAir.Services;
using МessageAir.ViewModels;

namespace МessageAir.VIew;

public partial class LoginView : ContentPage
{
    private AuthService _authService;

    public LoginView(LoginViewModel viewModel, AuthService authService)
	{
		InitializeComponent();
        BindingContext = viewModel;
        _authService = authService;
        InitializeAsync();
    }
    private async Task InitializeAsync()
    {
        await _authService.InitializeAsync();
        Debug.WriteLine($"Initialized username: {_authService.Username}");
    }
}