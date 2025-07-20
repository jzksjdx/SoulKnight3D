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

        private void Awake()
        {
            InGameData = Instantiate(Data);
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

        protected virtual bool GetIsCritHit()
        {
            return InGameData.CritChance > Random.Range(0, 100);
        }
    }

}
