using UnityEngine;
using QFramework;

// 1.请在菜单 编辑器扩展/Namespace Settings 里设置命名空间
// 2.命名空间更改后，生成代码之后，需要把逻辑代码文件（非 Designer）的命名空间手动更改
namespace QFramework.Example
{
    public partial class UICraft : ViewController
    {
        void Start()
        {
            // Code Here
            CraftItem.Hide();

            // 需要三个铁块 生成一个 GreenSword


            var craftItem = CraftItem.InstantiateWithParent(Content);

            craftItem.CostItem.Hide();
            craftItem.CostItem.InstantiateWithParent(craftItem.CostItemRoot)
                .Self(self =>
                {
                    self.Icon.sprite = Items.item_iron.GetIcon;
                    self.Count.text = "x 3";
                })
                .Show();

            craftItem.Icon.sprite = Items.item_greensword.GetIcon;
            craftItem.Count.text = "x 1";
            var slotGroup = ItemKit.GetSlotGroupByKey("物品栏");

            void UpdateButtonView()
            {
                var count = slotGroup.GetItemCount(Items.item_iron);
                if (count >= 3)
                {
                    craftItem.BtnCraft.interactable = true;
                }
                else
                {
                    craftItem.BtnCraft.interactable = false;
                }
            }
            
            UpdateButtonView();

            slotGroup.Changed.Register(() => { UpdateButtonView(); })
                .UnRegisterWhenGameObjectDestroyed(craftItem.gameObject);

            craftItem.BtnCraft.onClick.AddListener(() =>
            {
                var count = slotGroup.GetItemCount(Items.item_iron);
                if (count >= 3)
                {
                    slotGroup.SubItem(Items.item_iron, 3);

                    slotGroup.AddItem(Items.item_greensword);
                }
            });
            craftItem.Show();
        }
    }
}