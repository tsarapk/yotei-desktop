using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace YoteiTasks.Views;

public partial class LoginPopupView : UserControl
{
    public LoginPopupView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
