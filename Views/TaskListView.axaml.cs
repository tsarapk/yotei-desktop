using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using YoteiTasks.ViewModels;

namespace YoteiTasks.Views;

public partial class TaskListView : UserControl
{
    public TaskListView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnTaskItemClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Grid grid && 
            grid.DataContext is TaskListItemViewModel taskItem &&
            DataContext is TaskListViewModel viewModel)
        {
            viewModel.SelectTaskCommand.Execute(taskItem);
        }
    }
}
