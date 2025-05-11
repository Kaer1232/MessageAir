using Microsoft.EntityFrameworkCore;
using SignalRChatServer.Models;
using System.Diagnostics;

namespace SignalRChatServer.Data
{
    public class PrivateMessageRepository : IPrivateMessageRepository
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
                if (message.Timestamp == default)
                {
                    message.Timestamp = DateTime.UtcNow;
                }

                _context.PrivateMessages.Add(message);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding message");
                throw;
            }
        }

        public async Task AddFileMessageAsync(string fromUserId, string toUserId, string fileName, byte[] fileData, string fileType)
        {
            try
            {
                var fromUser = await _context.Users.FindAsync(fromUserId);
                if (fromUser == null)
                    throw new Exception("Sender user not found");

                var message = new PrivateMessageModel
                {
                    FromUserId = fromUserId,
                    ToUserId = toUserId,
                    FromUserName = fromUser.Username,
                    FileName = fileName,
                    FileData = fileData,
                    FileType = fileType,
                    Text = $"[Файл: {fileName}]",
                    Timestamp = DateTime.UtcNow
                };

                await _context.PrivateMessages.AddAsync(message);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file message");
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
            Id = m.Id,
            FromUserId = m.FromUserId,
            FromUserName = m.FromUser.Username, // Всегда получаем актуальное имя
            ToUserId = m.ToUserId,
            Text = m.IsDeleted ? "[Сообщение удалено]" : m.Text,
            Timestamp = m.Timestamp,
            IsDeleted = m.IsDeleted,
            FileData = m.IsDeleted ? null : m.FileData,
            FileName = m.IsDeleted ? null : m.FileName,
            FileType = m.IsDeleted ? null : m.FileType
        })
        .AsNoTracking()
        .ToListAsync();
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
                _logger.LogError(ex, "Error getting recent contacts");
                throw;
            }
        }

        public async Task<PrivateMessageModel> GetMessageByIdAsync(int id)
        {
            try
            {
                return await _context.PrivateMessages
                    .Include(m => m.FromUser)
                    .Include(m => m.ToUser)
                    .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting message by ID");
                throw;
            }
        }

        public async Task DeleteMessageAsync(int id)
        {
            try
            {
                var message = await _context.PrivateMessages.FindAsync(id);
                if (message == null)
                    throw new Exception("Message not found");

                message.IsDeleted = true;
                message.Text = "[Сообщение удалено]";
                message.FileData = null;
                message.FileName = null;
                message.FileType = null;

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message");
                throw;
            }
        }

        public async Task UpdateMessageAsync(PrivateMessageModel message)
        {
            try
            {
                var existingMessage = await _context.PrivateMessages.FindAsync(message.Id);
                if (existingMessage != null)
                {
                    _context.Entry(existingMessage).CurrentValues.SetValues(message);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating message");
                throw;
            }
        }
    }
}