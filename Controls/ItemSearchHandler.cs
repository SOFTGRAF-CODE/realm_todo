using RealmTodo.Models;
using RealmTodo.Services;

namespace RealmTodo.Controls;

public class ItemSearchHandler : SearchHandler
{
    public IList<Item> Items { get; set; }
    public Type SelectedItemNavigationTarget { get; set; }

    protected override void OnQueryChanged(string oldValue, string newValue)
    {
        base.OnQueryChanged(oldValue, newValue);

        if (string.IsNullOrWhiteSpace(newValue))
        {
            ItemsSource = null;
        }
        else
        {
            ItemsSource = RealmService.GetItems(newValue);
        }
    }

    protected override async void OnItemSelected(object item)
    {
        base.OnItemSelected(item);

        // Let the animation complete
        await Task.Delay(1000);

        ShellNavigationState state = (Application.Current.MainPage as Shell).CurrentState;
        // The following route works because route names are unique in this app.
        //await Shell.Current.GoToAsync($"{GetNavigationTarget()}?name={((Item)item).Summary}");
    }
    /*
    string GetNavigationTarget()
    {
         return (Shell.Current as AppShell).Routes.FirstOrDefault(route => route.Value.Equals(SelectedItemNavigationTarget)).Key;
    }*/
}