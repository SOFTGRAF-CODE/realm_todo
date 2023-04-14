using System.Collections.ObjectModel;
using System.Text.Json;
using Realms;
using Realms.Sync;
using RealmTodo.Models;

namespace RealmTodo.Services
{
    public static class RealmService
    {
        private static bool serviceInitialised;

        private static Realms.Sync.App app;

        private static Realm mainThreadRealm;

        private static Realm mainThreadRealmPartition;

        public static User CurrentUser => app.CurrentUser;

        public static async Task Init()
        {
            if (serviceInitialised)
            {
                return;
            }

            using Stream fileStream = await FileSystem.Current.OpenAppPackageFileAsync("atlasConfig.json");
            using StreamReader reader = new StreamReader(fileStream);
            var fileContent = await reader.ReadToEndAsync();

            var config = JsonSerializer.Deserialize<RealmAppConfig>(fileContent,  
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true});

            var appConfiguration = new AppConfiguration(config.AppId)
            {
                BaseUri = new Uri(config.BaseUrl)
            };

            app = Realms.Sync.App.Create(appConfiguration);

            serviceInitialised = true;
        }

        public static Realm GetMainThreadRealm()
        {
            return mainThreadRealm ??= GetRealm();
        }

        public static Realm GetMainThreadRealmPartition()
        {
            return mainThreadRealmPartition ??= GetRealmPartition();
        }

        public static List<Item> GetItems(string texto)
        {
            GetMainThreadRealmPartition();
            return mainThreadRealmPartition.All<Item>().Where(i=> i.Summary.Contains(texto)).ToList();
        }

        public static Realm GetRealmPartition()
        {
            var configPartition = new PartitionSyncConfiguration($"{app.CurrentUser.Id}", app.CurrentUser);
            var realmPartition = Realm.GetInstance(configPartition);
            return realmPartition;
        }


        public static Realm GetRealm()
        {
            var config = new FlexibleSyncConfiguration(app.CurrentUser)
            {
                PopulateInitialSubscriptions = (realm) =>
                {
                    var (query, queryName) = GetQueryForSubscriptionType(realm, SubscriptionType.Mine);
                    realm.Subscriptions.Add(query, new SubscriptionOptions { Name = queryName });
                }
            };

            return Realm.GetInstance(config);
        }

        public static async Task RegisterAsync(string email, string password)
        {
            await app.EmailPasswordAuth.RegisterUserAsync(email, password);
        }

        public static async Task LoginAsync(string email, string password)
        {
            await app.LogInAsync(Credentials.EmailPassword(email, password));

            //This will populate the initial set of subscriptions the first time the realm is opened
            //using var realm = GetRealm();
            //await realm.Subscriptions.WaitForSynchronizationAsync();
        }

        public static async Task LogoutAsync()
        {
            await app.CurrentUser.LogOutAsync();
            mainThreadRealm?.Dispose();
            mainThreadRealm = null;
        }

        public static async Task SetSubscription(Realm realm, SubscriptionType subType)
        {
            if (GetCurrentSubscriptionType(realm) == subType)
            {
                return;
            }

            realm.Subscriptions.Update(() =>
            {
                realm.Subscriptions.RemoveAll(true);

                var (query, queryName) = GetQueryForSubscriptionType(realm, subType);

                realm.Subscriptions.Add(query, new SubscriptionOptions { Name = queryName });
            });

            //There is no need to wait for synchronization if we are disconnected
            if (realm.SyncSession.ConnectionState != ConnectionState.Disconnected)
            {
                await realm.Subscriptions.WaitForSynchronizationAsync();
            }
        }

        public static SubscriptionType GetCurrentSubscriptionType(Realm realm)
        {
            var activeSubscription = realm.Subscriptions.FirstOrDefault();

            return activeSubscription.Name switch
            {
                "all" => SubscriptionType.All,
                "mine" => SubscriptionType.Mine,
                "myHighPri" => SubscriptionType.MyHighPriority,
                _ => throw new InvalidOperationException("Unknown subscription type")
            };
        }

        private static (IQueryable<Item> Query, string Name) GetQueryForSubscriptionType(Realm realm, SubscriptionType subType)
        {
            IQueryable<Item> query = null;
            string queryName = null;

            if (subType == SubscriptionType.Mine)
            {
                query = realm.All<Item>().Where(i => i.OwnerId == CurrentUser.Id);
                queryName = "mine";
            }
            else if (subType == SubscriptionType.MyHighPriority)
            {
                query = realm.All<Item>()
                  .Where(i => i.OwnerId == CurrentUser.Id &&
                         i.Priority < 2);
                queryName = "myHighPri";
            }
            else if (subType == SubscriptionType.All)
            {
                query = realm.All<Item>();
                queryName = "all";
            }
            else
            {
                throw new ArgumentException("Unknown subscription type");
            }

            return (query, queryName);
        }
    }

    public enum SubscriptionType
    {
        Mine,
        MyHighPriority,
        All,
    }

    
    public class RealmAppConfig
    {
        public string AppId { get; set; }

        public string BaseUrl { get; set; }
    }
}

