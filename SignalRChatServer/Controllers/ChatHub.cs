using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SignalRChatServer.Data;
using SignalRChatServer.Models;
using System.Security.Claims;

namespace SignalRChatServer.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IMessageRepository _messageRepository;


        public ChatHub(ILogger<ChatHub> logger, IMessageRepository messageRepository)
        {
            _logger = logger;
            _messageRepository = messageRepository;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var token = Context.GetHttpContext()?.Request.Query["access_token"];
                Console.WriteLine($"Token received: {token}");

                if (Context.User?.Identity?.IsAuthenticated != true)
                {
                    Context.Abort();
                    return;
                }

                // Проверяем, что _messageRepository не null
                if (_messageRepository == null)
                {
                    _logger.LogError("MessageRepository is not initialized");
                    throw new InvalidOperationException("MessageRepository is not initialized");
                }

                // Отправляем историю сообщений новому клиенту
                var recentMessages = await _messageRepository.GetRecentMessagesAsync();
                foreach (var msg in recentMessages)
                {
                    await Clients.Caller.SendAsync("ReceiveMessage",
                        msg.Sender,
                        msg.Text,
                        msg.Timestamp,
                        msg.Timestamp.Date.ToString("yyyy-MM-dd"));
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync");
                throw; // Можно заменить на обработку ошибки, если нужно
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.User?.Identity?.Name;

            if (exception != null)
            {
                _logger.LogError(exception, $"User {username} disconnected unexpectedly");
            }
            else
            {
                _logger.LogInformation($"User {username} disconnected gracefully");
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AuthenticatedUsers");
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string message)
        {
            var username = Context.User?.Identity?.Name;
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(username))
            {
                _logger.LogWarning("Unauthorized message attempt from connection: {ConnectionId}", Context.ConnectionId);
                throw new HubException("You must be authenticated to send messages");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                await Clients.Caller.SendAsync("ReceiveErrorMessage", "Message cannot be empty");
                return;
            }

            _logger.LogInformation($"User {username} (ID: {userId}) sent message: {message}");

            // Фиксируем время на сервере (UTC)
            var timestamp = DateTime.UtcNow;

            // Сохраняем сообщение в базу
            var messageModel = new MessageModel
            {
                Sender = username,
                Text = message,
                Timestamp = timestamp,
                IsSystemMessage = false
            };

            await _messageRepository.AddMessageAsync(messageModel);

            // Отправляем всем с timestamp
            await _messageRepository.AddMessageAsync(messageModel);

            // Добавляем дату в отправляемые данные
            await Clients.All.SendAsync("ReceiveMessage", 
        username, 
        message, 
        timestamp,
        timestamp.Date.ToString("yyyy-MM-dd"));
        }

        [Authorize(Roles = "Admin")]
        public async Task SendAdminMessage(string message)
        {
            var username = Context.User?.Identity?.Name;
            await Clients.All.SendAsync("ReceiveMessage",
                new
                {
                    Sender = $"[ADMIN] {username}",
                    Text = message,
                    Timestamp = DateTime.UtcNow,
                    IsSystemMessage = true
                });
        }

        [Authorize(Roles = "Admin")] // Только для администраторов
        public async Task PurgeAllMessages()
        {
            // Полная очистка таблицы
            await _messageRepository.PurgeAllMessagesAsync();

            // Уведомляем всех клиентов
            await Clients.All.SendAsync("OnMessagesPurged");
        }

        public async Task JoinGroup(string groupName)
        {
            var username = Context.User?.Identity?.Name;
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("ReceiveSystemMessage",
                $"{username} has joined the group {groupName}");
            _logger.LogInformation($"User {username} joined group {groupName}");
        }

        public async Task LeaveGroup(string groupName)
        {
            var username = Context.User?.Identity?.Name;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("ReceiveSystemMessage",
                $"{username} has left the group {groupName}");
            _logger.LogInformation($"User {username} left group {groupName}");
        }
    }
}