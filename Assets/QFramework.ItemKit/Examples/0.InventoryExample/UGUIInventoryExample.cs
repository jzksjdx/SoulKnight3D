using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

// 1.请在菜单 编辑器扩展/Namespace Settings 里设置命名空间
// 2.命名空间更改后，生成代码之后，需要把逻辑代码文件（非 Designer）的命名空间手动更改
namespace QFramework.Example
{
    public partial class UGUIInventoryExample : ViewController
    {
        // public class MyItemKitLoader : IItemKitLoader
        // {
        //     public ResLoader ResLoader { get; set; }
        //     
        //     public ItemDatabase LoadItemDatabase(string databaseName)
        //     {
        //         return ResLoader.LoadSync<ItemDatabase>(databaseName);
        //     }
        //
        //     public ItemLanguagePackage LoadLanguagePackage(string languagePackageName)
        //     {
        //         return ResLoader.LoadSync<ItemLanguagePackage>(languagePackageName);
        //
        //     }
        //
        //     public void LoadItemDatabaseAsync(string databaseName, Action<ItemDatabase> onLoadFinish)
        //     {
        //     }
        //
        //     public void LoadLanguagePackageAsync(string languagePackageName, Action<ItemLanguagePackage> onLoadFinish)
        //     {
        //     }
        // }

        private static int mIronCount = 10;

        public class MyExcelItem : IItem
        {
            public string GetKey { get; set; }
            public string GetName { get;set; }
            public string GetDescription { get;set; }
            public ItemLanguagePackage.LocaleItem LocaleItem { get; set; }
            public Sprite GetIcon { get;set; }
            public bool GetBoolean(string propertyName)
            {
                if (propertyName == "IsWeapon")
                {
                    return IsWeapon;
                }

                return false;
            }

            public int GetInt(string attributeName)
            {
                throw new NotImplementedException();
            }

            public float GetFloat(string attributeName)
            {
                throw new NotImplementedException();
            }

            public string GetString(string attributeName)
            {
                throw new NotImplementedException();
            }

