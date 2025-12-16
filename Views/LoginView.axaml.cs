using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using YoteiTasks.ViewModels;

namespace YoteiTasks.Views
{
    public partial class LoginView : UserControl, IDisposable
    {
        private bool _disposed;
        private LoginViewModel? _viewModel;
        
        public event EventHandler<bool>? LoginCompleted;
        
        public LoginView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            
            
            if (_viewModel != null)
            {
                _viewModel.LoginCompleted -= OnLoginCompleted;
            }
            
            _viewModel = DataContext as LoginViewModel;
            
            if (_viewModel != null)
            {
                _viewModel.LoginCompleted += OnLoginCompleted;
            }
        }
        
        private void OnLoginCompleted(object? sender, bool success)
        {
            LoginCompleted?.Invoke(this, success);
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                if (_viewModel != null)
                {
                    _viewModel.LoginCompleted -= OnLoginCompleted;
                    _viewModel = null;
                }
                
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
        
        ~LoginView()
        {
            Dispose(false);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    
                    if (_viewModel is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                
                _viewModel = null;
                _disposed = true;
            }
        }
    }
}
