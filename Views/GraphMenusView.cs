using Avalonia;
using Avalonia.Controls;
using YoteiTasks.Models;

namespace YoteiTasks.Views;

public partial class GraphCanvas
{
    private void ShowAddNodeMenu(Avalonia.Point position)
    {
        var contextMenu = new ContextMenu();
        
        var addNodeItem = new MenuItem { Header = "Добавить задачу" };
        addNodeItem.Click += (s, e) => AddNodeAtPosition(position);
        
        contextMenu.Items.Add(addNodeItem);
        
        contextMenu.Open(MainCanvas);
    }
    
    private void ShowNodeOperationsMenu(GraphNode? node)
    {
        if (node == null)
            return;

        var mainViewModel = DataContext as YoteiTasks.ViewModels.MainViewModel;
        var menu = new ContextMenu();
        var editNodeItem = new MenuItem { Header = "Выполнить" };
        var deleteNodeItem = new MenuItem { Header = "Удалить" };
        
        editNodeItem.Click += (s, e) =>
        {
            e.Handled = true;
            
        };
        
        deleteNodeItem.Click += (s, e) =>
        {
            if (Graph != null && node != null)
            {
                if (mainViewModel?.CanDeleteTasks == true)
                {
                    Graph.Nodes.Remove(node);
                    YoteiTasks.Services.NotificationService.Instance.ShowSuccess("Задача удалена");
                }
                else
                {
                    YoteiTasks.Services.NotificationService.Instance.ShowWarning("У вас недостаточно привилегий для удаления задач");
                }
            }
        };
        
        
        if (mainViewModel?.CanEditTasks == true)
        {
            menu.Items.Add(editNodeItem);
        }
        
        if (mainViewModel?.CanDeleteTasks == true)
        {
            menu.Items.Add(deleteNodeItem);
        }
        
        
        if (menu.Items.Count > 0)
        {
            menu.Open(MainCanvas);
        }
    }

    
}
