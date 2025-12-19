using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YoteiLib.Core;
using YoteiTasks.Services;

namespace YoteiTasks.ViewModels;




public class LoginPopupViewModel : INotifyPropertyChanged
{
    private readonly IUserActorService _userActorService;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isLoading;
    private bool _isRegisterMode;

    public string Username
    {
        get => _username;
        set
        {
            if (_username != value)
            {
                _username = value;
                OnPropertyChanged();
                ErrorMessage = string.Empty;
                (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    private void Register()
    {
        if (!IsRegisterMode)
            return;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите имя пользователя и пароль";
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Пароли не совпадают";
            return;
        }

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var actor = _userActorService.RegisterActor(Username, Password);
            LoginCompleted?.Invoke(this, actor);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка регистрации: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ToggleMode()
    {
        IsRegisterMode = !IsRegisterMode;
        ErrorMessage = string.Empty;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
    }

    public string Password
    {
        get => _password;
        set
        {
            if (_password != value)
            {
                _password = value;
                OnPropertyChanged();
                ErrorMessage = string.Empty;
                (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RegisterCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (_confirmPassword != value)
            {
                _confirmPassword = value;
                OnPropertyChanged();
                ErrorMessage = string.Empty;
                (RegisterCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsRegisterMode
    {
        get => _isRegisterMode;
        private set
        {
            if (_isRegisterMode != value)
            {
                _isRegisterMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Title));
                ErrorMessage = string.Empty;
                (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RegisterCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string Title => IsRegisterMode ? "Регистрация" : "Вход в систему";

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
                (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (RegisterCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand LoginCommand { get; }
    public ICommand RegisterCommand { get; }
    public ICommand ToggleModeCommand { get; }
    public ICommand CancelCommand { get; }

    public event EventHandler<Actor?>? LoginCompleted;

    public LoginPopupViewModel(IUserActorService userActorService)
    {
        _userActorService = userActorService;
        LoginCommand = new RelayCommand(_ => Login(), _ => CanLogin());
        RegisterCommand = new RelayCommand(_ => Register(), _ => CanRegister());
        ToggleModeCommand = new RelayCommand(_ => ToggleMode());
        CancelCommand = new RelayCommand(_ => Cancel());
    }

    private bool CanLogin()
    {
        return !IsLoading && 
               !string.IsNullOrWhiteSpace(Username) && 
               !string.IsNullOrWhiteSpace(Password) &&
               !IsRegisterMode;
    }

    private bool CanRegister()
    {
        return !IsLoading &&
               !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password) &&
               !string.IsNullOrWhiteSpace(ConfirmPassword) &&
               Password == ConfirmPassword &&
               IsRegisterMode;
    }

    private void Login()
    {
        if (!CanLogin())
            return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var actor = _userActorService.AuthenticateActor(Username, Password);
            
            if (actor != null)
            {
                LoginCompleted?.Invoke(this, actor);
            }
            else
            {
                ErrorMessage = "Неверное имя пользователя или пароль";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка входа: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Cancel()
    {
        LoginCompleted?.Invoke(this, null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
