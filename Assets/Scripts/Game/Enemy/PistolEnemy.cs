using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace SoulKnight3D
{
    public class PistolEnemy : Enemy
    {
        public GameObject Weapon;


        protected override void Start()
        {
            base.Start();
        }

        public void PistolAttackAnimationEffect()
        {
            Vector3 direction = PlayerController.Instance.CameraTarget.transform.position - Weapon.transform.position;
            Weapon.GetComponent<Gun>().ShootWithDirection(direction.normalized);
        }
    }
}

