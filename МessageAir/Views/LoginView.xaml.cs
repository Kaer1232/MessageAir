using МessageAir.ViewModels;

namespace МessageAir.VIew;

public partial class LoginView : ContentPage
{
	public LoginView(LoginViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}