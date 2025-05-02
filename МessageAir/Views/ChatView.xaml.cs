using МessageAir.ViewModels;

namespace МessageAir.VIew;

public partial class ChatView : ContentPage
{
    public ChatView(ChatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}