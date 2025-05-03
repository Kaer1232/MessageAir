using SignalRChatServer.Models;

namespace SignalRChatServer.Data
{
    public interface IMessageRepository
    {
        Task AddMessageAsync(MessageModel message);
        Task<IEnumerable<MessageModel>> GetRecentMessagesAsync(int count = 50);
        Task<int> PurgeAllMessagesAsync();
    }
}
