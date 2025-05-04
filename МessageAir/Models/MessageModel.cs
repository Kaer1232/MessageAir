using CommunityToolkit.Mvvm.ComponentModel;

namespace МessageAir.Models
{
    public class MessageModel: ObservableObject
    {
        public string Sender { get; set; }  // На клиенте используем User
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsCurrentUser { get; set; }
        public string FileName { get; set; } // Новое поле
        public byte[] FileData { get; set; } // Новое поле
        public string FileType { get; set; } // Новое поле (например: "image/jpeg")
        public string DateGroup => Timestamp.Date.ToString("yyyy-MM-dd");

        public bool HasFile => !string.IsNullOrEmpty(FileName) && FileData != null;
        public string FileExtension => Path.GetExtension(FileName)?.ToUpper().TrimStart('.');
        public string ShortFileName => Path.GetFileName(FileName);
        public long FileSize => FileData?.LongLength ?? 0;
    }
}
