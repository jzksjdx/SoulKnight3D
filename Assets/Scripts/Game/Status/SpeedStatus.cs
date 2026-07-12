using UnityEngine;

namespace SoulKnight3D
{
    public class SpeedBuff : Status
    {
        public float SpeedChange;
        private float _originalSpeed;

        protected override void OnStatusApplied()
        {
            _originalSpeed = _target.Speed;
            ToggleBuff(_target, true);
        }

        protected override void OnStatusRemoved()
        {
            if (_target != null)
            {
                ToggleBuff(_target, false);
            }
        }

        public void ToggleBuff(TargetableObject target, bool isBuffOn)
        {
            float toggleFactor = isBuffOn ? 1 : -1;
            target.Speed += _originalSpeed * SpeedChange * toggleFactor;
        }
    }

}
