using Microsoft.EntityFrameworkCore;
using SignalRChatServer.Models;
using System.Diagnostics;

namespace SignalRChatServer.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _context;

        public MessageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddMessageAsync(MessageModel message)
        {
            if (message.Timestamp == default)
            {
                message.Timestamp = DateTime.UtcNow;
            }

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<MessageModel>> GetRecentMessagesAsync(int count = 50)
        {
            return await _context.Messages
        .OrderByDescending(m => m.Timestamp)
        .Take(count)
        .Select(m => new MessageModel
        {
            Id = m.Id,
            Sender = m.Sender,
            Text = m.Text ?? string.Empty,
            Timestamp = m.Timestamp,
            IsSystemMessage = m.IsSystemMessage,
            FileName = m.FileName ?? string.Empty,
            FileData = m.FileData ?? Array.Empty<byte>(),
            FileType = m.FileType ?? string.Empty
        })
        .AsNoTracking()
        .ToListAsync();
        }

        public async Task<int> PurgeAllMessagesAsync()
        {
            // Вариант 1: через EF Core
            var count = await _context.Messages.ExecuteDeleteAsync();
            return count;
        }

        public async Task AddFileMessageAsync(string sender, string fileName, byte[] fileData, string fileType)
        {
            var message = new MessageModel
            {
                Sender = sender,
                Text = "[Файл]",
                FileName = fileName,
                FileData = fileData,
                FileType = fileType,
                Timestamp = DateTime.UtcNow,
                IsSystemMessage = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }
    }
}
