using SignalRChatServer.Models;

namespace SignalRChatServer.Services
{
    public interface IAuthService
    {
        (string hash, string salt) CreatePasswordHash(string password);
        bool VerifyPassword(string password, string hash, string salt);
        string GenerateJwtToken(UserModel user);
    }
}