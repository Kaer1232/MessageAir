using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace МessageAir.Interfaces
{
    public interface IAuthService
    {
        string Username { get; }
        string Token { get; }
        Task<bool> LoginAsync(string username, string password);
        Task InitializeAsync();
        Task LogoutAsync();
    }
}
