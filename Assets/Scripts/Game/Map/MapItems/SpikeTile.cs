using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class SpikeTile : MonoBehaviour
    {
        public int Damage = 2;
        private Animator _animator;
        private BoxCollider _collider;
        private bool _isSpikeOut = false;
        private int _animIdShowSpike;

        public EasyEvent<TargetableObject> OnTargetEnter = new EasyEvent<TargetableObject>();

        void Start()
        {
            _animator = GetComponent<Animator>();
            _collider = GetComponent<BoxCollider>();

            _animIdShowSpike = Animator.StringToHash("ShowSpike");

            _collider.OnTriggerEnterEvent((other) =>
            {
                if (!_isSpikeOut) { return; }
                if (other.TryGetComponent(out TargetableObject targetable))
                {
                    if (targetable.IsDead) { return; }
                    OnTargetEnter.Trigger(targetable);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        public void ToggleSpike(bool isSpikeOut)
        {
            _animator.SetBool(_animIdShowSpike, isSpikeOut);
            _isSpikeOut = isSpikeOut;
        }
    }
}

