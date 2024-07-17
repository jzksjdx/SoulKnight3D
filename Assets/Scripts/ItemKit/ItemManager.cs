using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class ItemManager : MonoBehaviour, ISingleton
    {
        public static ItemManager Instance => MonoSingletonProperty<ItemManager>.Instance;

        public List<IItem> Items = new List<IItem>();

        public List<GameObject> ItemGameObjects = new List<GameObject>();

        private void Awake()
        {
            Items.Add(ItemGameObjects[0].GetComponent<ItemPlant>());
        }


        public void OnSingletonInit()
        {

        }
    }
}
