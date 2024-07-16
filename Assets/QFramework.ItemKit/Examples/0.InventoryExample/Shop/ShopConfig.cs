using System;
using System.Collections.Generic;
using UnityEngine;

namespace QFramework.Example
{
    // Config
    // UI
    public class ShopConfig : MonoBehaviour
    {
        [DisplayLabel("商店名")]
        public string ShopName;
        
        public List<ShopBuyItem> BuyItems;
        public List<ShopSellItem> SellItems;
        
        [Serializable]
        public class ShopBuyItem
        {
            public Item Item;
            public int Price;

            [DisplayLabel("是否是有库存")]
            public bool Countable = false;
            [DisplayIf(nameof(Countable),false,true)]
            public int Count;
        }

        [Serializable]
        public class ShopSellItem
        {
            public Item Item;
            public int Price;
        }
    }
}