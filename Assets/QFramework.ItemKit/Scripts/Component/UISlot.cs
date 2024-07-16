using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QFramework
{
    public class UISlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        public Image Icon;
        public Text Count;

        public Slot Data { get; private set; }

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

            Data.Group.TriggerOnSlotInitWithData(this);
            
            return this;
        }
        
        void UpdateView()
        {
            if (Data.Count == 0)
            {
                Icon.Hide();
                Count.text = "";
            }
            else
            {
                if (Data.Item.IsStackable)
                {
                    Count.text = Data.Count.ToString();
                    Count.Show();
                }
                else
                {
                    Count.Hide();
                }
                        
                Icon.Show();
                if (Data.Item.GetIcon)
                {
                    Icon.sprite = Data.Item.GetIcon;
                }
            }
        }

        void SyncItemToMousePos()
        {
            var mousePos = Input.mousePosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform,
                    mousePos,
                    null,
                    out var localPos))
            {
                Icon.LocalPosition2D(localPos);
            }
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (mDragging || Data.Count == 0) return;
            mDragging = true;
            
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

        public void OnEndDrag(PointerEventData eventData)
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
                    var rectTransform = uiSlot.transform as RectTransform;
                    if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition))
                    {
                        if (Data.Count != 0)
                        {
                            // 能放到目标为止才进行物品交换
                            if (uiSlot.Data.Group.CheckCondition(Data.Item))
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
                                uiSlot.Data.Group.Changed.Trigger();
                                Data.Group.Changed.Trigger();
                            }
                        }
                    }
                }
                else
                {
                    Data.Item = null;
                    Data.Count = 0;
                    Data.Changed.Trigger();
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Data.Group.TriggerOnSlotPointerEnter(this);
            ItemKit.CurrentSlotPointerOn = this;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Data.Group.TriggerOnSlotPointerExit(this);

            if (ItemKit.CurrentSlotPointerOn == this)
            {
                ItemKit.CurrentSlotPointerOn = null;
            }
        }

        public UnityEvent OnSelectEvent;
        public UnityEvent OnDeselectEvent;
        public void OnSelect(BaseEventData eventData)
        {
            Data.Group.TriggerOnSlotSelect(this);
            ItemKit.CurrentSlotPointerOn = this;
            OnSelectEvent.Invoke(); 
        }

        public void OnDeselect(BaseEventData eventData)
        {
            OnDeselectEvent.Invoke();
            Data.Group.TriggerOnSlotDeselect(this);
            
            if (ItemKit.CurrentSlotPointerOn == this)
            {
                ItemKit.CurrentSlotPointerOn = null;
            }
        }
    }
}