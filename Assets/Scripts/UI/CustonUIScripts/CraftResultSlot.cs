using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SoulKnight3D
{
    public class CraftResultSlot : UISlot
    {
        public GameObject ResultPrefab = null;
        public EasyEvent OnItemCrafted = new EasyEvent();

        public override void UpdateView()
        {
            base.UpdateView();

            // Handle Manual drag to backpack
            if (Data.Item == null) { return; }
            if (Data.Item.State == ItemPlant.StorageState.Recipe) { return; }
            PlayRandomSound();
            if (Data.Item.State != ItemPlant.StorageState.Backpack)
            {
                Data.Item.State = ItemPlant.StorageState.Backpack;
                Data.Item.PickUpItem();
                Data.Item.MoveToItemManager();
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (mDragging)
            {
                mDragging = false;
                var canvas = Icon.GetComponent<Canvas>();
                canvas.DestroySelf();
                Icon.LocalPositionIdentity();


                if (ItemKit.CurrentSlotPointerOn)
                {
                    var uiSlot = ItemKit.CurrentSlotPointerOn;
                    if (uiSlot == this) { return; }
                    var rectTransform = uiSlot.transform as RectTransform;
                    if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition))
                    {
                        if (Data.Count > 0)
                        {
                            if (Data.Item.State == ItemPlant.StorageState.Recipe)
                            {
                                CraftItem();
                            }
                            // 物品交换
                            var cachedItem = uiSlot.Data.Item;
                            var cachedCount = uiSlot.Data.Count;

                            uiSlot.Data.Item = Data.Item;
                            uiSlot.Data.Count = Data.Count;

                            Data.Item = cachedItem;
                            Data.Count = cachedCount;

                            uiSlot.Data.Changed.Trigger();
                            Data.Changed.Trigger();
                        }
                    }
                }
                else
                {
                    CraftItem();

                    Data.Item.ThrowFromInventory();
                    AudioKit.PlaySound("swing");
                    Data.Item = null;
                    Data.Count = 0;
                    Data.Changed.Trigger();
                }
            }
        }

        private void CraftItem()
        {
            GameObject newItemplantObj = Instantiate(ResultPrefab, GameObjectsManager.Instance.transform);
            ItemPlant newItemPlant = newItemplantObj.GetComponent<ItemPlant>();
            newItemPlant.PickUpItem();
            newItemPlant.MoveToItemManager();
            Data.Item = newItemPlant;
            OnItemCrafted.Trigger();
        }

        private void PlayRandomSound()
        {
            List<string> soundNames = new List<string>() { "plant", "plant2" };
            AudioKit.PlaySound(soundNames[Random.Range(0, soundNames.Count)]);
        }
    }

}