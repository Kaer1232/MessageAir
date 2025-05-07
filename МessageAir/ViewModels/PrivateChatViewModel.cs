using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Serialization;
using МessageAir.Models;
using МessageAir.Services;

namespace МessageAir.ViewModels
{
    [QueryProperty(nameof(OtherUserId), "OtherUserId")]
    [QueryProperty(nameof(OtherUserName), "OtherUserName")]
    public partial class PrivateChatViewModel : ObservableObject
    {
        private readonly AuthService _authService;
        private HubConnection _hubConnection;

        [ObservableProperty]
        private string _otherUserId;

        [ObservableProperty]
        private string _otherUserName;

        [ObservableProperty]
        private string _message;

        [ObservableProperty]
        private bool _isConnected;

        private readonly Dictionary<string, PrivateMessageModel> _pendingFiles = new();

        [ObservableProperty]
        private string _status;

        public ObservableCollection<PrivateMessageModel> Messages { get; } = new();
        public Action ScrollToBottom { get; set; }

        public PrivateChatViewModel(AuthService authService)
        {
            _authService = authService;

            // Автоматическая инициализация при получении OtherUserId
            this.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(OtherUserId) && !string.IsNullOrEmpty(OtherUserId))
                {
                    InitializeConnection();
                }
            };
        }

        private async void InitializeConnection()
        {
            Messages.Clear();
            OnPropertyChanged(nameof(GroupedMessages));

            if (string.IsNullOrEmpty(OtherUserId)) return;

            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync();
            }

            try
            {
                // Останавливаем предыдущее подключение
                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }

                // Ваш оригинальный код создания подключения
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:5273/privateChatHub", options =>
                    {
                        options.AccessTokenProvider = async () =>
                        {
                            var token = await SecureStorage.GetAsync("jwt_token");
                            return string.IsNullOrEmpty(token) ? string.Empty : token;
                        };
                        options.SkipNegotiation = true;
                        options.Transports = HttpTransportType.WebSockets;
                    })
                    .WithAutomaticReconnect()
                    .Build();

                ConfigureHubEvents();
                await ConnectWithRetry();
                await LoadHistory();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InitializeConnection error: {ex}");
                Status = $"Ошибка подключения: {ex.Message}";
            }
        }

        private void ConfigureHubEvents()
        {
            _hubConnection.Remove("ReceivePrivateMessage");
            _hubConnection.Remove("ReceivePrivateFile");

            // Добавляем новые обработчики
            _hubConnection.On<PrivateMessageModel>("ReceivePrivateMessage", message =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Проверяем, что сообщение для текущего чата
                    if (message.FromUserId != OtherUserId && message.ToUserId != OtherUserId) return;

                    message.Timestamp = message.Timestamp.ToLocalTime();

                    // Проверяем, не было ли уже добавлено это сообщение локально
                    if (Messages.Any(m =>
                        m.Text == message.Text &&
                        Math.Abs((m.Timestamp - message.Timestamp).TotalSeconds) < 1))
                        return;

                    message.IsCurrentUser = message.FromUserName == _authService.Username;
                    Messages.Add(message);
                    OnPropertyChanged(nameof(GroupedMessages));
                    ScrollToBottom?.Invoke();
                });
            });

            _hubConnection.On<PrivateMessageModel, string>("ReceivePrivateFileConfirmed",
     (serverMessage, tempKey) =>
     {
         MainThread.BeginInvokeOnMainThread(() =>
         {
             if (_pendingFiles.TryGetValue(tempKey, out var pendingMessage))
             {
                 // Обновляем сообщение данными с сервера
                 pendingMessage.Id = serverMessage.Id;
                 pendingMessage.Timestamp = serverMessage.Timestamp;

                 _pendingFiles.Remove(tempKey);
                 OnPropertyChanged(nameof(GroupedMessages));
                 ScrollToBottom?.Invoke();
             }
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
        }

        public ObservableCollection<PrivateMessageGroup> GroupedMessages
        {
            get
            {
                var grouped = Messages
            .GroupBy(m => m.Timestamp.Date)
            .Select(g => new PrivateMessageGroup(
                g.Key.ToString("dd MMMM yyyy", CultureInfo.CurrentCulture),
                g.Key,
                g.OrderBy(m => m.Timestamp).ToList())) // Сообщения внутри группы - новые сверху
            .OrderBy(g => g.SortableDate) // Группы дат - старые сверху
            .ToList();

                return new ObservableCollection<PrivateMessageGroup>(grouped);
            }
        }

        [RelayCommand]
        private async Task SendFile()
        {
            if (_hubConnection?.State != HubConnectionState.Connected) return;

            try
            {
                var fileResult = await FilePicker.Default.PickAsync();
                if (fileResult == null) return;

                using var stream = await fileResult.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                if (fileBytes.Length > 8 * 1024 * 1024)
                {
                    Status = "Файл слишком большой (макс. 8MB)";
                    return;
                }

                // Генерируем уникальный временный ключ на основе UTC времени
                var tempUtcKey = DateTime.UtcNow.Ticks.ToString();

                var message = new PrivateMessageModel
                {
                    FromUserId = _authService.Username,
                    ToUserId = OtherUserId,
                    FileName = fileResult.FileName,
                    FileData = fileBytes,
                    FileType = fileResult.ContentType,
                    Timestamp = DateTime.Now, // Локальное время для отображения
                    IsCurrentUser = true,
                    Text = $"[Отправка файла: {fileResult.FileName}]"
                };

                // Добавляем в коллекцию
                Messages.Add(message);
                OnPropertyChanged(nameof(GroupedMessages));
                ScrollToBottom?.Invoke();

                // Отправляем на сервер с временным ключом UTC
                await _hubConnection.InvokeAsync("SendPrivateFile",
                    OtherUserId,
                    fileResult.FileName,
                    fileBytes,
                    fileResult.ContentType,
                    tempUtcKey);
            }
            catch (Exception ex)
            {
                Status = $"Ошибка: {ex.Message}";
                Debug.WriteLine($"SendFile error: {ex}");
            }
        }


        [RelayCommand]
        private async void Back()
        {
            try
            {
                // 1. Останавливаем и уничтожаем подключение
                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                }

                // 2. Очищаем текущие сообщения
                Messages.Clear();
                Message = null;

                // 3. Сбрасываем данные собеседника
                OtherUserId = string.Empty;
                OtherUserName = string.Empty;

                // 4. Возвращаемся на UsersView
                await Shell.Current.GoToAsync("//UsersView");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка в Back: {ex}");
            }
        }

        [RelayCommand]
        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(Message)) return;

            try
            {
                await _hubConnection.InvokeAsync("SendPrivateMessage", OtherUserId, Message);
                ScrollToBottom?.Invoke();
                Message = string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Send error: {ex}");
                Status = $"Ошибка: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task DownloadFile(PrivateMessageModel message)
        {
            if (message?.FileData == null) return;

            try
            {
                Status = "Скачивание файла...";

                // Сохраняем во временную папку
                var tempPath = Path.Combine(FileSystem.CacheDirectory, message.FileName);
                await File.WriteAllBytesAsync(tempPath, message.FileData);

                // Открываем файл
                await Launcher.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(tempPath),
                    Title = message.FileName
                });

                Status = "Файл открыт";
            }
            catch (Exception ex)
            {
                Status = $"Ошибка: {ex.Message}";
                Debug.WriteLine($"DownloadFile error: {ex}");
            }
        }

        private async Task LoadHistory()
        {
            try
            {
                Status = "Загрузка истории...";
                Messages.Clear();
                OnPropertyChanged(nameof(GroupedMessages));

                var messages = await _hubConnection.InvokeAsync<IEnumerable<PrivateMessageModel>>(
                    "GetConversation", OtherUserId);

                if (messages == null || !messages.Any())
                {
                    Status = "История сообщений пуста";
                    Debug.WriteLine("История сообщений не найдена");
                    return;
                }

                var tempList = new List<PrivateMessageModel>();
                foreach (var msg in messages.OrderBy(m => m.Timestamp))
                {
                    msg.IsCurrentUser = msg.FromUserName == _authService.Username;
                    msg.FromUserName ??= msg.IsCurrentUser ? _authService.Username : OtherUserName;
                    msg.Timestamp = msg.Timestamp.ToLocalTime();
                    tempList.Add(msg);
                }

                // Добавляем все сообщения разом
                foreach (var msg in tempList)
                {
                    Messages.Add(msg);
                }

                Status = $"Загружено {tempList.Count} сообщений";
                OnPropertyChanged(nameof(GroupedMessages));
                ScrollToBottom?.Invoke();
            }
            catch (HubException hex)
            {
                Status = "Сервер не вернул историю";
                Debug.WriteLine($"HubException in LoadHistory: {hex.Message}");
            }
            catch (Exception ex)
            {
                Status = $"Ошибка загрузки истории";
                Debug.WriteLine($"LoadHistory error: {ex}");

                Messages.Add(new PrivateMessageModel
                {
                    Text = "Не удалось загрузить историю сообщений",
                    Timestamp = DateTime.Now,
                    IsCurrentUser = false
                });
            }
            finally
            {
                if (Messages.Count == 0)
                {
                    Status = "Диалог пуст";
                    Debug.WriteLine("Диалог не содержит сообщений");
                }
            }
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
                    Status = "Connected to private chat";
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

        private async Task Reconnect()
        {
            try
            {
                if (_hubConnection.State == HubConnectionState.Disconnected)
                {
                    await _hubConnection.StartAsync();
                    Status = "Переподключено";
                }
            }
            catch (Exception ex)
            {
                Status = $"Ошибка переподключения: {ex.Message}";
                await Task.Delay(5000);
                await Reconnect();
            }
        }
    }
}