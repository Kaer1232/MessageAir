using CommunityToolkit.Mvvm.ComponentModel;

namespace МessageAir.Models
{
    public class MessageModel: ObservableObject
    {
        public string Sender { get; set; }  // На клиенте используем User
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsCurrentUser { get; set; }
        public string DateGroup => Timestamp.Date.ToString("yyyy-MM-dd");
    }
}
