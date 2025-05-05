using МessageAir.ViewModels;

namespace МessageAir.Views;

public partial class PrivateChatView : ContentPage
{
    public PrivateChatView(PrivateChatViewModel privateChatViewModel)
    {
        InitializeComponent();
        BindingContext = privateChatViewModel;
    }
}