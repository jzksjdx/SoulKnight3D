using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using QFramework;

namespace SoulKnight3D
{
    public class PoisonStatus : Status
    {
        [SerializeField] private int _damage;
        [SerializeField] private float _damageCooldown;

        private IEnumerator _damageTask;

        public override void ActivateStatus(TargetableObject target)
        {
            base.ActivateStatus(target);

            gameObject.Show();
            _damageTask = AppyDamage();
            StartCoroutine(_damageTask);
        }

        private IEnumerator AppyDamage()
        {
            while(_target != null)
            {
                if (_target.CompareTag("Enemy")) // only apply damage to enemies
                {
                    _target.ApplyDamage(_damage);
                    GameController.Instance.SpawnDamageText(_damage, _target.transform.position);
                }
                
                yield return new WaitForSeconds(_damageCooldown);
            }
        }

        protected override void HandleDespawn()
        {
            base.HandleDespawn();
            StopCoroutine(_damageTask);
            _damageTask = null;
        }
    }

}
