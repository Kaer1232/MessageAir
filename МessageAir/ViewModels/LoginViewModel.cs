using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using МessageAir.Services;

namespace МessageAir.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly AuthService _authService;
        private readonly HttpClient _httpClient;
        private readonly IConnectivity _connectivity;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _username;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _password;

        [ObservableProperty]
        private bool _isRegisterMode;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private string _errorMessage;

        public bool IsNotBusy => !IsBusy;

        public LoginViewModel(
            AuthService authService,
            HttpClient httpClient,
            IConnectivity connectivity)
        {
            _authService = authService;
            _httpClient = httpClient;
            _connectivity = connectivity;
            _httpClient.BaseAddress = new Uri("http://localhost:5273");
        }

        [RelayCommand(CanExecute = nameof(CanAuth))]
        private async Task Login()
        {
            if (_connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                ErrorMessage = "No internet connection";
                return;
            }

            try
            {
                IsBusy = true;
                ErrorMessage = string.Empty;

                if (await _authService.LoginAsync(Username, Password))
                {
                    Username = null;
                    Password = null;
                    await Shell.Current.GoToAsync($"//UsersView");
                }
                else
                {
                    ErrorMessage = "Invalid login attempt";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<AuthResult?> ParseAuthResponse(HttpResponseMessage response)
        {
            string rawResponse = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"Raw server response: {rawResponse}");

            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                StatusMessage = "Server returned empty response";
                return null;
            }

            // Проверяем, является ли ответ JSON-объектом
            if (!rawResponse.Trim().StartsWith("{") && !rawResponse.Trim().StartsWith("["))
            {
                StatusMessage = $"Invalid server response: {TruncateString(rawResponse, 50)}";
                Debug.WriteLine($"Non-JSON response: {rawResponse}");
                return null;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                return JsonSerializer.Deserialize<AuthResult>(rawResponse, options);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"JSON parsing failed. Content: {rawResponse}\nError: {ex}");
                StatusMessage = "Server returned malformed data";
                return null;
            }
        }

        private string TruncateString(string value, int maxLength)
        {
            return value.Length <= maxLength
                ? value
                : value.Substring(0, maxLength) + "...";
        }


        [RelayCommand(CanExecute = nameof(CanAuth))]
        private async Task Register()
        {
            if (!await CheckConnectivity()) return;

            try
            {
                IsBusy = true;
                StatusMessage = "Registering...";

                var requestData = new { Username, Password };
                Debug.WriteLine($"Sending: {JsonSerializer.Serialize(requestData)}");

                using var response = await _httpClient.PostAsJsonAsync("api/auth/register", requestData);

                if (response == null)
                {
                    StatusMessage = "No response from server";
                    return;
                }

                var result = await ParseAuthResponse(response);

                if (result == null)
                {
                    // Попробуем прочитать как plain text
                    var errorContent = await response.Content.ReadAsStringAsync();
                    StatusMessage = response.StatusCode switch
                    {
                        HttpStatusCode.BadRequest => "Invalid request format",
                        HttpStatusCode.Conflict => "User already exists",
                        _ => $"Error: {errorContent}"
                    };
                    return;
                }

                if (string.IsNullOrWhiteSpace(result.Token))
                {
                    StatusMessage = "Server returned empty token";
                    return;
                }

                StatusMessage = "Registration successful!";
                await Shell.Current.GoToAsync("//UsersView");
            }
            catch (HttpRequestException ex)
            {
                StatusMessage = $"Network error: {ex.Message}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unexpected error: {ex.GetBaseException().Message}";
                Debug.WriteLine($"Full error: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private string? ExtractErrorMessage(string json)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("message", out var message))
                    return message.GetString();
                if (doc.RootElement.TryGetProperty("error", out var error))
                    return error.GetString();
                return json; // Возвращаем весь JSON как сообщение
            }
            catch
            {
                return json; // Возвращаем сырой текст, если не парсится
            }
        }
        private async Task<string?> TryParseError(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("message", out var message))
                    return message.GetString();
                if (doc.RootElement.TryGetProperty("error", out var error))
                    return error.GetString();
            }
            catch
            {
                return null;
            }
            return null;
        }

        private async Task<T?> TryParseResponse<T>(string json) where T : class
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        private async Task<HttpResponseMessage?> SafePostRequest(string endpoint, object data)
        {
            try
            {
                return await _httpClient.PostAsJsonAsync(endpoint, data);
            }
            catch (HttpRequestException ex)
            {
                StatusMessage = $"Network error: {ex.Message}";
                return null;
            }
            catch (TaskCanceledException)
            {
                StatusMessage = "Request timed out";
                return null;
            }
        }

        private async Task<bool> CheckConnectivity()
        {
            if (_connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                StatusMessage = "No internet connection";
                await Shell.Current.DisplayAlert("Error", "Please check your internet connection", "OK");
                return false;
            }
            return true;
        }

        private async Task ProcessSuccessfulAuth(HttpResponseMessage response)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthResult>();
            if (result?.Token == null)
            {
                StatusMessage = "Invalid server response";
                return;
            }

            await Shell.Current.GoToAsync("//ChatView");
        }

        private async Task ProcessFailedAuth(HttpResponseMessage response)
        {
            try
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                StatusMessage = error?.Message ?? response.ReasonPhrase ?? "Unknown error";
            }
            catch
            {
                StatusMessage = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "Invalid credentials",
                    HttpStatusCode.BadRequest => "Invalid request",
                    HttpStatusCode.Conflict => "User already exists",
                    _ => $"Error: {response.StatusCode}"
                };
            }
        }

        private void HandleException(Exception ex)
        {
            StatusMessage = ex switch
            {
                HttpRequestException => "Network error occurred",
                JsonException => "Invalid server response",
                _ => $"Unexpected error: {ex.Message}"
            };
            Console.WriteLine($"Auth error: {ex}");
        }

        [RelayCommand]
        private void ToggleAuthMode()
        {
            IsRegisterMode = !IsRegisterMode;
            StatusMessage = IsRegisterMode ? "Register mode" : "Login mode";
        }

        private bool CanAuth() =>
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password) &&
            IsNotBusy;

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName is nameof(Username) or nameof(Password) or nameof(IsBusy))
            {
                LoginCommand.NotifyCanExecuteChanged();
                RegisterCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public record AuthResult(string Token);
    public record ErrorResponse(string Message);
}