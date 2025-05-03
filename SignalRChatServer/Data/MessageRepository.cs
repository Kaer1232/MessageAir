using Microsoft.EntityFrameworkCore;
using SignalRChatServer.Models;

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
    .Take(50)
    .ToListAsync();
        }

        public async Task<int> PurgeAllMessagesAsync()
        {
            // Вариант 1: через EF Core
            var count = await _context.Messages.ExecuteDeleteAsync();
            return count;
        }
    }
}
