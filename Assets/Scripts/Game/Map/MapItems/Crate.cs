using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using MoreMountains.Feedbacks;

namespace SoulKnight3D
{
    public class Crate : TargetableObject
    {
        public float DropRate = 0.1f;
        public MMFeedbacks BreakFeedbacks;

        protected override void Start()
        {
            base.Start();
        }

        public override void ApplyDamage(int Damage)
        {
            base.ApplyDamage(Damage);

            if (IsDead)
            {
                float dropChance = Random.Range(0f, 1f);
                if (DropRate >= dropChance)
                {
                    // drop energy
                    GameObjectsManager.Instance.SpawnSun(transform.position + new Vector3(0, 0.275f, 0));
                }
                BreakFeedbacks?.PlayFeedbacks();
            }
        }
    }

}
