using System.Collections.ObjectModel;
using МessageAir.Models;
using МessageAir.ViewModels;

namespace МessageAir.VIew;

public partial class ChatView : ContentPage
{
    [Obsolete]
    public ChatView(ChatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        viewModel.ScrollToBottom = () =>
        {
            // Небольшая задержка для стабилизации UI
            Device.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(300);

                if (viewModel.GroupedMessages is ObservableCollection<MessageGroup> groups && groups.Count > 0)
                {
                    var lastGroup = groups[groups.Count - 1];

                    if (lastGroup.Count > 0)
                    {
                        var lastMessage = lastGroup[lastGroup.Count - 1];
                        MessagesCollectionView.ScrollTo(
                            lastMessage,
                            lastGroup,
                            ScrollToPosition.End,
                            animate: true);
                    }
                }
            });
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ChatViewModel vm)
        {
            vm.InitializeConnection();
        }
    }

    private void HandleMessageCompleted(object sender, EventArgs e)
    {
        if (BindingContext is ChatViewModel viewModel && viewModel.SendMessageCommand.CanExecute(null))
        {
            viewModel.SendMessageCommand.Execute(null);
        }
    }
}