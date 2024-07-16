using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QFramework
{
    [CreateAssetMenu(menuName = "@ItemKit/Create Item Language Package")]
    public class ItemLanguagePackage : ScriptableObject
    {
        
        [DisplayLabel("物品数据库")]
        public ItemDatabase ItemDatabase;
        
        [DisplayLabel("语言")]
        public string Language;
        
        [Header("本地化物品")]
        public List<LocaleItem> LocaleItems = new List<LocaleItem>();


        private void OnValidate()
        {
            if (ItemDatabase && LocaleItems.Count == 0)
            {
                foreach (var item in ItemDatabase.Items)
                {
                    LocaleItems.Add(new LocaleItem()
                    {
                        Key = item.Key,
                        Name = item.Name,
                        Description = item.Description
                    });
                }
            } else if (ItemDatabase && LocaleItems.Count != 0)
            {
                // itemDB : item_iron、item_paper
                // itemLanguagePackage : 
                LocaleItems.RemoveAll(item => ItemDatabase.Items.All(i => i.Key != item.Key));

                var newLocaleItems = new List<LocaleItem>();
                
                foreach (var item in ItemDatabase.Items)
                {
                    var localeItem2Add = LocaleItems.FirstOrDefault(localeItem => localeItem.Key == item.Key);
                    if (localeItem2Add != null)
                    {
                        newLocaleItems.Add(localeItem2Add);
                    }
                    else
                    {
                        newLocaleItems.Add(new LocaleItem()
                        {
                            Key = item.Key,
                            Name = item.Name,
                            Description = item.Description
                        });
                    }
                }

                LocaleItems = newLocaleItems;
            }
            else if (!ItemDatabase)
            {
                LocaleItems.Clear();
            }
        }


        [Serializable]
        public class LocaleItem
        {
            public string Key;
            [DisplayLabel("名字:")]
            public string Name;
            [DisplayLabel("描述:")]
            public string Description;
        }
    }
}