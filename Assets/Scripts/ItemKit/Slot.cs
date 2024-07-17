using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class Slot
    {
        public IItem Item;
        public int Count;
        public EasyEvent Changed = new EasyEvent();

        public Slot(IItem item, int count)
        {
            Item = item;
            Count = count;
        }
    }

}

