using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalRChatServer.Data;
using SignalRChatServer.Models;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace SignalRChatServer.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IMessageRepository _messageRepository;
        private static readonly ConcurrentDictionary<string, FileTransfer> _fileTransfers = new();


        public ChatHub(ILogger<ChatHub> logger, IMessageRepository messageRepository)
        {
            _logger = logger;
            _messageRepository = messageRepository;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var recentMessages = await _messageRepository.GetRecentMessagesAsync();

                foreach (var msg in recentMessages)
                {
                    try
                    {
                        if (msg.HasFile)
                        {
                            await Clients.Caller.SendAsync("ReceiveFileMessage",
                                msg.Sender,
                                msg.FileName ?? string.Empty,
                                msg.FileData ?? Array.Empty<byte>(),
                                msg.FileType ?? string.Empty,
                                msg.Timestamp);
                        }
                        else
                        {
                            await Clients.Caller.SendAsync("ReceiveMessage",
                                msg.Sender,
                                msg.Text ?? string.Empty,
                                msg.Timestamp,
                                msg.Timestamp.Date.ToString("yyyy-MM-dd"));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error sending message to client");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync");
                throw;
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

        public async Task StartFileTransfer(string fileName, long fileSize, string contentType)
        {
            // Создаем новую передачу файла
            var transfer = new FileTransfer
            {
                FileName = fileName,
                FileSize = fileSize,
                ContentType = contentType,
                Stream = new MemoryStream()
            };

            _fileTransfers[Context.ConnectionId] = transfer;
            await Clients.Caller.SendAsync("FileTransferStarted");
        }

        public async Task SendFileChunk(byte[] chunk)
        {
            if (_fileTransfers.TryGetValue(Context.ConnectionId, out var transfer))
            {
                await transfer.Stream.WriteAsync(chunk, 0, chunk.Length);
                transfer.BytesReceived += chunk.Length;

                // Отправляем прогресс (опционально)
                var progress = (double)transfer.BytesReceived / transfer.FileSize * 100;
                await Clients.Caller.SendAsync("FileTransferProgress", progress);
            }
        }

        public async Task CompleteFileTransfer()
        {
            if (_fileTransfers.TryRemove(Context.ConnectionId, out var transfer))
            {
                try
                {
                    var fileData = transfer.Stream.ToArray();
                    await _messageRepository.AddFileMessageAsync(
                        Context.User?.Identity?.Name ?? "Anonymous",
                        transfer.FileName,
                        fileData,
                        transfer.ContentType);

                    await Clients.All.SendAsync("ReceiveFileMessage",
                        Context.User?.Identity?.Name,
                        transfer.FileName,
                        fileData,
                        transfer.ContentType,
                        DateTime.UtcNow);
                }
                finally
                {
                    transfer.Stream.Dispose();
                }
            }
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

    public class FileTransfer
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public MemoryStream Stream { get; set; }
        public long BytesReceived { get; set; }
    }
}