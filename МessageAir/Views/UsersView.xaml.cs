using МessageAir.ViewModels;

namespace МessageAir.Views;

public partial class UsersView : ContentPage
{
	public UsersView(UsersViewModel usersViewModel)
	{
		InitializeComponent();
		BindingContext = usersViewModel;
	}
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is UsersViewModel vm)
        {
            // При каждом появлении страницы проверяем подключение
            _ = vm.InitializeHub();
        }
    }
}