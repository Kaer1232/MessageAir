using SignalRChatServer.Models;

namespace SignalRChatServer.Data
{
    public interface IUserRepository
    {
        Task<UserModel> GetByIdAsync(string id);
        Task<UserModel> GetByUsernameAsync(string username);
        Task<bool> ExistsAsync(string username);
        Task AddAsync(UserModel user);
        Task UpdateAsync(UserModel user);
        Task<UserModel> AuthenticateAsync(string username, string password);
    }
}
