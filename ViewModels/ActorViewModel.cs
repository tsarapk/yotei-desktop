using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YoteiLib.Core;
using YoteiTasks.Services;

namespace YoteiTasks.ViewModels;

public class ActorViewModel : INotifyPropertyChanged
{
    private readonly Actor _actor;
    private readonly Action<ActorViewModel>? _deleteCallback;
    private readonly IUserActorService? _userActorService;
    private string _username = string.Empty;
    private string _password = string.Empty;

    private RelayCommand? _setCredentialsCommand;

    public ICommand DeleteCommand { get; }
    public ICommand SetCredentialsCommand => _setCredentialsCommand ??= new RelayCommand(_ => SetCredentials(), _ => CanSetCredentials());

    public ActorViewModel(Actor actor, Action<ActorViewModel>? deleteCallback = null, IUserActorService? userActorService = null)
    {
        _actor = actor;
        _deleteCallback = deleteCallback;
        _userActorService = userActorService;
        _username = actor.Username ?? string.Empty;
        
        DeleteCommand = new RelayCommand(_ => _deleteCallback?.Invoke(this));
    }

    public Actor Model => _actor;

    public string Id => _actor.Id.ToString();

    public string Name
    {
        get => _actor.Name;
        set
        {
            if (_actor.Name != value)
            {
                _actor.SetName(value ?? string.Empty);
                OnPropertyChanged();
            }
        }
    }

    public string Username
    {
        get => _username;
        set
        {
            if (_username != value)
            {
                _username = value ?? string.Empty;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCredentials));
                _setCredentialsCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (_password != value)
            {
                _password = value ?? string.Empty;
                OnPropertyChanged();
                _setCredentialsCommand?.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasCredentials => !string.IsNullOrEmpty(_actor.Username);

    private bool CanSetCredentials()
    {
        var hasService = _userActorService != null;
        var hasUsername = !string.IsNullOrWhiteSpace(_username);
        var hasPassword = !string.IsNullOrWhiteSpace(_password);
        
        Console.WriteLine($"CanSetCredentials: Service={hasService}, Username={hasUsername} ('{_username}'), Password={hasPassword}");
        
        return hasService && hasUsername && hasPassword;
    }

    private void SetCredentials()
    {
        if (_userActorService == null || !CanSetCredentials())
            return;

        try
        {
            _userActorService.SetActorCredentials(_actor, _username, _password);
            _password = string.Empty; 
            OnPropertyChanged(nameof(Password));
            OnPropertyChanged(nameof(HasCredentials));
        }
        catch (Exception ex)
        {
            // TODO: показать ошибку пользователю
            Console.WriteLine($"Error setting credentials: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


