using System;
using System.Collections.Generic;
using System.Linq;
using QFramework.Example;

namespace QFramework
{
    public class SlotGroup
    {
        public string Key = string.Empty;

        private List<Slot> mSlots = new List<Slot>();

        private Func<IItem, bool> mCondition = _ => true;

        public EasyEvent Changed = new EasyEvent();

        public bool CheckCondition(IItem item) => mCondition(item);

        public IReadOnlyList<Slot> Slots => mSlots;

        public int GetItemCount(IItem item) =>
            Slots.Where(slot => slot.Count > 0 && slot.Item == item)
                .Sum(slot => slot.Count);

        public SlotGroup CreateSlot(IItem item, int count)
        {
            mSlots.Add(new Slot(item, count, this));
            Changed.Trigger();
            return this;
        }
        
        public SlotGroup CreateSlotByKey(string itemKey, int count)
        {
            mSlots.Add(new Slot(ItemKit.ItemByKey[itemKey], count, this));
            Changed.Trigger();
            return this;
        }

        public SlotGroup CreateSlotsByCount(int count)
        {
            for (var i = 0; i < count; i++) CreateSlot(null, 0);
            Changed.Trigger();
            return this;
        }

        public Slot FindSlotByKey(string itemKey) =>
            mSlots.Find(s => s.Item != null && s.Item.GetKey == itemKey && s.Count != 0);

        public Slot FindEmptySlot() => mSlots.Find(s => s.Count == 0);


        public Slot FindAddableSlot(string itemKey)
        {
            var item = ItemKit.ItemByKey[itemKey];
            if (!item.IsStackable)
            {
                var slot = FindEmptySlot();
                if (slot != null)
                {
                    slot.Item = item;
                }

                return slot;
            }
            else if (item.IsStackable && !item.GetHasMaxStackableCount)
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
            else
            {
                return FindAddableHasMaxStackableCountSlot(itemKey);
            }
        }

        public Slot FindAddableHasMaxStackableCountSlot(string itemKey)
        {
            foreach (var slot in mSlots)
            {
                if (slot.Count != 0 && slot.Item.GetKey == itemKey && slot.Item.GetMaxStackableCount != slot.Count)
                {
                    return slot;
                }
            }

            var emptySlot = FindEmptySlot();
            if (emptySlot != null)
            {
                emptySlot.Item = ItemKit.ItemByKey[itemKey];
            }

            return emptySlot;
        }

        public struct ItemOperateResult
        {
            public bool Succeed;
            public int RemainCount;
            public MessageTypes MessageType;
        }

        public enum MessageTypes
        {
            Full, // 满了
        }

        public ItemOperateResult AddItem(IItem item, int addCount = 1)
        {
            if (item.IsStackable && item.GetHasMaxStackableCount)
            {
                do
                {
                    var slot = FindAddableHasMaxStackableCountSlot(item.GetKey);
                    if (slot != null)
                    {
                        var canAddCount = slot.Item.GetMaxStackableCount - slot.Count;
                        if (addCount <= canAddCount)
                        {
                            slot.Count += addCount;
                            slot.Changed.Trigger();
                            Changed.Trigger();
                            addCount = 0;
                        }
                        else
                        {
                            slot.Count += canAddCount;
                            slot.Changed.Trigger();
                            Changed.Trigger();

                            addCount -= canAddCount;
                        }
                    }
                    else
                    {
                        return new ItemOperateResult() { Succeed = false, RemainCount = addCount };
                    }
                } while (addCount > 0);

                return new ItemOperateResult() { Succeed = true, RemainCount = 0 };
            }
            else
            {
                var slot = FindAddableSlot(item.GetKey);

                if (slot == null)
                {
                    return new ItemOperateResult() { Succeed = false, };
                }

                slot.Count += addCount;
                slot.Changed.Trigger();
                Changed.Trigger();
                return new ItemOperateResult() { Succeed = true, RemainCount = 0 };
            }
        }

        public ItemOperateResult AddItem(string itemKey, int addCount = 1)
        {
            var item = ItemKit.ItemByKey[itemKey];
            return AddItem(item, addCount);
        }

        public bool SubItem(string itemKey, int subCount = 1)
        {
            return SubItem(ItemKit.ItemByKey[itemKey], subCount);
        }

        public bool SubItem(IItem item, int subCount = 1)
        {
            var slot = FindSlotByKey(item.GetKey);
            if (slot != null && slot.Count >= subCount)
            {
                slot.Count -= subCount;
                slot.Changed.Trigger();
                Changed.Trigger();
                return true;
            }
            else if (slot != null)
            {
                subCount -= slot.Count;
                slot.Count = 0;
                slot.Changed.Trigger();
                return SubItem(item,subCount);
            }

            return false;
        }

        public SlotGroup Condition(Func<IItem, bool> condition)
        {
            mCondition = condition;
            return this;
        }

        private Action<UISlot> mOnSlotInitWithData;
        private Action<UISlot> mOnSlotSelect;
        private Action<UISlot> mOnSlotDeselect;
        private Action<UISlot> mOnSlotPointerEnter;
        private Action<UISlot> mOnSlotPointerExit;

        public void TriggerOnSlotInitWithData(UISlot uiSlot)
        {
            mOnSlotInitWithData?.Invoke(uiSlot);
        }
        public void TriggerOnSlotSelect(UISlot uiSlot)
        {
            mOnSlotSelect?.Invoke(uiSlot);
        }
        public void TriggerOnSlotDeselect(UISlot uiSlot)
        {
            mOnSlotDeselect?.Invoke(uiSlot);
        }

        public void TriggerOnSlotPointerEnter(UISlot uiSlot)
        {
            mOnSlotPointerEnter?.Invoke(uiSlot);
        }
        
        public void TriggerOnSlotPointerExit(UISlot uiSlot)
        {
            mOnSlotPointerExit?.Invoke(uiSlot);
        }

        

        public SlotGroup OnSlotInitWithData(Action<UISlot> onSlotInitWithData)
        {
            mOnSlotInitWithData = onSlotInitWithData;
            return this;
        }

        public SlotGroup OnSlotSelect(Action<UISlot> onSlotSelect)
        {
            mOnSlotSelect = onSlotSelect;
            return this;
        }
        public SlotGroup OnSlotDeselect(Action<UISlot> onSlotDeselect)
        {
            mOnSlotDeselect = onSlotDeselect;
            return this;
        }
        public SlotGroup OnSlotPointerEnter(Action<UISlot> onSlotPointerEnter)
        {
            mOnSlotPointerEnter= onSlotPointerEnter;
            return this;
        }

        public SlotGroup OnSlotPointerExit(Action<UISlot> onSlotPointerExit)
        {
            mOnSlotPointerExit = onSlotPointerExit;
            return this;
        }
    }
}