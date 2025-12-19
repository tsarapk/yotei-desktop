using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using YoteiLib.Core;
using YoteiTasks.Services;
using YoteiTasks.ViewModels;

namespace YoteiTasks;

public partial class App : Application
{
    public static IAuthService? AuthService { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        
        var saveService = new SqliteSaveService();
        AuthService = new AuthService(saveService);
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
            
            
            var yotei = new Yotei();
            var userActorService = new UserActorService(yotei.Actors, () => yotei.Actors.Create());
            var superUserService = new SuperUserService(yotei, userActorService);
            
            
            superUserService.GetOrCreateSuperUser();
            
            mainWindow.DataContext = new MainViewModel(yotei, saveService, userActorService, superUserService);
        }

        base.OnFrameworkInitializationCompleted();
    }
}