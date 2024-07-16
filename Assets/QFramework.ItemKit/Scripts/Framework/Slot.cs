using System;

namespace QFramework
{
    public class Slot
    {
        public IItem Item;
        public int Count;
        public EasyEvent Changed = new EasyEvent();
        public SlotGroup Group { get; private set; }

        public Slot(IItem item, int count,SlotGroup group)
        {
            Item = item;
            Count = count;
            Group = group;
        }
    }
}