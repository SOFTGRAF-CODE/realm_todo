using Realms.Sync;

namespace RealmTodo;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        //RealmApp = Realms.Sync.App.Create(AppConfiguration.RealmAppId);
        MainPage = new AppShell();
    }
}