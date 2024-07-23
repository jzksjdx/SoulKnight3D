using UnityEngine;
using UnityEngine.UI;
using QFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SoulKnight3D
{
	public class UIInventoryPanelData : UIPanelData
	{
	}
	public partial class UIInventoryPanel : UIPanel, IController
    {
        public List<UISlot> HotbarSlots;
        public List<UISlot> BackpackSlots;

        public static EasyEvent<bool> OnToggleInventory = new EasyEvent<bool>();

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as UIInventoryPanelData ?? new UIInventoryPanelData();
            // please add init code here

            StoragePanel.Hide();

            InitHotbarSlots();
            InitBackpackSlots();

            PlayerInputs.Instance.OnInteractPerformed.Register(() =>
            {
                if (StoragePanel.gameObject.activeSelf)
                {
                    CloseBackpack();
                } else
                {
                    OpenBackpack();
                }
            }).UnRegisterWhenGameObjectDestroyed(this);

            PlayerInputs.Instance.OnPausePerformed.Register(() =>
            {
               if (StoragePanel.gameObject.activeSelf)
               {
                    CloseBackpack();
               }
            });
        }

        private void OpenBackpack() 
        {
            PlayRandomSound();
            this.GetSystem<ControlSystem>().ToggleCursor(true);
            StoragePanel.Show();
            OnToggleInventory.Trigger(true);
        }

        private void CloseBackpack()
        {
            PlayRandomSound();
            this.GetSystem<ControlSystem>().ToggleCursor(false);
            StoragePanel.Hide();
            OnToggleInventory.Trigger(false);
        }

        private void PlayRandomSound()
        {
            List<string> soundNames = new List<string>() { "tap", "tap2" };
            AudioKit.PlaySound(soundNames[Random.Range(0, soundNames.Count)]);
        }

        private void Start()
        {

            //BtnAddItem1.onClick.AddListener(() =>
            //{
            //    if (!ItemKit.AddItem(ItemManager.Instance.Items[0].GetKey))
            //    {
            //        Debug.Log("背包已满");
            //    }
            //});

            //BtnSubitem1.onClick.AddListener(() =>
            //{
            //    if (!ItemKit.SubItem(ItemManager.Instance.Items[0].GetKey))
            //    {
            //        Debug.Log("数量不足");
            //    }
            //});
        }

        public void InitHotbarSlots()
        {
            ItemKit.CreateSlotGroup("Hotbar").CreateSlotsByCount(HotbarSlots.Count);
            for (int i = 0; i < HotbarSlots.Count; i++)
            {
                HotbarSlots[i].InitWithData(ItemKit.GetSlotGroupByKey("Hotbar").Slots[i]);
            }
        }

        public void InitBackpackSlots()
        {
            ItemKit.CreateSlotGroup("Backpack").CreateSlotsByCount(BackpackSlots.Count);
            for (int i = 0; i < BackpackSlots.Count; i++)
            {
                BackpackSlots[i].InitWithData(ItemKit.GetSlotGroupByKey("Backpack").Slots[i]);
            }
        }

        protected override void OnOpen(IUIData uiData = null)
		{
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
		}

        public IArchitecture GetArchitecture()
        {
            return Global.Interface;
        }
    }
}
