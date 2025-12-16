using System;
using System.Threading.Tasks;
using System.Windows.Input;
using YoteiTasks.Services;
using YoteiTasks.Views;

namespace YoteiTasks.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isLoading;

        public string Username
        {
            get => _username;
            set => this.RaiseAndSetIfChanged(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        private ICommand? _loginCommand;
        private ICommand? _cancelCommand;

        public ICommand LoginCommand => _loginCommand ??= new RelayCommand(async _ => await Login(), _ => !IsLoading);
        public ICommand CancelCommand => _cancelCommand ??= new RelayCommand(_ => LoginCompleted?.Invoke(this, false));

        public event EventHandler<bool>? LoginCompleted;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
        }

        private async Task Login()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter both username and password";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var result = await _authService.LoginAsync(Username, Password);
                if (result)
                {
                    LoginCompleted?.Invoke(this, true);
                }
                else
                {
                    ErrorMessage = "Invalid username or password";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
