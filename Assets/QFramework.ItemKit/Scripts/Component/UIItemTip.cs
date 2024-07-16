using System;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework
{
    public class UIItemTip : MonoBehaviour
    {
        public GameObject TipPanel;
        public Image Icon;
        public Text Name;
        public Text Description;

        private static UIItemTip mDefault;

        public static void Show(UISlot slot)
        {
            if (slot.Data.Item != null && slot.Data.Count > 0)
            {
                mDefault.Icon.sprite = slot.Data.Item.GetIcon;
                mDefault.Name.text = slot.Data.Item.GetName;
                mDefault.Description.text = slot.Data.Item.GetDescription;
                mDefault.TipPanel.Show();

                var slotWorldPos2D = slot.Position2D();

                // if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                //         mDefault.TipPanel.transform.parent as RectTransform, mousePos, null,
                //         out var localPos))
                // {


                var panelSize = mDefault.TipPanel.GetComponent<RectTransform>().sizeDelta;
                var screenSize = mDefault.GetComponent<RectTransform>().sizeDelta;
                mDefault.TipPanel.Position2D(slotWorldPos2D);
                var tipPosition = mDefault.TipPanel.LocalPosition2D();

                var topDistance = screenSize.y * 0.5f - (tipPosition.y + panelSize.y * 0.5f);
                var bottomDistance = (tipPosition.y - panelSize.y * 0.5f) - (-screenSize.y * 0.5f);
                var leftDistance = (tipPosition.x - panelSize.x * 0.5f) -  (-screenSize.x * 0.5f);
                var rightDistance = screenSize.x * 0.5f - (tipPosition.x + panelSize.x * 0.5f);

                
                var minDistance = Mathf.Min(topDistance, bottomDistance, leftDistance, rightDistance);
                const int Offset = 40;

                if (Math.Abs(minDistance - topDistance) < 0.01f)
                {
                    mDefault.TipPanel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);
                    mDefault.TipPanel.LocalPosition2D(mDefault.TipPanel.LocalPosition2D() + Vector2.down * Offset);
                }  
                else if (Mathf.Abs(minDistance - bottomDistance) < 0.01f)
                {
                    mDefault.TipPanel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);
                    mDefault.TipPanel.LocalPosition2D(mDefault.TipPanel.LocalPosition2D() + Vector2.up * Offset);
                } else if (Mathf.Abs(minDistance - leftDistance) < 0.01f)
                {
                    mDefault.TipPanel.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
                    mDefault.TipPanel.LocalPosition2D(mDefault.TipPanel.LocalPosition2D() + Vector2.right * Offset);

                } else if (Mathf.Abs(minDistance - rightDistance) < 0.01f)
                {
                    mDefault.TipPanel.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
                    mDefault.TipPanel.LocalPosition2D(mDefault.TipPanel.LocalPosition2D() + Vector2.left * Offset);
                }
            }
        }

        public static void Hide()
        {
            mDefault.TipPanel.Hide();
        }

        private void Awake()
        {
            mDefault = this;
        }

        private void Start()
        {
            mDefault.TipPanel.Hide();
        }

        private void OnDestroy()
        {
            mDefault = null;
        }
    }
}