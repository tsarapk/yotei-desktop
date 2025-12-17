using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace YoteiTasks.Views;

public partial class RecurringTaskConfigView : UserControl
{
    public RecurringTaskConfigView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
