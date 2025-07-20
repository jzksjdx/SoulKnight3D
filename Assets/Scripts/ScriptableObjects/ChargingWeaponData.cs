using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    [CreateAssetMenu(fileName = "ChargingWeaponData", menuName = "ScriptableObject/ChargingWeaponData")]
    public class ChargeWeaponData : WeaponData
    {
        public int MaxDamage;
        public int MaxCritChance;
        public float ChargeTime;
    }
}