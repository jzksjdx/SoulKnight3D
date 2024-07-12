using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class SpeedBuff : MonoBehaviour
    {
        public float Duration;
        public float SpeedChange;

        private float _originalSpeed;

        public void ActivateBuff(TargetableObject target)
        {
            _originalSpeed = target.Speed;
            ToggleBuff(target, true);
            ActionKit.Delay(Duration, () =>
            {
                ToggleBuff(target, false);
                Destroy(gameObject);
            }).Start(this);
        }

        public void ToggleBuff(TargetableObject target, bool isBuffOn)
        {
            float toggleFactor = isBuffOn ? 1 : -1;
            target.Speed += _originalSpeed * SpeedChange * toggleFactor;
        }
    }

}
