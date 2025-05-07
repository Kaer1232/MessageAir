using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SignalRChatServer.Data;
using SignalRChatServer.Models;
using System.Diagnostics;

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
                throw new HubException("User not authenticated");

            return await _userRepository.GetAllUsersExceptAsync(currentUserId);
        }

        public async Task SendPrivateFile(
            string toUserId,
            string fileName,
            byte[] fileData,
            string fileType,
            string clientTempKey = null)
        {
            var fromUserId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(fromUserId))
                throw new HubException("User not authenticated");

            try
            {
                var fromUser = await _userRepository.GetByIdAsync(fromUserId);
                if (fromUser == null) throw new HubException("Sender not found");

                var message = new PrivateMessageModel
                {
                    FromUserId = fromUserId,
                    FromUserName = fromUser.Username,
                    ToUserId = toUserId,
                    FileName = fileName,
                    FileData = fileData,
                    FileType = fileType,
                    Text = $"[Файл: {fileName}]",
                    Timestamp = DateTime.UtcNow
                };

                await _messageRepository.AddMessageAsync(message);

                // Отправляем подтверждение с временным ключом
                await Clients.User(fromUserId).SendAsync(
                    "ReceivePrivateFileConfirmed",
                    message,
                    clientTempKey);

                // Отправляем получателю
                await Clients.User(toUserId).SendAsync("ReceivePrivateFile", message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "File send error");
                throw new HubException("File send failed: " + ex.Message);
            }
        }


        public async Task<IEnumerable<PrivateMessageModel>> GetConversation(string otherUserId)
        {
            var currentUserId = Context.UserIdentifier;
            return await _messageRepository.GetConversationAsync(currentUserId, otherUserId);
        }
    }
}
