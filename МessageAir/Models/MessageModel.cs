using CommunityToolkit.Mvvm.ComponentModel;

namespace МessageAir.Models
{
    public class MessageModel
    {
        public string User { get; set; }
        public string Sender { get; set; }
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsCurrentUser { get; set; }
    }
}
