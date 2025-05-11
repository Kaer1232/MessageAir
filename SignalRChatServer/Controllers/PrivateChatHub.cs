using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SignalRChatServer.Data;
using SignalRChatServer.Models;
using System.Diagnostics;

namespace SignalRChatServer.Controllers
{
    [Authorize]
    public class PrivateChatHub : Hub
    {
        private readonly IPrivateMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;

        public PrivateChatHub(IPrivateMessageRepository messageRepo, IUserRepository userRepo)
        {
            _messageRepository = messageRepo;
            _userRepository = userRepo;
        }

        public async Task SendPrivateMessage(string toUserId, string message)
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
                Debug.WriteLine($"Ошибка при отправке сообщения: {ex}");
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
    string fileType)
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

                // Отправляем только получателю
                await Clients.User(toUserId).SendAsync("ReceivePrivateFile", message);

                // Возвращаем сообщение отправителю через специальный метод
                await Clients.Caller.SendAsync("ReceiveOwnFile", message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "File send error");
                throw new HubException("File send failed: " + ex.Message);
            }
        }

        public async Task DeleteMessage(int messageId)
        {
            var currentUserId = Context.UserIdentifier;
            var message = await _messageRepository.GetMessageByIdAsync(messageId);

            if (message == null) throw new HubException("Message not found");
            if (message.FromUserId != currentUserId) throw new HubException("You can only delete your own messages");

            // Сохраняем имя отправителя перед удалением
            string senderName = message.FromUserName;

            message.Text = "[Сообщение удалено]";
            message.IsDeleted = true;
            message.FileData = null;
            message.FileName = null;
            message.FileType = null;

            await _messageRepository.UpdateMessageAsync(message);

            // Отправляем обновление с сохранением имени отправителя
            var updatedMessage = new PrivateMessageModel
            {
                Id = message.Id,
                FromUserId = message.FromUserId,
                FromUserName = senderName, // Сохраняем оригинальное имя
                ToUserId = message.ToUserId,
                Text = message.Text,
                Timestamp = message.Timestamp,
                IsDeleted = true
            };

            await Clients.User(message.FromUserId).SendAsync("MessageDeleted", updatedMessage);
            await Clients.User(message.ToUserId).SendAsync("MessageDeleted", updatedMessage);
        }

        public async Task UpdateMessage(int messageId, string newText)
        {
            try
            {
                var currentUserId = Context.UserIdentifier;
                var message = await _messageRepository.GetMessageByIdAsync(messageId);

                if (message == null) throw new HubException("Message not found");
                if (message.FromUserId != currentUserId) throw new HubException("Not authorized to edit this message");
                if (message.FileData != null) throw new HubException("Cannot edit file messages");
                if (message.IsDeleted) throw new HubException("Cannot edit deleted message");

                message.Text = newText;
                message.IsEdited = true;
                message.Timestamp = DateTime.UtcNow;

                await _messageRepository.UpdateMessageAsync(message);

                // Уведомляем обоих пользователей об изменении
                await Clients.User(message.FromUserId).SendAsync("MessageUpdated", message);
                await Clients.User(message.ToUserId).SendAsync("MessageUpdated", message);
            }
            catch (Exception ex)
            {
                throw new HubException($"Failed to update message: {ex.Message}");
            }
        }

        public async Task<IEnumerable<PrivateMessageModel>> GetConversation(string otherUserId)
        {
            var currentUserId = Context.UserIdentifier;
            return await _messageRepository.GetConversationAsync(currentUserId, otherUserId);
        }
    }
}