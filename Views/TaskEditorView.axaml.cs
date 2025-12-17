using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using YoteiLib.Core;
using YoteiTasks.Models;
using YoteiTasks.ViewModels;

namespace YoteiTasks.Views;

public partial class TaskEditorView : UserControl
{
    public TaskEditorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        UpdateVisibility();
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        UpdateVisibility();
    }
    
    private void UpdateVisibility()
    {
        bool hasTask = DataContext != null;
        EmptyStateText.IsVisible = !hasTask;
        EditorContent.IsVisible = hasTask;
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is TaskEditorViewModel vm && vm.Node.TaskNode != null)
        {
            StatusComboBox.SelectedItem = vm.Node.TaskNode.Status;
        }
    }
}

