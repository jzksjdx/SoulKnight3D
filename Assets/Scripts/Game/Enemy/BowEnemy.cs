using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace SoulKnight3D
{
    public class BowEnemy : Enemy
    {
        public GameObject Weapon;
        [SerializeField] private Transform RightHand;
        private ChargeWeapon _bow;
        private bool _IsAnimatingBowString = false;

        protected override void Start()
        {
            base.Start();

            _bow = Weapon.GetComponent<ChargeWeapon>();
        }

        protected override void Update()
        {
            base.Update();

            if (_IsAnimatingBowString)
            {
                _bow.MidPoint.position = RightHand.position;
                Vector3 arrowDirection = (_bow.ArrowHead.localPosition - _bow.MidPoint.localPosition).normalized;
                _bow.MidPoint.localRotation = Quaternion.FromToRotation(Vector3.right, arrowDirection);
                _bow.UpdateBowString(true);
            }
        }

        private void AnimateBowString()
        {
            _IsAnimatingBowString = true;
            _bow.DrawArrowFeedback?.PlayFeedbacks();
        }

        private void AnimateShootArrow()
        {
            _IsAnimatingBowString = false;
            _bow.UpdateBowString(false);
            // shoot arrow
            Vector3 direction = PlayerController.Instance.CameraTarget.transform.position - _bow.ArrowHead.position;
            Weapon.GetComponent<Gun>().ShootWithDirection(direction.normalized);
        }
    }

}
