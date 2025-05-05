using System.ComponentModel.DataAnnotations.Schema;

namespace МessageAir.Models
{
    [Table("PrivateMessages")]
    public class PrivateMessageModel
    {
        public int Id { get; set; }
        public string FromUserId { get; set; }
        public string ToUserId { get; set; }
        public string FromUserName { get; set; }
        public string? Text { get; set; }
        public DateTime Timestamp { get; set; }
        public byte[]? FileData { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public string DateGroup => Timestamp.Date.ToString("yyyy-MM-dd");

        [NotMapped]
        public bool IsCurrentUser { get; set; }

        public bool HasFile => !string.IsNullOrEmpty(FileName) && FileData != null;
        public string FileExtension => Path.GetExtension(FileName)?.ToUpper().TrimStart('.');
        public string ShortFileName => Path.GetFileName(FileName);
        public long FileSize => FileData?.LongLength ?? 0;
    }
}
