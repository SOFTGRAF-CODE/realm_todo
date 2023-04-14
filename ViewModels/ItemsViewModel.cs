using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RealmTodo.Models;
using RealmTodo.Services;
using Realms;
using Realms.Sync;
using System.Collections.ObjectModel;

namespace RealmTodo.ViewModels
{
    public partial class ItemsViewModel : BaseViewModel
    {
        [ObservableProperty]
        private string connectionStatusIcon = "wifi_on.png";

        [ObservableProperty]
        private bool isShowAllTasks;

        [ObservableProperty]
        private ObservableCollection<Item> items;

        private PartitionSyncConfiguration config;
        private Realm realm;
        private string currentUserId;
        private bool isOnline = true;

        [RelayCommand]
        public async void OnAppearing()
        {
            config = new PartitionSyncConfiguration($"{RealmService.CurrentUser.Id}", RealmService.CurrentUser);
            realm = Realm.GetInstance(config);

            await GetTodos();

            /*if (Items.Count == 0)
            {
                GetTodos();
            }*/

            //var currentSubscriptionType = RealmService.GetCurrentSubscriptionType(realm);
            //IsShowAllTasks = currentSubscriptionType == SubscriptionType.All;

            /*realm = RealmService.GetMainThreadRealm();
            currentUserId = RealmService.CurrentUser.Id;
            Items = realm.All<Item>().OrderBy(i => i.Id);

            var currentSubscriptionType = RealmService.GetCurrentSubscriptionType(realm);
            IsShowAllTasks = currentSubscriptionType == SubscriptionType.All;*/
        }
        
        public async Task GetTodos()
        {
            //IsBusy = true;
            try
            {
                var tlist = realm.All<Item>().OrderBy(i => i.Id);
                Items = new ObservableCollection<Item>(tlist);
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayPromptAsync("Error", ex.Message);
            }
            //IsBusy = false;
        }

        [RelayCommand]
        public async Task Logout()
        {
            IsBusy = true;
            await RealmService.LogoutAsync();
            IsBusy = false;

            await Shell.Current.GoToAsync($"//login");
        }

        [RelayCommand]
        public async Task AddItem()
        {
            await Shell.Current.GoToAsync($"itemEdit");
        }

        [RelayCommand]
        public async Task EditItem(Item item)
        {
            if (!await CheckItemOwnership(item))
            {
                return;
            }
            var itemParameter = new Dictionary<string, object>() { { "item", item } };
            await Shell.Current.GoToAsync($"itemEdit", itemParameter);
        }

        [RelayCommand]
        public async Task DeleteItem(Item item)
        {
            if (!await CheckItemOwnership(item))
            {
                return;
            }

            await realm.WriteAsync(() =>
            {
                realm.Remove(item);
            });
        }

        [RelayCommand]
        public void ChangeConnectionStatus()
        {
            isOnline = !isOnline;

            if (isOnline)
            {
                realm.SyncSession.Start();
            }
            else
            {
                realm.SyncSession.Stop();
            }

            ConnectionStatusIcon = isOnline ? "wifi_on.png" : "wifi_off.png";
        }

        private async Task<bool> CheckItemOwnership(Item item)
        {
            if (!item.IsMine)
            {
                await DialogService.ShowAlertAsync("Error", "You cannot modify items not belonging to you", "OK");
                return false;
            }

            return true;
        }

        async partial void OnIsShowAllTasksChanged(bool value)
        {
            //await RealmService.SetSubscription(realm, value ? SubscriptionType.All : SubscriptionType.Mine);
            /*
            await RealmService.SetSubscription(realm, value
                               ? SubscriptionType.All
                               : SubscriptionType.MyHighPriority);*/


            if (!isOnline)
            {
                await DialogService.ShowToast("Switching subscriptions does not affect Realm data when the sync is offline.");
            }
        }
    }
}

