using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SignalRChatServer.Models
{
    [Table("Messages")]
    public class MessageModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Sender { get; set; }

        [Required]// На сервере Sender
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsSystemMessage { get; set; }

        [Column(TypeName = "varbinary(max)")]
        public byte[] FileData { get; set; } = Array.Empty<byte>();

        [Column(TypeName = "nvarchar(max)")]
        public string FileName { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(max)")]
        public string FileType { get; set; } = string.Empty;

        [NotMapped]
        public bool HasFile => !string.IsNullOrEmpty(FileName) && FileData != null;
    }
}
