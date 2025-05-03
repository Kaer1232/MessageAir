using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Maui.Storage;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using МessageAir.Interfaces;

namespace МessageAir.Services
{
    public class AuthService: IAuthService
    {
        private const string AuthTokenKey = "auth_token";
        private readonly HttpClient _httpClient;

        public string Username { get; private set; }
        public string Token { get; private set; }

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("http://localhost:5273");
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", new
                {
                    Username = username,
                    Password = password
                });

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResult>();
                    Console.WriteLine($"Token received: {result.Token}"); // Логирование
                    Username = username;
                    await SecureStorage.SetAsync("username", username);
                    await SecureStorage.SetAsync("jwt_token", result.Token);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex}");
            }
            return false;
        }

        public async Task InitializeAsync()
        {
            Username = await SecureStorage.GetAsync("username");
            Token = await SecureStorage.Default.GetAsync(AuthTokenKey);
            if (!string.IsNullOrEmpty(Token))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(Token);
                Username = jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            }
        }

        public async Task LogoutAsync()
        {
            Token = null;
            Username = null;
            SecureStorage.Remove("username");
            SecureStorage.Default.Remove(AuthTokenKey);
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    public class AuthResult
    {
        public string Token { get; set; }
    }
}