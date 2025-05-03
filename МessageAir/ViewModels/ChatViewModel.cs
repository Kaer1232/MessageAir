using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using МessageAir.Models;
using МessageAir.Services;

namespace МessageAir.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        private readonly AuthService _authService;
        private HubConnection _hubConnection;

        [ObservableProperty]
        private string _message;
        [ObservableProperty]
        private ObservableCollection<MessageModel> _messages = new();

        [ObservableProperty]
        private bool _isConnected;

        [ObservableProperty]
        private string _status;

        public string Username => _authService.Username;


        public ChatViewModel(AuthService authService)
        {
            _authService = authService;
            InitializeConnection();
            ScrollToBottom?.Invoke();

            Debug.WriteLine($"Current username: {_authService.Username}");
        }

        public async void InitializeConnection()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
            _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5273/chatHub", options =>
            {
                options.AccessTokenProvider = async () => await SecureStorage.GetAsync("jwt_token");
                options.SkipNegotiation = true;
                options.Transports = HttpTransportType.WebSockets;
            })
            .WithAutomaticReconnect()
            .Build();


            _hubConnection.On<string, string, DateTime, string>("ReceiveMessage", (sender, message, timestamp, dateGroup) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    var localTime = timestamp.ToLocalTime();

                    Messages.Add(new MessageModel
                    {
                        Sender = sender,
                        Text = message,
                        Timestamp = localTime,
                        IsCurrentUser = sender == _authService.Username
                        // DateGroup теперь вычисляется автоматически
                    });

                    OnPropertyChanged(nameof(GroupedMessages));
                });
            });

            _hubConnection.Reconnecting += ex =>
            {
                Status = "Reconnecting...";
                IsConnected = false;
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += id =>
            {
                Status = "Reconnected";
                IsConnected = true;
                return Task.CompletedTask;
            };

            try
            {
                await ConnectWithRetry();
                await _hubConnection.StartAsync();
                IsConnected = true;
                Status = "Connected to chat";
            }
            catch (Exception ex)
            {
                Status = $"Connection failed: {ex.Message}";
            }
        }

        public ObservableCollection<MessageGroup> GroupedMessages
        {
            get
            {
                var grouped = Messages
            .GroupBy(m => m.Timestamp.Date)
            .Select(g => new MessageGroup(
                g.Key.ToString("dd MMMM yyyy", CultureInfo.CurrentCulture),
                g.Key,
                g.OrderBy(m => m.Timestamp).ToList())) // Сообщения внутри группы - новые сверху
            .OrderBy(g => g.SortableDate) // Группы дат - старые сверху
            .ToList();

                return new ObservableCollection<MessageGroup>(grouped);
            }
        }


        [RelayCommand]
        private async Task NuclearPurge()
        {
            try
            {
                // 1. Очищаем локальные сообщения
                Messages.Clear();

                // 2. Отправляем команду на сервер
                await _hubConnection.InvokeAsync("PurgeAllMessages");

                // 3. Получаем подтверждение
                _hubConnection.On("OnMessagesPurged", () =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Messages.Clear();
                        Status = "Все сообщения уничтожены";
                    });
                });
            }
            catch (Exception ex)
            {
                Status = $"Ядерный удар не удался: {ex.Message}";
            }
        }

        private void ConfigureHubEvents()
        {
            _hubConnection.On<string, string>("ReceiveMessage", (sender, message) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Messages.Add(new MessageModel
                    {
                        Sender = sender,
                        Text = message,
                        Timestamp = DateTime.Now,
                        IsCurrentUser = sender == _authService.Username
                    });
                });
            });

            _hubConnection.Reconnecting += ex =>
            {
                Status = "Reconnecting...";
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                Status = "Reconnected";
                IsConnected = true;
                return Task.CompletedTask;
            };

            _hubConnection.Closed += async ex =>
            {
                IsConnected = false;
                Status = "Connection closed";
                await Task.Delay(5000);
                await ConnectToHub();
            };
        }

        private async Task ConnectWithRetry()
        {
            int maxAttempts = 3;
            for (int i = 0; i < maxAttempts; i++)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    IsConnected = true;
                    Status = "Connected";
                    return;
                }
                catch (Exception ex) when (i < maxAttempts - 1)
                {
                    Status = $"Attempt {i + 1} failed, retrying...";
                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    Status = $"Connection failed: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        private async Task ConnectToHub()
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _hubConnection.StartAsync();
                    IsConnected = true;
                    Status = "Connected to chat hub";
                }
                catch (Exception ex)
                {
                    Status = $"Connection failed: {ex.Message}";
                    await Task.Delay(5000);
                    await ConnectToHub();
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanSendMessage))]
        private async Task SendMessage()
        {
            Messages.Clear();
            if (string.IsNullOrWhiteSpace(Message)) return;

            // Проверяем соединение
            if (_hubConnection == null)
            {
                InitializeConnection();
                await ConnectWithRetry();
            }

            try
            {
                await _hubConnection.InvokeAsync("SendMessage", Message);
                Status = string.Empty;
                ScrollToBottom?.Invoke();
            }
            catch (Exception ex)
            {
                Status = $"Failed to send: {ex.Message}";
                // Пытаемся переподключиться
                await Reconnect();
            }
        }
        public Action ScrollToBottom { get; set; }


        private async Task Reconnect()
        {
            try
            {
                InitializeConnection();
                await ConnectWithRetry();
            }
            catch (Exception ex)
            {
                Status = $"Reconnect failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task Disconnect()
        {
            Debug.WriteLine($"Before disconnect - Username: {_authService?.Username}");
            try
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                }

                Messages.Clear();
                Message = null;

                // Добавляем проверку на null и await
                if (_authService != null)
                {
                    await _authService.LogoutAsync();
                    Debug.WriteLine($"Username after logout: {_authService.Username}"); // Должно быть null
                }

                // Перенаправляем на страницу логина
                await Shell.Current.GoToAsync("//LoginView", animate: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Disconnect error: {ex.Message}");
                // Можно добавить отображение ошибки пользователю
                await Shell.Current.DisplayAlert("Ошибка", "Не удалось выйти", "OK");
            }
            Debug.WriteLine($"After disconnect - Username: {_authService?.Username}");
        }

        private bool CanSendMessage() => !string.IsNullOrWhiteSpace(Message) && IsConnected;

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(Message) || e.PropertyName == nameof(IsConnected))
            {
                SendMessageCommand.NotifyCanExecuteChanged();
            }
        }
    }
}