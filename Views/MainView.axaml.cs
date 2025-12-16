using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using YoteiTasks.ViewModels;

namespace YoteiTasks.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }
    }
}
