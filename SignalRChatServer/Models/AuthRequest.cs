namespace SignalRChatServer.Models
{
    public class LoginAuthRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RegisterAuthRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class AuthResponse
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
