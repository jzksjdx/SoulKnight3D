using UnityEngine;
using UnityEngine.UI;
using QFramework;
using System.Collections;
using System.Collections.Generic;

namespace SoulKnight3D
{
	public class UIInventoryPanelData : UIPanelData
	{
	}
	public partial class UIInventoryPanel : UIPanel
	{
        public List<UISlot> HotbarSlots;

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as UIInventoryPanelData ?? new UIInventoryPanelData();
            // please add init code here
        }

        private void Start()
        {
            ItemKit.AddItemConfig(ItemManager.Instance.Items[0]);

            ItemKit.Slots[0].Item = ItemManager.Instance.Items[0];
            ItemKit.Slots[0].Count = 1;

            //UISlot.Hide();
            InitHotbarSlots();

            BtnAddItem1.onClick.AddListener(() =>
            {
                if (!ItemKit.AddItem(ItemManager.Instance.Items[0].GetKey))
                {
                    Debug.Log("背包已满");
                }
            });

            BtnSubitem1.onClick.AddListener(() =>
            {
                if (!ItemKit.SubItem(ItemManager.Instance.Items[0].GetKey))
                {
                    Debug.Log("数量不足");
                }
            });
        }

        public void InitHotbarSlots()
        {
            for(int i = 0; i < HotbarSlots.Count; i ++)
            {
                HotbarSlots[i].InitWithData(ItemKit.Slots[i]);
                ItemKit.Slots.Add(new Slot(null, 0));
            }
        }

        //     private void OnGUI()
        //     {
        //IMGUIHelper.SetDesignResolution(640, 360);

        //foreach (var slot in ItemKit.Slots)
        //{
        //             GUILayout.BeginHorizontal("box");
        //	if (slot.Count == 0)
        //	{
        //		GUILayout.Label("格子: 空");
        //	} else
        //	{
        //                 GUILayout.Label($"格子: {slot.Item.Name} x {slot.Count}");
        //             }

        //             GUILayout.EndHorizontal();
        //         }

        //         GUILayout.BeginHorizontal();
        //         GUILayout.Label("物品1");
        //         if (GUILayout.Button("+"))
        //         {
        //             if (!ItemKit.AddItem("item_1"))
        //             {
        //                 Debug.Log("物品栏已满");
        //             }
        //         }
        //         if (GUILayout.Button("-")) { ItemKit.SubItem("item_1"); }
        //         GUILayout.EndHorizontal();

        //         GUILayout.BeginHorizontal();
        //         GUILayout.Label("物品2");
        //         if (GUILayout.Button("+")) {
        //             if (!ItemKit.AddItem("item_2"))
        //             {
        //                 Debug.Log("物品栏已满");
        //             }
        //         }
        //         if (GUILayout.Button("-")) { ItemKit.SubItem("item_2"); }
        //         GUILayout.EndHorizontal();

        //         GUILayout.BeginHorizontal();
        //         GUILayout.Label("物品3");
        //         if (GUILayout.Button("+")) {
        //             if (!ItemKit.AddItem("item_3"))
        //             {
        //                 Debug.Log("物品栏已满");
        //             }
        //         }
        //         if (GUILayout.Button("-")) { ItemKit.SubItem("item_3"); }
        //         GUILayout.EndHorizontal();

        //         GUILayout.BeginHorizontal();
        //         GUILayout.Label("物品4");
        //         if (GUILayout.Button("+")) {
        //             if (!ItemKit.AddItem("item_4"))
        //             {
        //                 Debug.Log("物品栏已满");
        //             }
        //         }
        //         if (GUILayout.Button("-")) { ItemKit.SubItem("item_4"); }
        //         GUILayout.EndHorizontal();

        //         GUILayout.BeginHorizontal();
        //         GUILayout.Label("物品5");
        //         if (GUILayout.Button("+")) {
        //             if (!ItemKit.AddItem("item_5"))
        //             {
        //                 Debug.Log("物品栏已满");
        //             }
        //         }
        //         if (GUILayout.Button("-")) { ItemKit.SubItem("item_5"); }
        //         GUILayout.EndHorizontal();
        //     }



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
	}
}
