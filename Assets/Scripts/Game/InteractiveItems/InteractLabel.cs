using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    public class InteractLabel : MonoBehaviour
    {
        public TextMesh LabelText;

        void Update()
        {
            transform.LookAt(Camera.main.transform);
        }

        public void SetLabelText(string text, WeaponData.WeaponRarity color)
        {
            LabelText.text = text;
            LabelText.color = GetLabelColor(color);
        }

        private Color GetLabelColor(WeaponData.WeaponRarity rarity)
        {
            switch (rarity)
            {
                case WeaponData.WeaponRarity.White:
                    return Color.white;
                case WeaponData.WeaponRarity.Green:
                    return new Color(61f / 255, 226f / 255, 90f / 255);
                case WeaponData.WeaponRarity.Blue:
                    return new Color(21f / 255, 165f / 255, 251f / 255);
                case WeaponData.WeaponRarity.Purple:
                    return new Color(191f / 255, 62f / 255, 202f / 255);
                case WeaponData.WeaponRarity.Orange:
                    return new Color(248f / 255, 138f / 255, 29f / 255);
                case WeaponData.WeaponRarity.Red:
                    return new Color(226f / 255, 27f / 255, 27f / 255);
                default:
                    return Color.white;
            }
        }
    }

}
