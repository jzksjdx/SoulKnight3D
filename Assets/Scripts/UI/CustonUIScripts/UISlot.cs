using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
    public class UISlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Image Icon;
        public Text Count;

        public Slot Data { get; set; }

        protected bool mDragging = false;

        public UISlot InitWithData(Slot data)
        {
            if (Data != null)
            {
                Data.Changed.UnRegister(UpdateView);
            }
            Data = data;
            Data.Changed.Register(UpdateView)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            UpdateView();
            return this;
        }

        public virtual void UpdateView()
        {
            if (Data.Count == 0 || Data.Item == null)
            {
                Icon.Hide();
                Count.text = "";
            }
            else
            {
                if (Data.Item.GetIcon)
                {
                    Icon.sprite = Data.Item.GetIcon;
                }
                Icon.Show();
                Count.text = Data.Count.ToString();
            }
        }

        void SyncItemToMousePos()
        {
            var mousePos = Input.mousePosition;
            //var controller = FindAnyObjectByType<UIInventoryPanel>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, mousePos, null, out var localPos))
            {
                Icon.LocalPosition2D(localPos); 
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (mDragging || Data.Count == 0) return;
            mDragging = true;
            AudioKit.PlaySound("seedlift");

            var canvas = Icon.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1000;
            SyncItemToMousePos();
        }

        public void OnDrag(PointerEventData eventData)
        {
           if (mDragging)
            {
                SyncItemToMousePos();
            }
        }

        public virtual void OnEndDrag(PointerEventData eventData)
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
                    Data.Item.ThrowFromInventory();
                    AudioKit.PlaySound("swing");
                    Data.Item = null;
                    Data.Count = 0;
                    Data.Changed.Trigger();
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ItemKit.CurrentSlotPointerOn = this;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (ItemKit.CurrentSlotPointerOn == this)
            {
                ItemKit.CurrentSlotPointerOn = null; 
            }
        }
    }

}
