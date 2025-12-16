using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YoteiLib.Core;

namespace YoteiTasks.ViewModels;

public class RolePrivItemViewModel : INotifyPropertyChanged
{
    private readonly Role _role;
    private bool _isSelected;

    public RolePrivItemViewModel(RolePriv priv, Role role)
    {
        Priv = priv;
        _role = role;
        _isSelected = role.privs.Contains(priv);
    }

    public RolePriv Priv { get; }

    public string Name => Priv.ToString();

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;

            if (_isSelected)
            {
                if (!_role.privs.Contains(Priv))
                    _role.privs.Add(Priv);
            }
            else
            {
                _role.privs.Remove(Priv);
            }

            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RoleViewModel : INotifyPropertyChanged
{
    private readonly Role _role;
    private readonly Action<RoleViewModel>? _deleteCallback;

    public RoleViewModel(Role role, Action<RoleViewModel>? deleteCallback = null)
    {
        _role = role;
        _deleteCallback = deleteCallback;

        DeleteCommand = new RelayCommand(_ => _deleteCallback?.Invoke(this));

        var items = new ObservableCollection<RolePrivItemViewModel>();
        foreach (var priv in Enum.GetValues<RolePriv>())
        {
            items.Add(new RolePrivItemViewModel(priv, _role));
        }
        Privileges = items;
    }

    public Role Model => _role;

    public string Name
    {
        get => _role.Name;
        set
        {
            if (_role.Name == value)
                return;

            _role.Name = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public int Strength
    {
        get => _role.Strength;
        set
        {
            if (_role.Strength == value)
                return;

            _role.Strength = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<RolePrivItemViewModel> Privileges { get; }

    public ICommand DeleteCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
