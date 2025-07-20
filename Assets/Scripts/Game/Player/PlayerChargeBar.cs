using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    public class PlayerChargeBar : MonoBehaviour
    {
        [SerializeField] private List<SpriteRenderer> ChargeBarGrids;

        public void UpdateChargeBar(float progress) // progress in 0 to 1
        {
            if (progress > 1) { return; }
            int gridNum = Mathf.FloorToInt(progress * 5);
            float currentGridPercent = progress * 5 - gridNum;
            ChargeBarGrids[gridNum].color = new Color(1, 1, 1, currentGridPercent);
        }

        public void ResetChargeBar()
        {
            foreach(SpriteRenderer grid in ChargeBarGrids)
            {
                grid.color = Color.black;
            }
        }
    }

}
