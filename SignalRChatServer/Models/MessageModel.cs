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
        public string Sender { get; set; }  // На сервере Sender
        public string Text { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsSystemMessage { get; set; }
    }
}
