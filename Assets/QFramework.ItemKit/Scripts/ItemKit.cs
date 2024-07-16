using System.Collections;
using System.Collections.Generic;

namespace QFramework
{
    public class ItemKit
    {
        public static IItemKitSaverAndLoader SaverAndLoader = new DefaultItemKitSaverAndLoader();
        public static IItemKitLoader Loader = new DefaultItemKitLoader();
        
        public static UISlot CurrentSlotPointerOn = null;

        public static Dictionary<string, SlotGroup> mSlotGroupByKey = new Dictionary<string, SlotGroup>();

        public static SlotGroup GetSlotGroupByKey(string key) => mSlotGroupByKey[key];

        public static SlotGroup CreateSlotGroup(string key)
        {
            var slotGorup = new SlotGroup()
            {
                Key = key
            };
            mSlotGroupByKey.Add(key,slotGorup);
            return slotGorup;
        }

        public const string DefaultLanguage = "DefaultLanguage";
        public static string CurrentLanguage { get; private set; } = DefaultLanguage;

        public static void LoadItemDatabase(string databaseName)
        {
            var database = Loader.LoadItemDatabase(databaseName);
            foreach (var databaseItem in database.Items)
            {
                AddItemConfig(databaseItem);
            }
        }
        
        public static IEnumerator LoadItemDatabaseAsync(string databaseName)
        {
            bool done = false;
            Loader.LoadItemDatabaseAsync(databaseName,database =>
            {
                foreach (var databaseItem in database.Items)
                {
                    AddItemConfig(databaseItem);
                }

                done = true;
            });

            while (!done)
            {
                yield return null;
            }
        }
        
        public static void LoadLanguagePackage(string languagePackageName)
        {
            if (languagePackageName == DefaultLanguage)
            {
                CurrentLanguage = DefaultLanguage;

                foreach (var item in ItemByKey.Values)
                {
                    item.LocaleItem = null;
                }
            }
            else
            {
                CurrentLanguage = languagePackageName;

                var languagePackage = Loader.LoadLanguagePackage(languagePackageName);
                foreach (var localeItem in languagePackage.LocaleItems)
                {
                    if (ItemByKey.TryGetValue(localeItem.Key, out var item))
                    {
                        item.LocaleItem = localeItem;
                    }
                }
            }
        }
        
        public static IEnumerator LoadLanguagePackageAsync(string languagePackageName)
        {
            var done = false;
            if (languagePackageName == DefaultLanguage)
            {
                CurrentLanguage = DefaultLanguage;

                foreach (var item in ItemByKey.Values)
                {
                    item.LocaleItem = null;
                }

                done = true;
            }
            else
            {
                CurrentLanguage = languagePackageName;

                Loader.LoadLanguagePackageAsync(languagePackageName, languagePackage =>
                {
                    foreach (var localeItem in languagePackage.LocaleItems)
                    {
                        if (ItemByKey.TryGetValue(localeItem.Key, out var item))
                        {
                            item.LocaleItem = localeItem;
                        }
                    }

                    done = true;
                });
            }

            while (!done)
            {
                yield return null;
            }
        }



        public static void AddItemConfig(IItem itemConfig)
        {
            ItemByKey.Add(itemConfig.GetKey, itemConfig);
        }
        
        public static Dictionary<string, IItem> ItemByKey = new Dictionary<string, IItem>();
        
        public static void Save() => SaverAndLoader.Save(mSlotGroupByKey);

        public static void Load() => SaverAndLoader.Load(mSlotGroupByKey);

        public static void Clear() => SaverAndLoader.Clear();
    }
}