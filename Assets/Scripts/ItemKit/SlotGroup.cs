using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    public class SlotGroup
    {
        public string Key = string.Empty;
        private List<Slot> mSlots = new List<Slot>();
        public List<Slot> Slots => mSlots;

        public SlotGroup CreateSlot(ItemPlant item, int count)
        {
            mSlots.Add(new Slot(item, count));
            return this;
        }

        public SlotGroup CreateSlotsByCount(int count)
        {
            for (var i = 0; i < count; i++) { CreateSlot(null, 0); }
            return this; 
        }

        public Slot FindSlotByKey(string itemKey)
        {
            return mSlots.Find(s => s.Item != null && s.Item.GetKey == itemKey && s.Count != 0);
        }

        public Slot FindEmptySlot() => mSlots.Find(s => s.Count == 0);

        public Slot FindAddableSlot(string itemKey)
        {
            var slot = FindSlotByKey(itemKey);
            if (slot == null)
            {
                slot = FindEmptySlot();
                if (slot != null)
                {
                    slot.Item = ItemKit.ItemByKey[itemKey];
                }
            }
            return slot;
        }

        public bool AddItem(string itemKey, int addCount = 1)
        {
            var slot = FindAddableSlot(itemKey);
            if (slot == null)
            {
                return false;
            }
            slot.Count += addCount;
            return true;
        }

        public bool SubItem(string itemKey, int subCount = 1)
        {
            var slot = FindSlotByKey(itemKey);
            if (slot != null && slot.Count >= subCount)
            {
                slot.Count -= subCount;
                return true;
            }
            return false;
        }
    }

}
