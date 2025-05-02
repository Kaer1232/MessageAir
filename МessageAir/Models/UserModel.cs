namespace МessageAir.Models
{
    public class UserModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

public class AuthRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
}
