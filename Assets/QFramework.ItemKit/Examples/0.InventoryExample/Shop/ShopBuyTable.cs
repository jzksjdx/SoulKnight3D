using System;
using System.Collections.Generic;

namespace QFramework.Example
{
    public class ShopBuyTable
    {
        public class BuyItem
        {
            public IItem Item;
            public Func<int> PriceGetter;
            public Func<int> CountGetter;
            public Action OnBuy;
        }

        public Dictionary<IItem, BuyItem> Table = new Dictionary<IItem, BuyItem>();

        public void Add(IItem item, Func<int> PriceGetter, Func<int> CountGetter)
        {
            Table.Add(item,new BuyItem()
            {
                Item = item,
                PriceGetter = PriceGetter,
                CountGetter = CountGetter,
            });
        }


        public int GetPrice(IItem item)
        {
            return Table[item].PriceGetter();
        }

        public int GetCount(IItem item)
        {
            return Table[item].CountGetter();
        }
    }
}