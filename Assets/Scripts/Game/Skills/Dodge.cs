using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class Dodge : Skill, IUnRegisterList
    {
        [SerializeField] float DodgeForce = 3f;

        Weapon _weaponCache;
        private int _critChanceCache = 0;

        public List<IUnRegister> UnregisterList { get; } = new List<IUnRegister>();

        protected override void Start()
        {
            base.Start();

            // register starting weapon
            Weapon startWeapon = PlayerController.Instance.PlayerAttack.GetCurrentWeapon();
            _weaponCache = startWeapon;
            _critChanceCache = startWeapon.InGameData.CritChance;
            startWeapon.OnWeaponFired.Register(() =>
            {
                _weaponCache.InGameData.CritChance = _critChanceCache;
            }).AddToUnregisterList(this);

            PlayerController.Instance.PlayerAttack.OnWeaponSwitched.Register((weaponData, weaponObject) =>
            {
                // reset previous weapon
                if (_weaponCache)
                {
                    _weaponCache.InGameData.CritChance = _critChanceCache;
                    this.UnRegisterAll();
                }

                // handle new weapon
                Weapon weapon = weaponObject.GetComponent<Weapon>();
                _weaponCache = weapon;
                _critChanceCache = weaponData.CritChance;
                _weaponCache.OnWeaponFired.Register(() =>
                {
                    _weaponCache.InGameData.CritChance = _critChanceCache;
                }).AddToUnregisterList(this);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        public override void UseSkill()
        {
            base.UseSkill();
            PlayerController.Instance.PlayerAnimation.ToggleDodge(true);
            PlayerController.Instance.PlayerAttack.DisableAttack = true;
            AudioKit.PlaySound("fx_skill_c2");
            // apply force
            Vector2 movementVector = PlayerInputs.Instance.GetMovementVectorNormalized();
            Quaternion rotation = Quaternion.Euler(0, PlayerController.Instance.transform.eulerAngles.y, 0);
            Vector3 rotatedMovementVector = rotation * new Vector3(movementVector.x, 0, movementVector.y);
            Vector3 dodgeDir = rotatedMovementVector.normalized + Vector3.up * 0.1f;
            PlayerController.Instance.SelfRigidbody.AddForce(dodgeDir * DodgeForce, ForceMode.Impulse);
            PlayerController.Instance.PlayerStats.IsInvincible = true;
            Physics.IgnoreLayerCollision(3, 10, true);
        }


        protected override void HandleSkillEnd()
        {
            base.HandleSkillEnd();

            // enforce critical attack
            _weaponCache.InGameData.CritChance = 100;

            PlayerController.Instance.PlayerAnimation.ToggleDodge(false);
            PlayerController.Instance.PlayerAttack.DisableAttack = false;
            PlayerController.Instance.PlayerStats.IsInvincible = true;
            Physics.IgnoreLayerCollision(3, 10, false);
        }
    }

}
