using Realms;
using Realms.Sync;
using RealmTodo.Models;
using RealmTodo.Services;
using System.Collections.ObjectModel;

namespace RealmTodo.Views;


public partial class ItemsPage : ContentPage
{
    private PartitionSyncConfiguration config;
    private Realm realm;

    public ItemsPage()
	{
        config = new PartitionSyncConfiguration($"{RealmService.CurrentUser.Id}", RealmService.CurrentUser);
        realm = Realm.GetInstance(config);

        InitializeComponent();
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(((SearchBar)sender).Text))
        {
            var items = new ObservableCollection<Item>(realm.All<Item>().Where(i => i.Summary.Contains(((SearchBar)sender).Text, StringComparison.OrdinalIgnoreCase)).ToList());
            listaTasks.ItemsSource = items;
        }
        else listaTasks.ItemsSource = new ObservableCollection<Item>(realm.All<Item>()).ToList();

    }
}
 