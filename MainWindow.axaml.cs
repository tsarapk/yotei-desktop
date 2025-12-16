using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using System;
using System.ComponentModel;
using System.Linq;
using YoteiLib.Core;
using YoteiTasks.Services;
using YoteiTasks.ViewModels;
using YoteiTasks.Views;

namespace YoteiTasks;

public partial class MainWindow : Window
{
    private readonly ContentControl _mainContent;
    private readonly Popup? _loginPopup;
    private readonly Popup? _projectCreationPopup;
    private readonly Popup? _projectEditorPopup;
    private LoginPopupView? _loginPopupView;
    private ProjectCreationPopupView? _projectCreationPopupView;
    private ProjectEditorView? _projectEditorView;
    private MainViewModel? _mainViewModel;

    public MainWindow()
    {
        InitializeComponent();

        _mainContent = this.FindControl<ContentControl>("MainContent")
                      ?? throw new InvalidOperationException("MainContent ContentControl not found in XAML");
        _loginPopup = this.FindControl<Popup>("LoginPopup");
        _projectCreationPopup = this.FindControl<Popup>("ProjectCreationPopup");
        _projectEditorPopup = this.FindControl<Popup>("ProjectEditorPopup");

        this.KeyDown += OnKeyDown;
        
        
        this.DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            _mainViewModel = viewModel;
            SetupLoginPopup();
            SetupProjectCreationPopup();
            SetupProjectEditorPopup();
        }
    }
    
    private void SetupLoginPopup()
    {
        if (_loginPopup == null || _mainViewModel == null)
            return;
            
        _loginPopupView = new LoginPopupView
        {
            DataContext = new LoginPopupViewModel(_mainViewModel.UserActorService)
        };
        
        if (_loginPopupView.DataContext is LoginPopupViewModel loginVm)
        {
            loginVm.LoginCompleted += (s, actor) =>
            {
                _mainViewModel.OnLoginCompleted(actor);
            };
        }
        
        _loginPopup.Child = _loginPopupView;
    }
    
    private void SetupProjectCreationPopup()
    {
        if (_projectCreationPopup == null || _mainViewModel == null)
            return;
        
        
        _mainViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsProjectCreationPopupOpen) && 
                _mainViewModel.IsProjectCreationPopupOpen)
            {
                
                _projectCreationPopupView = new ProjectCreationPopupView
                {
                    DataContext = new ProjectCreationPopupViewModel(
                        _mainViewModel.Actors.Select(a => a.Model),
                        _mainViewModel.Roles.Select(r => r.Model))
                };
                
                if (_projectCreationPopupView.DataContext is ProjectCreationPopupViewModel projectVm)
                {
                    projectVm.ProjectCreated += (s, project) =>
                    {
                        _mainViewModel.OnProjectCreated(project);
                    };
                }
                
                _projectCreationPopup.Child = _projectCreationPopupView;
            }
        };
    }
    
    private void SetupProjectEditorPopup()
    {
        if (_projectEditorPopup == null || _mainViewModel == null)
            return;
        
        
        _mainViewModel.PropertyChanged += OnProjectEditorPopupChanged;
    }
    
    private void OnProjectEditorPopupChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsProjectEditorPopupOpen) && 
            _mainViewModel != null &&
            _mainViewModel.IsProjectEditorPopupOpen &&
            _mainViewModel.SelectedProject != null &&
            _projectEditorPopup != null)
        {
            
            var currentProject = _mainViewModel.SelectedProject;
            
            Console.WriteLine($"[MainWindow] Открываем редактор для проекта: {currentProject.Name} (ID: {currentProject.Id})");
            
            _projectEditorView = new ProjectEditorView
            {
                DataContext = new ProjectEditorViewModel(
                    currentProject,
                    _mainViewModel.Actors.Select(a => a.Model),
                    _mainViewModel.Roles.Select(r => r.Model))
            };
            
            if (_projectEditorView.DataContext is ProjectEditorViewModel editorVm)
            {
                editorVm.EditCompleted += (s, saved) =>
                {
                    _mainViewModel.OnProjectEdited(saved);
                };
            }
            
            _projectEditorPopup.Child = _projectEditorView;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.S && e.KeyModifiers == KeyModifiers.Control)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.Save();
                e.Handled = true;
            }
        }
    }
}