﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SignalRChatServer.Models
{
    [Table("PrivateMessages")]
    public class PrivateMessageModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [NotMapped] // Не сохраняется в БД
        public Guid ClientId { get; set; } = Guid.NewGuid();
        public string FromUserId { get; set; }
        public string FromUserName { get; set; } // Добавляем
        public string ToUserId { get; set; }
        public string? Text { get; set; }
        public DateTime Timestamp { get; set; }

        public byte[]? FileData { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey("FromUserId")]
        public virtual UserModel FromUser { get; set; }

        [ForeignKey("ToUserId")]
        public virtual UserModel ToUser { get; set; }
    }
}
