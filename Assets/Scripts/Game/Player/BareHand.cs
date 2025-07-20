using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using MoreMountains.Feedbacks;

namespace SoulKnight3D
{
    public class BareHand : MonoBehaviour
    {
        [SerializeField] private Collider BareHandRange;
        [SerializeField] private GameObject RightHandSlash, LeftHandSlash, RightHandHitEffect, LeftHandHitEffect;
        [SerializeField] private MMF_Player SlashSound;

        private PlayerAnimation _playerAnimation;
        private PlayerAttack _playerAttack;

        private List<Enemy> _enemiesInRange = new List<Enemy>();
        private bool _canUseBareHand = false;
        [SerializeField] private int _movesLeft = 0;

        private float _cooldownTimeout = 0.1f;
        private float _cooldownTimeoutDelta;
        private int _damage = 4;

        private void Start()
        {
            _playerAnimation = PlayerController.Instance.PlayerAnimation;
            _playerAttack = PlayerController.Instance.PlayerAttack;

            BareHandRange.OnTriggerEnterEvent((other) =>
            {
                if (other.CompareTag("Enemy") == false) { return; }
                if (_playerAttack.GetCurrentWeapon().InGameData.Animation == WeaponData.WeaponAnimation.Melee) { return; }
                //if (_playerStats.Energy.Value >= _playerAttack.GetCurrentWeapon().Data.EnergyCost) { return; } 
                
                if (other.TryGetComponent(out Enemy enemy) == false) { return; }
                _enemiesInRange.Add(enemy);
                _canUseBareHand = true;
                enemy.OnDeath.Register(() =>
                {
                    _enemiesInRange.Remove(enemy);
                    if (_enemiesInRange.Count == 0)
                    {
                        _canUseBareHand = false;
                    }
                }).UnRegisterWhenGameObjectDestroyed(enemy);

            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            BareHandRange.OnTriggerExitEvent((other) =>
            {
                if (other.CompareTag("Enemy") == false) { return; }
                if (other.TryGetComponent(out Enemy enemy) == false) { return; }
                _enemiesInRange.Remove(enemy);
                if (_enemiesInRange.Count == 0)
                {
                    _canUseBareHand = false;
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            PlayerInputs.Instance.OnAttackPerformed.Register((isAttacking) =>
            {
                if (!isAttacking) { return; }


                if (!_canUseBareHand) { return; }

                if (_playerAttack.DisableAttack && _movesLeft < 2)
                {
                    _movesLeft += 1;
                }
                if (!_playerAttack.DisableAttack && _cooldownTimeoutDelta <= 0f)
                {
                    _movesLeft += 1;
                    ToggleBareHand(true);
                }
                
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            _playerAttack.OnWeaponSwitched.Register((_, _) =>
            {
                _movesLeft = 0;
                _cooldownTimeoutDelta = _cooldownTimeout;
                _playerAttack.DisableAttack = false;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void Update()
        {
            if (_cooldownTimeoutDelta >= 0f)
            {
                _cooldownTimeoutDelta -= Time.deltaTime;
            } 
        }

        public void ToggleBareHand(bool isBareHand)
        {
            _playerAttack.DisableAttack = isBareHand;
            _playerAnimation.ToggleBareHandAnimation(isBareHand);
            _playerAttack.ToggleBareHandAttack(isBareHand);
        }

        public void AttackFromAniamtion(bool isRightHand)
        {
            if (_cooldownTimeoutDelta >= 0f) { return; }
            _cooldownTimeoutDelta = _cooldownTimeout;
            
            Vector3 boxSize = new Vector3(1, 1, 0.5f);
            Vector3 attackCenter = transform.position + Vector3.up * 0.5f + PlayerController.Instance.transform.forward * 0.25f;
            Quaternion boxRotation = PlayerController.Instance.transform.rotation;
            bool didHit = false;
            Collider[] targets = Physics.OverlapBox(attackCenter, boxSize / 2, boxRotation);
            foreach (Collider target in targets)
            {
                if (target.CompareTag(tag)) { continue; }

                // handle targetable objects (enemies, room objects)
                if (target.TryGetComponent(out TargetableObject targetableObject))
                {
                    if (targetableObject.IsDead) { continue; }
                    didHit = true;
                    targetableObject.ApplyDamage(_damage);

                    GameController.Instance.SpawnDamageText(_damage, target.ClosestPoint(transform.position));
                    continue;
                }

                // handle bullet hit
                if (target.TryGetComponent(out Bullet bullet))
                {
                    didHit = true;
                    bullet.DestroyBullet();
                }
            }

            // handle effects
            if (isRightHand)
            {
                if (didHit) { RightHandHitEffect.Show(); }
                else { RightHandHitEffect.Hide(); }
                RightHandSlash.Hide();
                RightHandSlash.Show();
            } else
            {
                if (didHit) { LeftHandHitEffect.Show(); }
                else { LeftHandHitEffect.Hide(); }
                LeftHandSlash.Hide();
                LeftHandSlash.Show();
            }
            SlashSound?.PlayFeedbacks();

            _movesLeft -= 1;
            if (_movesLeft <= 0)
            {
                _playerAttack.DisableAttack = false;
                ActionKit.Delay(_cooldownTimeout, () =>
                {
                    ToggleBareHand(false);
                }).Start(this);
            }
        }

        //private void OnDrawGizmosSelected()
        //{
        //    if (PlayerController.Instance == null) { return; }
        //    Vector3 boxSize = new Vector3(1, 1, 0.5f);
        //    Vector3 attackCenter = transform.position + Vector3.up * 0.5f + PlayerController.Instance.transform.forward * 0.25f;
        //    Gizmos.DrawCube(attackCenter, boxSize);
        //}


    }
}
