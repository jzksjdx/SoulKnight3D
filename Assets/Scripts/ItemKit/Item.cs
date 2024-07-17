using UnityEngine;
using System.Collections;

namespace SoulKnight3D
{
    public interface IItem
    {
        public string GetKey { get; }
        public string GetName { get; }
        public Sprite GetIcon { get; }
    }

    public class Item: IItem
    {
        public string Key;
        public string Name;

        public Item(string key, string name)
        {
            Key = key;
            Name = name;
        }

        public string GetKey => Key;
        public string GetName => Name;
        public Sprite GetIcon => null;
    }
}
