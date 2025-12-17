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
    private readonly Popup? _resourceReportPopup;
    private readonly Popup? _taskListPopup;
    private LoginPopupView? _loginPopupView;
    private ProjectCreationPopupView? _projectCreationPopupView;
    private ProjectEditorView? _projectEditorView;
    private ResourceReportView? _resourceReportView;
    private TaskListView? _taskListView;
    private MainViewModel? _mainViewModel;

    public MainWindow()
    {
        InitializeComponent();

        _mainContent = this.FindControl<ContentControl>("MainContent")
                      ?? throw new InvalidOperationException("MainContent ContentControl not found in XAML");
        _loginPopup = this.FindControl<Popup>("LoginPopup");
        _projectCreationPopup = this.FindControl<Popup>("ProjectCreationPopup");
        _projectEditorPopup = this.FindControl<Popup>("ProjectEditorPopup");
        _resourceReportPopup = this.FindControl<Popup>("ResourceReportPopup");
        _taskListPopup = this.FindControl<Popup>("TaskListPopup");

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
            SetupResourceReportPopup();
            SetupTaskListPopup();
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
    
    private void SetupResourceReportPopup()
    {
        if (_resourceReportPopup == null || _mainViewModel == null)
            return;
        
        _mainViewModel.PropertyChanged += OnResourceReportPopupChanged;
    }
    
    private void OnResourceReportPopupChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsResourceReportPopupOpen) && 
            _mainViewModel != null &&
            _mainViewModel.IsResourceReportPopupOpen &&
            _mainViewModel.ResourceReportViewModel != null &&
            _resourceReportPopup != null)
        {
            _resourceReportView = new ResourceReportView
            {
                DataContext = _mainViewModel.ResourceReportViewModel
            };
            
            _resourceReportPopup.Child = _resourceReportView;
        }
    }
    
    private void SetupTaskListPopup()
    {
        if (_taskListPopup == null || _mainViewModel == null)
            return;
        
        _mainViewModel.PropertyChanged += OnTaskListPopupChanged;
    }
    
    private void OnTaskListPopupChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsTaskListPopupOpen) && 
            _mainViewModel != null &&
            _mainViewModel.IsTaskListPopupOpen &&
            _mainViewModel.TaskListViewModel != null &&
            _taskListPopup != null)
        {
            _taskListView = new TaskListView
            {
                DataContext = _mainViewModel.TaskListViewModel
            };
            
            _taskListPopup.Child = _taskListView;
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