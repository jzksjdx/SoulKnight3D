using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class Weapon : ViewController 
    {
        public EasyEvent OnWeaponFired = new EasyEvent();
        [SerializeField] private WeaponData Data;  // game asset
        [HideInInspector] public WeaponData InGameData;

        protected float _cooldownTimeout = 0f;
        private bool _guaranteedCriticalHit;

        private void Awake()
        {
            InGameData = Instantiate(Data);
            OnWeaponFired.Register(ClearGuaranteedCriticalHit);
        }

        protected virtual void Start()
        {

        }

        protected virtual void Update()
        {
            if (_cooldownTimeout >= 0f)
            {
                _cooldownTimeout -= Time.deltaTime;
            }
        }

        public virtual void Attack() { }

        public float GetRemainingCooldown()
        {
            return Mathf.Max(0f, _cooldownTimeout);
        }

        public void SetAttackDelay(float delay)
        {
            _cooldownTimeout = Mathf.Max(0f, delay);
        }

        protected virtual bool GetIsCritHit()
        {
            return HasGuaranteedCriticalHit ||
                InGameData.CritChance > Random.Range(0, 100);
        }

        protected bool HasGuaranteedCriticalHit => _guaranteedCriticalHit;

        public void GrantGuaranteedCriticalHit()
        {
            _guaranteedCriticalHit = true;
        }

        public void ClearGuaranteedCriticalHit()
        {
            _guaranteedCriticalHit = false;
        }

        public WeaponData GetPrefabWeaponData()
        {
            return Data;
        }
    }

}
