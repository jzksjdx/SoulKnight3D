using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    public class ItemKit
    {
        public static UISlot CurrentSlotPointerOn = null;

        public static Dictionary<string, SlotGroup> mSlotGroupByKey = new Dictionary<string, SlotGroup>();

        public static SlotGroup GetSlotGroupByKey(string key) => mSlotGroupByKey[key];

        public static SlotGroup CreateSlotGroup(string key)
        {
            var slotGroup = new SlotGroup()
            {
                Key = key
            };
            mSlotGroupByKey.Add(key, slotGroup);
            return slotGroup;
        }

        public static void AddItemConfig(ItemPlant itemConfig)
        {
            if (ItemByKey.ContainsKey(itemConfig.GetKey))
            {
                return;
            }
            ItemByKey.Add(itemConfig.GetKey, itemConfig);
        }
        public static Dictionary<string, ItemPlant> ItemByKey = new Dictionary<string, ItemPlant>();

    }

}
