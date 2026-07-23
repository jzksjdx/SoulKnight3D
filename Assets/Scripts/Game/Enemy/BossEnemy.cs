using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public abstract class BossEnemy : TargetableObject
    {
        [Header("Boss Enrage")]
        [SerializeField, Range(0.05f, 0.95f)] private float _enrageHealthFraction = 0.5f;
        [SerializeField, Min(0)] private int _enrageEnergyOrbCount = 10;
        [SerializeField, Range(0.1f, 1f)] private float _enragedAttackIntervalMultiplier = 0.65f;

        public EasyEvent OnDeath = new EasyEvent();
        public EasyEvent OnEnraged = new EasyEvent();
        public bool IsEnraged { get; private set; }
        protected float AttackIntervalMultiplier => IsEnraged
            ? _enragedAttackIntervalMultiplier
            : 1f;

        protected override void Start()
        {
            base.Start();
            IsEnraged = false;
        }

        public override void ApplyDamage(int damage)
        {
            if (IsDead) { return; }

            base.ApplyDamage(damage);
            if (!IsDead && !IsEnraged &&
                Health.Value <= Mathf.CeilToInt(MaxHealth * _enrageHealthFraction))
            {
                IsEnraged = true;
                EnemyRewardDropSystem.DropEnergy(transform.position, _enrageEnergyOrbCount);
                OnBecameEnraged();
                OnEnraged.Trigger();
            }
        }

        protected virtual void OnBecameEnraged()
        {
        }

        protected void NotifyDeath()
        {
            OnDeath.Trigger();
        }
    }
}
