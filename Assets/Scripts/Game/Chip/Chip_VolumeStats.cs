using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;


namespace SoulKnight3D
{
    public class Chip_VolumeStats : Chip, IController
    {
        private float _musicVolume = 1f;
        private float _soundVolume = 1f;
        private float _volumeThreshold = 0.9f;

        private float _originalSpeed = 1f;
        private int _originalArmor;
        private int _originalHealth;
        private int _originalEnergy;

        protected override void Start()
        {
            base.Start();
            ActionKit.DelayFrame(1, () =>
            {
                PlayerStats stats = PlayerController.Instance.PlayerStats;
                _originalSpeed = stats.Speed;
                _originalArmor = stats.MaxArmor;
                _originalHealth = stats.MaxHealth;
                _originalEnergy = stats.MaxEnergy;

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

        public override void ApplyUpgradeEffect()
        {
            base.ApplyUpgradeEffect();
            ApplyAudioVolumeEffect();
        }

        private void ApplyAudioVolumeEffect()
        {
            PlayerStats stats = PlayerController.Instance.PlayerStats;
            if (_isUpgraded)
            {
                //_weapon.InGameData.Cooldown = _weapon.GetPrefabWeaponData().Cooldown;
                if (IsThresholdReached())
                {
                    stats.Speed = _originalSpeed * 1.3f;
                    stats.MaxArmor = _originalArmor + 1;
                    stats.Armor.Value++;
                    stats.Armor.Value--;
                    stats.MaxEnergy = _originalEnergy + 50;
                    stats.Energy.Value++;
                    stats.Energy.Value--;
                    stats.MaxHealth = _originalHealth + 1;
                    stats.Health.Value++;
                    stats.Health.Value--;
                } else
                {
                    stats.Speed = _originalSpeed;
                    stats.MaxArmor = _originalArmor;
                    if (stats.Armor.Value >= _originalArmor) { stats.Armor.Value = _originalArmor; }
                    stats.MaxEnergy = _originalEnergy;
                    if (stats.Energy.Value >= _originalEnergy) { stats.Energy.Value = _originalEnergy; }
                    stats.MaxHealth = _originalHealth;
                    if (stats.Health.Value >= _originalHealth) { stats.Health.Value = _originalHealth; }
                }
               
            }
            else
            {
                if (IsThresholdReached())
                {
                    stats.Speed = _originalSpeed;
                } else
                {
                    stats.Speed = _originalSpeed * 0.5f;
                }
            }
        }

        private bool IsThresholdReached()
        {
            float minVolume = _musicVolume < _soundVolume ? _musicVolume : _soundVolume;
            return minVolume >= _volumeThreshold;
        }

        public IArchitecture GetArchitecture()
        {
            return Global.Interface;
        }
    }

}
