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
            if (ChargeBarGrids.Count == 0) { return; }

            progress = Mathf.Clamp01(progress);
            float scaledProgress = progress * ChargeBarGrids.Count;
            for (int i = 0; i < ChargeBarGrids.Count; i++)
            {
                float gridFill = Mathf.Clamp01(scaledProgress - i);
                ChargeBarGrids[i].color = new Color(1, 1, 1, gridFill);
            }
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
