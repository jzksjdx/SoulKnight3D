using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using MoreMountains.Feedbacks;

namespace SoulKnight3D
{
    public class ChargeWeapon : Gun
    {
        public MMF_Player DrawArrowFeedback;
        [SerializeField] private LineRenderer BowString;
        [SerializeField] private Transform EndPoint1, EndPoint2;
        public Transform MidPoint;

        [Tooltip("Where arrow head is placed on left hand")]
        public Transform ArrowHead;

        private bool _isAttacking = false;
        private bool _canShoot = false;
        [SerializeField] private bool _isCharging = false;
        private float _chargeTime;
        private float _chargeTimeDelta = 0f;

        protected override void Start()
        {
            base.Start();

            PlayerInputs.Instance.OnAttackPerformed.Register((isAttacking) =>
            {
                _isAttacking = isAttacking;
                if (!isAttacking && _isCharging && _canShoot)
                {
                    ShootArrow();
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            PlayerController.Instance.PlayerAttack.OnWeaponSwitched.Register((_, weaponObject) =>
            {
                if (weaponObject == this) { return; }
                ResetChargeProgress();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            SetChargeSpeed();
        }

        private void OnDisable()
        {
            ResetChargeProgress();
        }

        private void OnEnable()
        {
            ResetChargeProgress();
        }

        private void ResetChargeProgress()
        {
            if (!_isCharging) { return; }
            _isCharging = false;
            _chargeTimeDelta = 0f;
            MidPoint.Hide();
            UpdateBowString(false);
            UpdateChargeBar();
            ToggleChargeBar(false);
            _cooldownTimeout = InGameData.Cooldown;
        }

        protected override void Update()
        {
            base.Update();

            if (_isCharging)
            {
                _chargeTimeDelta += Time.deltaTime;
                Transform rightHand = PlayerController.Instance.PlayerAnimation.RightHand;
                MidPoint.position = rightHand.position;
                Vector3 arrowDirection = (ArrowHead.localPosition - MidPoint.localPosition).normalized;
                MidPoint.localRotation = Quaternion.FromToRotation(Vector3.right, arrowDirection);
                UpdateBowString(true);
                UpdateChargeBar();
            }
        }

        public override void Attack()
        {
            if (_cooldownTimeout > 0f) { return; }
            
            if (_isCharging == false)
            {
                _isCharging = true;
                _canShoot = false;
                MidPoint.Show();
                DrawArrowFeedback?.PlayFeedbacks();
                if (PlayerController.Instance == null) { return; }
                PlayerController.Instance.PlayerAnimation.SetAnimationBowDraw(_chargeTime);
                ToggleChargeBar(true);
            }
        }

        public void AllowShoot()
        {
            _canShoot = true;
            // Player already released mouse, shoot right now
            if (!_isAttacking)
            {
                ShootArrow();
            }
        }

        private void ShootArrow()
        {
            _isCharging = false;

            PlayerController.Instance.PlayerAnimation.SetAnimationBowShoot();
            Vector3 targetPosition = PlayerController.Instance.PlayerAttack.target.position;
            Vector3 shootDirection = (targetPosition - shootPoint.position).normalized;

            Bullet newBullet = SpawnBulletFromPool(shootPoint.position);
            Vector3 bulletDirection = DeviateBullet(shootDirection);
            newBullet.SelfRigidbody.velocity = bulletDirection * BulletSpeed;
            newBullet.transform.rotation = Quaternion.LookRotation(bulletDirection);

            _chargeTimeDelta = 0f;

            MidPoint.Hide();
            UpdateBowString(false);
            UpdateChargeBar();
            ToggleChargeBar(false);

            if (InGameData.EnergyCost > 0)
            {
                OnWeaponFired.Trigger();
            }

            //feedback
            ShootFeedback?.PlayFeedbacks();

            _cooldownTimeout = InGameData.Cooldown;
        }

        public override Bullet SpawnBulletFromPool(Vector3 position)
        {
            GameObject newBulletObj = GameObjectsManager.Instance.SpawnBullet(bulletPrefab)
               .Position(position);
            Bullet newBullet = newBulletObj.GetComponent<Bullet>();

            ChargeWeaponData data = InGameData as ChargeWeaponData;
            float actualDamage = _chargeTimeDelta / _chargeTime * (data.MaxDamage - data.Damage) + data.Damage;
            int actualDamageInt = (int)Mathf.Clamp(actualDamage, data.Damage, data.MaxDamage);
            newBullet.InitializeBullet(tag, actualDamageInt, GetIsCritHit(), bulletPrefab);
            newBulletObj.Show();
            return newBullet;
        }

        protected override bool GetIsCritHit()
        {
            ChargeWeaponData data = InGameData as ChargeWeaponData;
            float actualCritChance = _chargeTimeDelta / _chargeTime * (data.MaxCritChance - data.CritChance) + data.CritChance;
            float actualDamageClamped = Mathf.Clamp(actualCritChance, data.CritChance, data.MaxCritChance);
            return actualDamageClamped > Random.Range(0, 100);
        }

        public void UpdateBowString(bool isCharging)
        {
            if (isCharging)
            {
                BowString.positionCount = 3;
                BowString.SetPosition(0, EndPoint1.localPosition);
                BowString.SetPosition(1, MidPoint.localPosition);
                BowString.SetPosition(2, EndPoint2.localPosition);
            } else
            {
                BowString.positionCount = 2;
                BowString.SetPosition(0, EndPoint1.localPosition);
                BowString.SetPosition(1, EndPoint2.localPosition);
            }
        }

        public void SetChargeSpeed(float multiplier = 1)
        {
            _chargeTime = (InGameData as ChargeWeaponData).ChargeTime / multiplier;
        }

        private void UpdateChargeBar()
        {
            PlayerController.Instance.PlayerAttack.SetChargeBarProgress(_chargeTimeDelta / _chargeTime);
        }

        private void ToggleChargeBar(bool isShown)
        {
            PlayerController.Instance.PlayerAttack.ToggleChargeBar(isShown);
        }
    }

}
