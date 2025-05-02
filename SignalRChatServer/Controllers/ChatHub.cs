using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SignalRChatServer.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var token = Context.GetHttpContext()?.Request.Query["access_token"];
            Console.WriteLine($"Token received: {token}");

            if (Context.User?.Identity?.IsAuthenticated != true)
            {
                Context.Abort();
                return;
            }

            await base.OnConnectedAsync();
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

            try
            {
                await Clients.All.SendAsync("ReceiveMessage", username, message);

                //await Clients.All.SendAsync("ReceiveMessage",
                //    new
                //    {
                //        Sender = username,
                //        Text = message,
                //        Timestamp = DateTime.UtcNow,
                //        IsSystemMessage = false
                //    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                await Clients.Caller.SendAsync("ReceiveErrorMessage", "Failed to send message");
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