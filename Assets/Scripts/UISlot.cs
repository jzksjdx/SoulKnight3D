using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using QFramework;

namespace SoulKnight3D
{
    public class UISlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Image Icon;
        public Text Count;

        public Slot Data { get; set; }

        private bool mDragging = false;

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

        public void UpdateView()
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
            var controller = FindAnyObjectByType<UIInventoryPanel>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(controller.transform as RectTransform, mousePos, null, out var localPos))
            {
                Icon.LocalPosition2D(localPos);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (mDragging || Data.Count == 0) return;
            mDragging = true;

            var controller = FindAnyObjectByType<UIInventoryPanel>();
            Icon.Parent(controller);
            SyncItemToMousePos();
        }

        public void OnDrag(PointerEventData eventData)
        {
           if (mDragging)
            {
                SyncItemToMousePos();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
           if (mDragging)
            {
                Icon.Parent(transform);
                Icon.LocalPositionIdentity();
                // 检测鼠标是否在任意一个UISlot
                var uiSlots = transform.parent.GetComponentsInChildren<UISlot>();
                bool throwItem = true;
                foreach (var uiSlot in uiSlots)
                {
                    var rectTransform = uiSlot.transform as RectTransform;
                    if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition))
                    {
                        throwItem = false;
                         
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
                        break;
                    }
                }

                if (throwItem)
                {
                    Data.Item = null;
                    Data.Count = 0;
                    Data.Changed.Trigger();
                }
            }
        }
    }

}
