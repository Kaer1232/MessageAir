using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using МessageAir.Interfaces;

namespace МessageAir.ViewModels
{
    public partial class ProfileViewModel: ObservableObject
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _displayName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public bool IsNotBusy => !IsBusy;

        public ProfileViewModel(IAuthService authService)
        {
            _authService = authService;
        }

        [RelayCommand]
        private async Task UpdateName()
        {
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                StatusMessage = "Display name cannot be empty";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
