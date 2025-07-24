using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class Chip_VolumeAtk : Chip, IController
    {
        private float _musicVolume = 1f;
        private float _soundVolume = 1f;

        private float _minAtkSpeedFactor = 0.3f;
        private float _maxBulletSize = 3f;
        private Weapon _weapon;

        protected override void Start()
        {
            base.Start();

            _isUpgraded = true;
            ActionKit.DelayFrame(1, () =>
            {
                _weapon = PlayerController.Instance.PlayerAttack.GetCurrentWeapon();
                PlayerController.Instance.PlayerAttack.OnWeaponSwitched.Register((weaponData, weaponObject) =>
                {
                    Weapon weapon = weaponObject.GetComponent<Weapon>();
                    _weapon = weapon;
                    ApplyAudioVolumeEffect();
                }).UnRegisterWhenGameObjectDestroyed(this);

                this.GetSystem<AudioSystem>().MusicVolume.RegisterWithInitValue(volume =>
                {
                    _musicVolume = volume;
                    ApplyAudioVolumeEffect();
                }).UnRegisterWhenGameObjectDestroyed(this);

                this.GetSystem<AudioSystem>().SoundVolume.RegisterWithInitValue(volume =>
                {
                    _soundVolume = volume;
                    ApplyAudioVolumeEffect();
                }).UnRegisterWhenGameObjectDestroyed(this);
            }).Start(this);
           
        }

        private void ApplyAudioVolumeEffect()
        {
            if (_isUpgraded)
            {
                _weapon.InGameData.Cooldown = _weapon.GetPrefabWeaponData().Cooldown;
                ModifyWeaponBulletSize();
            } else
            {
                ModifyWeaponCoolDown();
            }
        }

        private void ModifyWeaponCoolDown()
        {
            float minVolume = _musicVolume < _soundVolume ? _musicVolume : _soundVolume;
            float factor = _minAtkSpeedFactor + (1 - _minAtkSpeedFactor) * minVolume;
            float modifiedCd = _weapon.GetPrefabWeaponData().Cooldown / factor;
            _weapon.InGameData.Cooldown = modifiedCd;
        }

        private void ModifyWeaponBulletSize()
        {
            if (_weapon.TryGetComponent(out Gun gun) == false) { return; }
            float minVolume = _musicVolume < _soundVolume ? _musicVolume : _soundVolume;
            float factor = 1f + (_maxBulletSize - 1f) * minVolume;
            gun.BulletSize = factor;
        }

        public override void ApplyUpgradeEffect()
        {
            base.ApplyUpgradeEffect();
            ModifyWeaponBulletSize();

        }

        public IArchitecture GetArchitecture()
        {
            return Global.Interface;
        }
    }

}