            public bool IsWeapon { get; set; }
            public bool IsStackable { get;set; }
            public bool GetHasMaxStackableCount { get;set; }
            public int GetMaxStackableCount { get;set; }
        }
        private void Awake()
        {

            var itemTextAsset = Resources.Load<TextAsset>("Items");
            var iconAtlas = Resources.Load<SpriteAtlas>("IconAtlas");
            var itemString = itemTextAsset.text;
            var itemRows = itemString.Split("\n");
            var line = 0;
            
            // key	name	description	icon	stackable	max_count	is_weapon

            var excelItems = new List<MyExcelItem>();
            
            foreach (var itemRow in itemRows)
            {
                if (line == 0)
                {
                    
                }
                else
                {
                    if (itemRow.IsTrimNotNullAndEmpty())
                    {
                        var itemFields = itemRow.Split(",");

                        var key = itemFields[0];
                        var name = itemFields[1];
                        var description = itemFields[2];
                        var iconName = itemFields[3];
                        var stackableString = itemFields[4];
                        var maxCountString = itemFields[5];
                        var isWeaponString = itemFields[6];

                        var item = new MyExcelItem()
                        {
                            GetKey = key,
                            GetName = name,
                            GetDescription = description,
                            GetIcon = iconAtlas.GetSprite(iconName),
                            IsStackable = int.Parse(stackableString) == 1,
                            GetMaxStackableCount = int.Parse(maxCountString),
                            IsWeapon = int.Parse(isWeaponString) == 1,
                        };
                        excelItems.Add(item);
                    }
                }

                line++;
            }
            
            
            itemString.LogInfo();
            // ResKit.Init();

            // var resLoader = ResLoader.Allocate();
            ItemKit.SaverAndLoader = new MySaverAndLoader();
            // ItemKit.Loader = new MyItemKitLoader()
            // {
            //     ResLoader = resLoader
            // };

            ItemKit.LoadItemDatabase("ExampleItemDatabase");
            ItemKit.LoadLanguagePackage("ItemEnglishPackage");

            excelItems.ForEach(item => ItemKit.ItemByKey.Add(item.GetKey, item));
            
            ItemKit.CreateSlotGroup("物品栏")
                .CreateSlot(Items.item_iron, 1)
                .CreateSlot(Items.item_greensword, 1)
                .CreateSlotByKey("iron_sword", 1)
                .CreateSlotByKey("shoe", 1)
                .CreateSlotsByCount(8)
                .OnSlotInitWithData(uiSlot =>
                {
                    uiSlot.OnPointerClickEvent(_ =>
                    {
                        if (uiSlot.Data.Item != null)
                        {
                            Debug.Log(uiSlot.Data.Item.GetName + ":点击了");
                        }
                    }).UnRegisterWhenGameObjectDestroyed(uiSlot.gameObject);
                })
                .OnSlotPointerEnter(uiSlot =>
                {
                    UIItemTip.Show(uiSlot);
                })
                .OnSlotPointerExit(uiSlot =>
                {
                    UIItemTip.Hide();
                });

            ItemKit.CreateSlotGroup("背包")
                .CreateSlotsByCount(20);

            ItemKit.CreateSlotGroup("武器")
                .CreateSlot(null, 0)
                .Condition(item => item.GetBoolean("是武器"));

            ItemKit.CreateSlotGroup("宝箱")
                .CreateSlotsByCount(6);

            ItemKit.CreateSlotGroup("宝箱2")
                .CreateSlot(null, 0)
                .CreateSlot(null, 0)
                .CreateSlot(Items.item_greensword, 1)
                .CreateSlot(null, 0)
                .CreateSlot(null, 0)
                .CreateSlot(null, 0);

            ItemKit.Load();


            var weaponSlot = ItemKit.GetSlotGroupByKey("武器").Slots[0];

            weaponSlot.Changed.Register(() =>
            {
                if (weaponSlot.Count != 0)
                {
                    Debug.Log("切换武器:" + weaponSlot.Item.GetName);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        void Start()
        {
            TreasureBoxExample.Hide();

            BtnAddItem1.onClick.AddListener(() =>
            {
                var result = ItemKit.GetSlotGroupByKey("物品栏").AddItem(Items.item_iron_key, 20);

                Debug.Log("RemainCount:" + result.RemainCount);
                if (!result.Succeed)
                {
                    if (result.MessageType == SlotGroup.MessageTypes.Full)
                    {
                        Debug.Log("背包已满");
                    }
                }
            });

            BtnAddItem2.onClick.AddListener(() =>
            {
                var result = ItemKit.GetSlotGroupByKey("物品栏").AddItem(Items.item_greensword_key);
                if (!result.Succeed)
                {
                    if (result.MessageType == SlotGroup.MessageTypes.Full)
                    {
                        Debug.Log("背包已满");
                    }
                }
            });

            BtnAddItem3.onClick.AddListener(() =>
            {
                var result = ItemKit.GetSlotGroupByKey("物品栏").AddItem(Items.item_paper_key);
                if (!result.Succeed)
                {
                    if (result.MessageType == SlotGroup.MessageTypes.Full)
                    {
                        Debug.Log("背包已满");
                    }
                }
            });


            BtnSubItem1.onClick.AddListener(() =>
            {
                if (!ItemKit.GetSlotGroupByKey("物品栏").SubItem(Items.item_iron_key, 10))
                {
                    Debug.Log("数量不足");
                }
            });

            BtnSubItem2.onClick.AddListener(() =>
            {
                ItemKit.GetSlotGroupByKey("物品栏").SubItem(Items.item_greensword_key);
            });

            BtnSubItem3.onClick.AddListener(() => { ItemKit.GetSlotGroupByKey("物品栏").SubItem(Items.item_paper_key); });

            BtnTreasureBox.onClick.AddListener(() =>
            {
                TreasureBoxExample.SetActive(!TreasureBoxExample.activeSelf);

                if (TreasureBoxExample.activeSelf)
                {
                    TreasureBoxExample.GetComponent<UISlotGroup>()
                        .RefreshWithGroupKey("宝箱");
                }
            });

            BtnTreasureBox2.onClick.AddListener(() =>
            {
                TreasureBoxExample.SetActive(!TreasureBoxExample.activeSelf);

                if (TreasureBoxExample.activeSelf)
                {
                    TreasureBoxExample.GetComponent<UISlotGroup>()
                        .RefreshWithGroupKey("宝箱2");
                }
            });

            BtnShop1.onClick.AddListener(() =>
            {
                var shopConfig = BtnShop1.GetComponent<ShopConfig>();
                UIShop.Title.text = shopConfig.ShopName;

                var sellTable = new ShopSellTable();

                foreach (var sellItem in shopConfig.SellItems)
                {
                    sellTable.Add(sellItem.Item, () => sellItem.Price);
                }


                var buyTable = new ShopBuyTable();

                foreach (var buyItem in shopConfig.BuyItems)
                {
                    if (buyItem.Countable)
                    {
                        buyTable.Add(buyItem.Item, () => buyItem.Price, () => buyItem.Count);
                    }
                    else
                    {
                        buyTable.Add(buyItem.Item, () => buyItem.Price, null);
                    }
                }

                UIShop.ShowWithPriceTables(buyTable, sellTable);
            });

            BtnShop2.onClick.AddListener(() =>
            {
                UIShop.Title.text = "原材料商店";

                UIShop.ShowWithPriceTables(
                    new ShopBuyTable()
                    {
                        Table = new Dictionary<IItem, ShopBuyTable.BuyItem>()
                        {
                            {
                                Items.item_iron, new ShopBuyTable.BuyItem()
                                {
                                    Item = Items.item_iron,
                                    PriceGetter = () =>
                                        ItemKit.GetSlotGroupByKey("物品栏")
                                            .GetItemCount(Items.item_iron) + 5,
                                    CountGetter = () => mIronCount,
                                    OnBuy = () => mIronCount--
                                }
                            }
                        }
                    },
                    new ShopSellTable()
                    {
                        Table = new Dictionary<IItem, ShopSellTable.SellItem>()
                        {
                            {
                                Items.item_iron, new ShopSellTable.SellItem()
                                {
                                    Item = Items.item_iron,
                                    PriceGetter = () => 5,
                                    OnSell = () =>
                                    {
                                        mIronCount++;
                                    },
                                }
                            },
                            {
                                Items.item_greensword, new ShopSellTable.SellItem()
                                {
                                    Item = Items.item_greensword,
                                    PriceGetter = () => ItemKit.GetSlotGroupByKey("物品栏")
                                        .GetItemCount(Items.item_greensword) * 2,
                                    OnSell = () => { },
                                }
                            },
                        }
                    });
            });


            void UpdateLanguageText()
            {
                if (ItemKit.CurrentLanguage == ItemKit.DefaultLanguage)
                {
                    BtnLanguage.GetComponentInChildren<Text>().text = "简->EN";
                }
                else
                {
                    BtnLanguage.GetComponentInChildren<Text>().text = "EN->简";
                }
            }

            UpdateLanguageText();

            BtnLanguage.onClick.AddListener(() =>
            {
                if (ItemKit.CurrentLanguage == ItemKit.DefaultLanguage)
                {
                    ItemKit.LoadLanguagePackage("ItemEnglishPackage");
                }
                else
                {
                    ItemKit.LoadLanguagePackage(ItemKit.DefaultLanguage);
                }

                UpdateLanguageText();
            });

            ActionKit.NextFrame(() =>
            {
                GetComponent<UISlotGroup>().UISlotRoot.GetChild(1)
                    .GetComponent<Selectable>().Select();

            }).Start(this);
        }

        private void OnApplicationQuit()
        {
            ItemKit.Save();
        }
    }
}