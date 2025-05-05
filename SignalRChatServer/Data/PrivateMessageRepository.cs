using Microsoft.EntityFrameworkCore;
using SignalRChatServer.Models;

namespace SignalRChatServer.Data
{
    public class PrivateMessageRepository: IPrivateMessageRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PrivateMessageRepository> _logger;

        public PrivateMessageRepository(AppDbContext context, ILogger<PrivateMessageRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddMessageAsync(PrivateMessageModel message)
        {
            try
            {
                message.Timestamp = DateTime.UtcNow;
                _context.PrivateMessages.Add(message);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Ошибка базы данных при сохранении сообщения");
                throw new Exception("Ошибка сохранения сообщения в базу данных");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении сообщения");
                throw;
            }
        }

        public async Task<IEnumerable<PrivateMessageModel>> GetConversationAsync(string userId1, string userId2)
        {
            return await _context.PrivateMessages
        .Include(m => m.FromUser)
        .Where(m => (m.FromUserId == userId1 && m.ToUserId == userId2) ||
                   (m.FromUserId == userId2 && m.ToUserId == userId1))
        .OrderBy(m => m.Timestamp)
        .Select(m => new PrivateMessageModel
        {
            FromUserId = m.FromUserId,
            FromUserName = m.FromUser.Username, // Загружаем имя
            ToUserId = m.ToUserId,
            Text = m.Text,
            Timestamp = m.Timestamp
        })
        .AsNoTracking()
        .ToListAsync();
        }

        public async Task<IEnumerable<PrivateMessageModel>> GetUserMessagesAsync(string userId)
        {
            try
            {
                return await _context.PrivateMessages
                    .Where(m => m.FromUserId == userId || m.ToUserId == userId)
                    .OrderByDescending(m => m.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<UserModel>> GetRecentContactsAsync(string userId, int count = 5)
        {
            try
            {
                var recentContacts = await _context.PrivateMessages
                    .Where(m => m.FromUserId == userId || m.ToUserId == userId)
                    .OrderByDescending(m => m.Timestamp)
                    .Select(m => m.FromUserId == userId ? m.ToUserId : m.FromUserId)
                    .Distinct()
                    .Take(count)
                    .ToListAsync();

                return await _context.Users
                    .Where(u => recentContacts.Contains(u.Id))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent contacts for user {UserId}", userId);
                throw;
            }
        }
    }
}
