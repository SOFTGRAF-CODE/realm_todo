using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Realms;
using Realms.Sync;
using RealmTodo.Models;
using RealmTodo.Services;

namespace RealmTodo.ViewModels
{
    public partial class EditItemViewModel : BaseViewModel, IQueryAttributable
    {
        [ObservableProperty]
        private int? priority;

        [ObservableProperty]
        private Item initialItem;

        [ObservableProperty]
        private string summary;

        [ObservableProperty]
        private string pageHeader;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.Count > 0 && query["item"] != null) // we're editing an Item
            {
                InitialItem = query["item"] as Item;
                Summary = InitialItem.Summary;
                Priority = InitialItem.Priority;
                PageHeader = $"Modify Item {InitialItem.Id}";
            }
            else // we're creating a new item
            {
                Summary = "";
                Priority = 2;
                PageHeader = "Create a New Item";
            }
        }

        [RelayCommand]
        public async Task SaveItem()
        {
            if (string.IsNullOrWhiteSpace(Summary))
                return;
            IsBusy = true;


            try
            {
                var todo =
                    new Item
                    {
                        Summary = Summary,
                        Partition = RealmService.CurrentUser.Id,
                        OwnerId = RealmService.CurrentUser.Profile.Email,
                        Priority = Priority
                    };
                IsBusy = false;

                var config = new PartitionSyncConfiguration($"{RealmService.CurrentUser.Id}", RealmService.CurrentUser);
                var realm = Realm.GetInstance(config);

                //var realm = RealmService.GetMainThreadRealm();
                await realm.WriteAsync(() =>
                {
                    realm.Add(todo);

                });
                //Summary = "";
            }
            catch (Exception ex)
            {

                await Application.Current.MainPage.DisplayPromptAsync("Error", ex.Message);
            }

            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        public async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}

