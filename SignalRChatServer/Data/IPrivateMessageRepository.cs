using SignalRChatServer.Models;

namespace SignalRChatServer.Data
{
    public interface IPrivateMessageRepository
    {
        Task AddMessageAsync(PrivateMessageModel message);
        Task<IEnumerable<PrivateMessageModel>> GetConversationAsync(string userId1, string userId2);
        Task<IEnumerable<PrivateMessageModel>> GetUserMessagesAsync(string userId);
        Task<IEnumerable<UserModel>> GetRecentContactsAsync(string userId, int count = 5);
    }
}
