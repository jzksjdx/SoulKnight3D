using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class TargetableObject : MonoBehaviour
    {
        public int MaxHealth;
        public float Speed;
        public BindableProperty<int> Health = new BindableProperty<int>();
        public bool IsDead
        {
            get
            {
                return Health.Value <= 0;
            }
        }

        protected virtual void Start()
        {
            Health.Value = MaxHealth;
        }

        public virtual void ApplyDamage(int Damage)
        {
            Health.Value -= Damage;
        }
    }

}
