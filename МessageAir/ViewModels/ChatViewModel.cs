using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        }

        private async void InitializeConnection()
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

            _hubConnection.On<IEnumerable<MessageModel>>("ReceiveMessageHistory", messages =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var msg in messages)
                    {
                        Messages.Add(new MessageModel
                        {
                            User = msg.Sender,
                            Text = msg.Text,
                            Timestamp = msg.Timestamp,
                            IsCurrentUser = msg.Sender == _authService.Username
                        });
                    }
                });
            });

            _hubConnection.On<MessageModel>("ReceiveMessage", msg =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Messages.Add(new MessageModel
                    {
                        User = msg.Sender,
                        Text = msg.Text,
                        Timestamp = msg.Timestamp,
                        IsCurrentUser = msg.Sender == _authService.Username
                    });
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

        private void ConfigureHubEvents()
        {
            _hubConnection.On<string, string>("ReceiveMessage", (user, msg) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Messages.Add(new MessageModel
                    {
                        User = user,
                        Text = msg,
                        Timestamp = DateTime.Now
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
            }
            catch (Exception ex)
            {
                Status = $"Failed to send: {ex.Message}";
                // Пытаемся переподключиться
                await Reconnect();
            }
        }

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
            try
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                }

                await _authService.LogoutAsync();

                await Shell.Current.GoToAsync("//LoginView");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Disconnect error: {ex.Message}");
            }
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