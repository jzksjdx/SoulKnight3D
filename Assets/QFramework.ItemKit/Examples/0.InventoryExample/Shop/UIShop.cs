using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using QFramework;

// 1.请在菜单 编辑器扩展/Namespace Settings 里设置命名空间
// 2.命名空间更改后，生成代码之后，需要把逻辑代码文件（非 Designer）的命名空间手动更改
namespace QFramework.Example
{
    public partial class UIShop : ViewController
    {
        public static BindableProperty<int> Coin = new BindableProperty<int>(100);

        private ShopConfig mShopComponent;

        public enum Modes
        {
            Buy,
            Sell
        }

        public Modes Mode = Modes.Buy;
        
        private void Awake()
        {
            mShopComponent = GetComponent<ShopConfig>();
            
            Coin.Value = PlayerPrefs.GetInt(nameof(Coin), Coin.Value);
            Coin.Register((coin) => { PlayerPrefs.SetInt(nameof(Coin), coin); })
                .UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void OnGUI()
        {
            IMGUIHelper.SetDesignResolution(480, 270);
            GUILayout.Label("Coin:" + Coin.Value);
        }


        public void ShowWithPriceTables(ShopBuyTable buyTable,
            ShopSellTable sellTable)
        {
            mBuyTable = buyTable;
            mSellTable = sellTable;
            ShowBuyItems(mBuyTable);
        }

        
        public void ShowBuyItems(ShopBuyTable buyTable)
        {
            Mode = Modes.Buy;
            mBuyTable = buyTable;
            Content.DestroyChildren();
            foreach (var kv in mBuyTable.Table)
            {
                var item = kv.Key;
                var priceGetter = kv.Value.PriceGetter;
                var countGetter = kv.Value.CountGetter;
                var onBuy = kv.Value.OnBuy;
                
                var shopItem = UIShopItem
                    .InstantiateWithParent(Content)
                    .InitWithData(item)
                    .Show();

                var buyItem = item;

                if (countGetter != null)
                {
                    shopItem.Count.text = countGetter().ToString();
                    shopItem.Count.Show();
                }
                else
                {
                    shopItem.Count.Hide();
                }
                Coin.RegisterWithInitValue(coin =>
                {
                    shopItem.UpdateBuyPriceText(Coin.Value,priceGetter());
                }).UnRegisterWhenGameObjectDestroyed(shopItem.gameObject);
                shopItem.BtnBuy.onClick.AddListener(() =>
                {
                    Coin.Value -= mBuyTable.GetPrice(buyItem);
                    ItemKit.GetSlotGroupByKey("物品栏").AddItem(buyItem);
                    shopItem.UpdateBuyPriceText(Coin.Value,priceGetter());
                    onBuy?.Invoke();
                    if (countGetter != null)
                    {
                        shopItem.Count.text = countGetter().ToString();

                        if (countGetter() == 0)
                        {
                            shopItem.BtnBuy.interactable = false;
                        }
                        else
                        {
                            shopItem.UpdateBuyPriceText(Coin.Value,priceGetter());
                        }
                    }
                });
            }

            this.Show();
        }

        private ShopSellTable mSellTable;
        private ShopBuyTable mBuyTable;
        
        public void ShowSellItems(ShopSellTable sellTable)
        {
            mSellTable = sellTable;
            var sellItems = new HashSet<ShopConfig.ShopSellItem>()
            {
            };
            
            foreach (var item in ItemKit.GetSlotGroupByKey("物品栏").Slots.Where(s => s.Count > 0)
                         .Select(s => s.Item)
                         .ToHashSet())
            {
                if (mSellTable != null && mSellTable.Table.ContainsKey(item))
                {
                    sellItems.Add(new ShopConfig.ShopSellItem()
                    {
                        Item = item as Item,
                        Price = mSellTable.GetPrice(item),
                    });
                }
            }
            
            Mode = Modes.Sell;
            Content.DestroyChildren();
            foreach (var shopSellItem in sellItems)
            {
                var shopItem = UIShopItem.InstantiateWithParent(Content);
                shopItem.Icon.sprite = shopSellItem.Item.GetIcon;
                shopItem.Description.text = shopSellItem.Item.GetDescription;
                shopItem.Count.Hide();
                var price = shopSellItem.Price;
                var item = shopSellItem.Item;

                shopItem.BtnBuyText.text = $"出售({price})";
                shopItem.BtnBuy.onClick.AddListener(() =>
                {
                    Coin.Value += price;
                    ItemKit.GetSlotGroupByKey("物品栏").SubItem(item);
                    ShowSellItems(mSellTable);
                    mSellTable.Table[item].OnSell?.Invoke();
                });
                shopItem.Show();
            }
        }
        
        
        void Start()
        {
            BtnBuy.onClick.AddListener(() =>
            {
                ShowBuyItems(mBuyTable);
            });
            
            BtnSell.onClick.AddListener(() =>
            {
                ShowSellItems(mSellTable);
            });
            
            ItemKit.GetSlotGroupByKey("物品栏").Changed.Register(() =>
            {
                if (Mode == Modes.Sell)
                {
                    ShowSellItems(mSellTable); 
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
        
    }
}