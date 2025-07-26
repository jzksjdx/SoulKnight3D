using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoulKnight3D
{
    public class PlayerStats : TargetableObject
    {
        [Header("Player Stats")]
        public int MaxArmor = 5;
        public int MaxEnergy = 200;
        public BindableProperty<int> Armor = new BindableProperty<int>(5);
        public BindableProperty<int> Energy = new BindableProperty<int>(200);

        public bool IsInvincible = false;

        private PlayerAnimation _playerAnimation;
        // timeout deltatime
        private float _invincibleTimeout = 0.4f;
        private float _invincibleTimeoutDelta;
        private float _armorRecCdDelta;
        private float _armorRecCd = 4f;
        private float _armorRecTimeoutDelta;
        private float _armorRecTimeout = 1f;

        protected override void Start()
        {
            base.Start();
            Armor.Value = MaxArmor;
            Energy.Value = MaxEnergy;
            _playerAnimation = GetComponent<PlayerAnimation>();

            _invincibleTimeoutDelta = _invincibleTimeout;
        }

        // Update is called once per frame
        void Update()
        {
            // recover armor
            if (Armor.Value < MaxArmor)
            {
                if (_armorRecCdDelta >= 0f)
                {
                    _armorRecCdDelta -= Time.deltaTime;
                }
                else
                {
                    if (_armorRecTimeoutDelta >= 0f)
                    {
                        _armorRecTimeoutDelta -= Time.deltaTime;
                    }
                    else
                    {
                        Armor.Value++;
                        _armorRecTimeoutDelta = _armorRecTimeout;
                    }
                }
            }

            // invinciblility cooldown
            if (IsInvincible)
            {
                _invincibleTimeoutDelta -= Time.deltaTime;
                if (_invincibleTimeoutDelta <= 0f)
                {
                    _invincibleTimeoutDelta = _invincibleTimeout;
                    IsInvincible = false;
                }
            }
        }

        public override void ApplyDamage(int damage)
        {
            if (IsDead) { return; }
            if (IsInvincible) { return; }
            IsInvincible = true;
            if (Armor.Value >= damage)
            {
                Armor.Value -= damage;
            }
            else
            {
                int armorDamage = damage - Armor.Value;
                Armor.Value = 0;
                Health.Value -= armorDamage;
            }
            AudioKit.PlaySound("fx_hit_p1");

            _armorRecTimeoutDelta = _armorRecTimeout;
            _armorRecCdDelta = _armorRecCd;

            if (Health.Value <= 0)
            {
                _playerAnimation.SetAnimationDie();

                ActionKit.Delay(3f, () =>
                {
                    GameController.Instance.ToggleGameFreeze(true);
                    UIEndPanel endPanel = UIKit.OpenPanel<UIEndPanel>();
                    endPanel.UpdateEndTitle(false);
                }).Start(this);
                
            }
        }

        public void RecoverHealth(int amount)
        {
            if (Health.Value + amount >= MaxHealth)
            {
                Health.Value = MaxHealth;
            } else
            {
                Health.Value += amount;
            }
        }

        public void RecoverEnergy(int amount)
        {
            if (Energy.Value + amount >= MaxEnergy)
            {
                Energy.Value = MaxEnergy;
            }
            else
            {
                Energy.Value += amount;
            }
        }
    }

}
