using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class MinimapCam : MonoBehaviour
    {
        private float _inBattleHeight = 10f;
        private float _exploreHeight = 60f;

        public void TogglePosition(bool isInBattle)
        {
            if (isInBattle)
            {
                ActionKit.Lerp(_exploreHeight, _inBattleHeight, 0.3f, (value) =>
                {
                    transform.localPosition = new Vector3(0, value, 0);
                }).Start(this);
            } else
            {
                ActionKit.Lerp(_inBattleHeight, _exploreHeight, 0.3f, (value) =>
                {
                    transform.localPosition = new Vector3(0, value, 0);
                }).Start(this);
            }
        }
    }

}
