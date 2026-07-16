using UnityEngine;

namespace SoulKnight3D
{
    public class PistolEnemy : Enemy
    {
        public GameObject Weapon;
        private Gun _gun;

        protected override void Start()
        {
            base.Start();
            _gun = Weapon.GetComponent<Gun>();
        }

        public void PistolAttackAnimationEffect()
        {
            if (Player == null) { return; }
            _gun.ShootAtTarget(Player.transform, Player.CameraTarget.transform.position);
        }
    }
}

