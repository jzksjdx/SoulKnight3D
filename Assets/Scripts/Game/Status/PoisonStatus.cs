using UnityEngine;

namespace SoulKnight3D
{
    public class PoisonStatus : Status
    {
        [SerializeField] private int _damage;
        [SerializeField] private float _damageCooldown;

        private float _damageCooldownTimer;

        protected override void OnStatusApplied()
        {
            _damageCooldownTimer = 0f;
        }

        protected override void OnStatusTick(float deltaTime)
        {
            if (_target == null) { return; }

            _damageCooldownTimer -= deltaTime;
            if (_damageCooldownTimer > 0f) { return; }

            _damageCooldownTimer = _damageCooldown;
            if (_target.CompareTag("Enemy")) // only apply damage to enemies
            {
                _target.ApplyDamage(_damage);
                GameController.Instance.SpawnDamageText(_damage, _target.transform.position);
            }
        }
    }

}
