using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace YoteiTasks.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;
        private EventHandler? _canExecuteChanged;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => _canExecuteChanged += value;
            remove => _canExecuteChanged -= value;
        }

        public bool CanExecute(object? parameter)
        {
            try
            {
                return _canExecute == null || _canExecute(parameter);
            }
            catch
            {
                return false;
            }
        }

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                _execute(parameter);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;
        private EventHandler? _canExecuteChanged;

        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => _canExecuteChanged += value;
            remove => _canExecuteChanged -= value;
        }

        public bool CanExecute(object? parameter)
        {
            try
            {
                return _canExecute == null || _canExecute((T?)parameter);
            }
            catch
            {
                return false;
            }
        }

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                _execute((T?)parameter);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
