using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace YoteiTasks.Views;

public partial class ResourceReportView : UserControl
{
    public ResourceReportView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
