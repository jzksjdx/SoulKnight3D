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

        private readonly HashSet<TargetableObject> _enemiesInRange =
            new HashSet<TargetableObject>();
        private readonly HashSet<TargetableObject> _attackTargets =
            new HashSet<TargetableObject>();
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
                if (!TryGetEnemyTarget(other, out TargetableObject enemy)) { return; }
                if (_playerAttack.GetCurrentWeapon().InGameData.Animation == WeaponData.WeaponAnimation.Melee) { return; }
                //if (_playerStats.Energy.Value >= _playerAttack.GetCurrentWeapon().Data.EnergyCost) { return; } 

                if (!_enemiesInRange.Add(enemy)) { return; }
                _canUseBareHand = true;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            BareHandRange.OnTriggerExitEvent((other) =>
            {
                if (!TryGetEnemyTarget(other, out TargetableObject enemy)) { return; }
                RemoveEnemy(enemy);
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
            if (_canUseBareHand)
            {
                _enemiesInRange.RemoveWhere(IsUnavailableEnemy);
                _canUseBareHand = _enemiesInRange.Count > 0;
            }

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
            _attackTargets.Clear();
            Collider[] targets = Physics.OverlapBox(attackCenter, boxSize / 2, boxRotation);
            foreach (Collider target in targets)
            {
                if (target.CompareTag(tag)) { continue; }

                // handle targetable objects (enemies, room objects)
                TargetableObject targetableObject =
                    target.GetComponentInParent<TargetableObject>();
                if (targetableObject != null &&
                    !targetableObject.CompareTag(tag) &&
                    _attackTargets.Add(targetableObject))
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
            _attackTargets.Clear();

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

        private static bool TryGetEnemyTarget(
            Collider collider, out TargetableObject enemy)
        {
            enemy = collider != null
                ? collider.GetComponentInParent<TargetableObject>()
                : null;
            return enemy != null && enemy.CompareTag("Enemy") && !enemy.IsDead;
        }

        private static bool IsUnavailableEnemy(TargetableObject enemy)
        {
            return enemy == null || enemy.IsDead ||
                !enemy.gameObject.activeInHierarchy;
        }

        private void RemoveEnemy(TargetableObject enemy)
        {
            if (enemy != null)
            {
                _enemiesInRange.Remove(enemy);
            }
            _canUseBareHand = _enemiesInRange.Count > 0;
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
