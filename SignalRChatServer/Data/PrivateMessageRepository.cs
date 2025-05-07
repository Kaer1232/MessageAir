using Microsoft.EntityFrameworkCore;
using SignalRChatServer.Models;
using System.Diagnostics;

namespace SignalRChatServer.Data
{
    public class PrivateMessageRepository : IPrivateMessageRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PrivateMessageRepository> Degug;

        public PrivateMessageRepository(AppDbContext context, ILogger<PrivateMessageRepository> logger)
        {
            _context = context;
            Degug = logger;
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
            catch (DbUpdateException dbEx)
            {
                Degug.LogError(dbEx, "Database error saving message");
                throw new Exception("Error saving message to database");
            }
            catch (Exception ex)
            {
                Degug.LogError(ex, "Error adding message");
                throw;
            }
        }

        public async Task AddFileMessageAsync(
    string fromUserId,
    string toUserId,
    string fileName,
    byte[] fileData,
    string fileType)
        {
            try
            {
                // Получаем имя отправителя из базы данных
                var fromUser = await _context.Users.FindAsync(fromUserId);
                if (fromUser == null)
                    throw new Exception("Sender user not found");

                var message = new PrivateMessageModel
                {
                    FromUserId = fromUserId,
                    ToUserId = toUserId,
                    FromUserName = fromUser.Username, // Устанавливаем имя пользователя
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
                Debug.WriteLine(ex, "Error saving file message");
                throw new Exception("Database operation failed: " + ex.Message);
            }
        }

        public async Task<IEnumerable<PrivateMessageModel>> GetConversationAsync(string userId1, string userId2)
        {
            try
            {
                return await _context.PrivateMessages
                    .Include(m => m.FromUser)
                    .Where(m => (m.FromUserId == userId1 && m.ToUserId == userId2) ||
                               (m.FromUserId == userId2 && m.ToUserId == userId1))
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new PrivateMessageModel
                    {
                        FromUserId = m.FromUserId,
                        FromUserName = m.FromUser.Username,
                        ToUserId = m.ToUserId,
                        Text = m.Text,
                        FileName = m.FileName,
                        FileData = m.FileData,
                        FileType = m.FileType,
                        Timestamp = m.Timestamp
                    })
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Degug.LogError(ex, "Error getting conversation");
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
                Degug.LogError(ex, "Error getting recent contacts");
                throw;
            }
        }
    }
}