using SignalRChatServer.Models;

namespace SignalRChatServer.Data
{
    public interface IPrivateMessageRepository
    {
        Task AddMessageAsync(PrivateMessageModel message);
        Task AddFileMessageAsync(string fromUserId, string toUserId, string fileName, byte[] fileData, string fileType);
        Task<IEnumerable<PrivateMessageModel>> GetConversationAsync(string userId1, string userId2);
        Task<IEnumerable<UserModel>> GetRecentContactsAsync(string userId, int count = 5);
        Task<PrivateMessageModel> GetMessageByIdAsync(int id);
        Task DeleteMessageAsync(int id);
        Task UpdateMessageAsync(PrivateMessageModel message);
    }
}