using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class Weapon : ViewController 
    {
        public EasyEvent OnWeaponFired = new EasyEvent();
        public WeaponData Data;

        protected float _cooldownTimeout = 0f;


        protected virtual void Update()
        {
            if (_cooldownTimeout >= 0f)
            {
                _cooldownTimeout -= Time.deltaTime;
            }
        }

        protected bool GetIsCritHit()
        {
            return Data.CritChance > Random.Range(0, 100);
        }
    }

}
