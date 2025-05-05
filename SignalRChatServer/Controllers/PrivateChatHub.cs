using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SignalRChatServer.Data;
using SignalRChatServer.Models;

namespace SignalRChatServer.Controllers
{
    [Authorize]
    public class PrivateChatHub: Hub
    {
        private readonly IPrivateMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;

        public PrivateChatHub(IPrivateMessageRepository messageRepo, IUserRepository userRepo)
        {
            _messageRepository = messageRepo;
            _userRepository = userRepo;
        }

        public async Task SendPrivateMessage( string toUserId, string message)
        {

            try
            {
                var fromUserId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(fromUserId))
                    throw new HubException("User not authenticated");

                var fromUser = await _userRepository.GetByIdAsync(fromUserId);
                var toUser = await _userRepository.GetByIdAsync(toUserId);

                if (fromUser == null || toUser == null)
                    throw new HubException("User not found");

                var messageModel = new PrivateMessageModel
                {
                    FromUserId = fromUserId,
                    FromUserName = fromUser.Username,
                    ToUserId = toUserId,
                    Text = message,
                    Timestamp = DateTime.UtcNow
                };

                await _messageRepository.AddMessageAsync(messageModel);

                await Clients.User(fromUserId).SendAsync("ReceivePrivateMessage", messageModel);
                await Clients.User(toUserId).SendAsync("ReceivePrivateMessage", messageModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке сообщения: {ex}");
                throw new HubException($"Не удалось отправить сообщение: {ex.Message}");
            }
        }

        public async Task<IEnumerable<UserModel>> GetAvailableUsers()
        {
            var currentUserId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new HubException("User not authenticated");
            }

            try
            {
                return await _userRepository.GetAllUsersExceptAsync(currentUserId);
            }
            catch (Exception ex)
            {
                throw new HubException("Error getting users", ex);
            }
        }


        public async Task<IEnumerable<PrivateMessageModel>> GetConversation(string otherUserId)
        {
            var currentUserId = Context.UserIdentifier;
            return await _messageRepository.GetConversationAsync(currentUserId, otherUserId);
        }
    }
}
