using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using МessageAir.Models;
using МessageAir.Services;

namespace МessageAir.ViewModels
{
    public partial class UsersViewModel : ObservableObject
    {
        private HubConnection _hubConnection;
        private AuthService _authService;

        private bool _isInitialLoad = true;

        public ObservableCollection<UserModel> Users { get; } = new();


        public ICommand SelectUserCommand { get; }
        public ICommand RefreshCommand { get; }

        private bool _isRefreshing;


        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public UsersViewModel(AuthService authService)
        {
            InitializeHub();
            _authService = authService;
            Debug.WriteLine($"Current username UserView: {_authService.Username}");
        }

        [RelayCommand]
        private async Task OpenPrivateChatAsync(UserModel user)
        {
            if (user is null) return;

            try
            {
                var parameters = new Dictionary<string, object>
        {
            { "OtherUserId", user.Id },
            { "OtherUserName", user.Username }
        };
                await Shell.Current.GoToAsync("//PrivateChatView", true, parameters);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "😢");
            }
        }

        public async Task Reset()
        {
            _isInitialLoad = true;
            Users.Clear();
            await _hubConnection?.StopAsync();
        }

        public async Task InitializeHub()
        {
            try
            {

                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }


                _hubConnection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:5273/privateChatHub", options =>
                    {
                        options.AccessTokenProvider = async () =>
                        {
                            var token = await SecureStorage.GetAsync("jwt_token");
                            return token;
                        };
                        options.SkipNegotiation = true;
                        options.Transports = HttpTransportType.WebSockets;
                    })
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<string>("ReceiveError", error =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Shell.Current.DisplayAlert("Ой-ой", error, "Понял");
                    });
                });

                await _hubConnection.StartAsync();

                if (_isInitialLoad)
                {
                    await LoadUsers();
                    _isInitialLoad = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Милая ошибка: Не удалось подключиться: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task LoadUsersAsync()
        {
            IsRefreshing = true;

            try
            {
                var users = await _hubConnection.InvokeAsync<List<UserModel>>("GetAvailableUsers");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task LoadUsers()
        {
            IsRefreshing = true;

            try
            {
                if (_hubConnection.State != HubConnectionState.Connected)
                {
                    await _hubConnection.StartAsync();
                }

                var users = await _hubConnection.InvokeAsync<IEnumerable<UserModel>>("GetAvailableUsers");

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Users.Clear();
                    foreach (var user in users)
                    {
                        Users.Add(user);
                    }
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Ошибка",
                    $"Не удалось загрузить пользователей: {ex.Message}",
                    "OK");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task Disconnect()
        {
            Debug.WriteLine($"Before disconnect - Username: {_authService?.Username}");
            try
            {
                // 1. Очищаем список пользователей
                Users.Clear();
                _isInitialLoad = true;

                // 2. Останавливаем и уничтожаем подключение
                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                }

                // 3. Выходим из системы
                if (_authService != null)
                {
                    await _authService.LogoutAsync();
                }

                // 4. Полностью сбрасываем NavigationStack
                await Shell.Current.GoToAsync("//LoginView", animate: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Disconnect error: {ex.Message}");
                await Shell.Current.DisplayAlert("Ошибка", "Не удалось выйти", "OK");
            }
        }
    }
}
