using Avalonia.Controls;
using YoteiTasks.Services;

namespace YoteiTasks.Views;

public partial class NotificationPanel : UserControl
{
    public NotificationPanel()
    {
        InitializeComponent();
        DataContext = NotificationService.Instance;
    }
}
