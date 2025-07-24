using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class SpeedBuff : Status
    {
        public float SpeedChange;
        private float _originalSpeed;

        public override void ActivateStatus(TargetableObject target)
        {
            base.ActivateStatus(target);
            _originalSpeed = target.Speed;
            ToggleBuff(target, true);
          
        }

        protected override void HandleDespawn()
        {
            ToggleBuff(_target, false);
            base.HandleDespawn();
        }

        public void ToggleBuff(TargetableObject target, bool isBuffOn)
        {
            float toggleFactor = isBuffOn ? 1 : -1;
            target.Speed += _originalSpeed * SpeedChange * toggleFactor;
        }
    }

}
