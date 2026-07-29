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
        private readonly HashSet<TargetableObject> _scannedEnemies =
            new HashSet<TargetableObject>();
        private readonly HashSet<TargetableObject> _attackTargets =
            new HashSet<TargetableObject>();
        private readonly Collider[] _rangeHits = new Collider[32];
        private bool _canUseBareHand = false;
        private bool _isBareHandActive;
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
                RefreshTargetsInRange();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            PlayerInputs.Instance.OnAttackPerformed.Register((isAttacking) =>
            {
                if (!isAttacking) { return; }

                if (_playerAttack.AreActionsBlocked ||
                    _playerAttack.IsAttackInputSuppressed)
                {
                    CancelBareHandAttack();
                    return;
                }

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
                _isBareHandActive = false;
                _playerAttack.DisableAttack = false;
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void Update()
        {
            if (_playerAttack != null &&
                _playerAttack.AreActionsBlocked &&
                _isBareHandActive)
            {
                CancelBareHandAttack();
            }

            RefreshTargetsInRange();

            if (_cooldownTimeoutDelta >= 0f)
            {
                _cooldownTimeoutDelta -= Time.deltaTime;
            } 
        }

        public void ToggleBareHand(bool isBareHand)
        {
            _isBareHandActive = isBareHand;
            _playerAttack.DisableAttack = isBareHand;
            _playerAnimation.ToggleBareHandAnimation(isBareHand);
            _playerAttack.ToggleBareHandAttack(isBareHand);
        }

        public void AttackFromAniamtion(bool isRightHand)
        {
            if (_playerAttack.AreActionsBlocked)
            {
                CancelBareHandAttack();
                return;
            }
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

        public void CancelBareHandAttack()
        {
            _movesLeft = 0;
            _cooldownTimeoutDelta = _cooldownTimeout;
            if (!_isBareHandActive) { return; }

            ToggleBareHand(false);
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

        private void RefreshTargetsInRange()
        {
            _scannedEnemies.Clear();
            if (BareHandRange == null || !BareHandRange.enabled ||
                !BareHandRange.gameObject.activeInHierarchy)
            {
                ApplyScannedTargets();
                return;
            }

            Bounds bounds = BareHandRange.bounds;
            Vector3 center = bounds.center;
            Vector3 halfExtents = bounds.extents;
            Quaternion rotation = Quaternion.identity;
            if (BareHandRange is BoxCollider box)
            {
                center = box.transform.TransformPoint(box.center);
                Vector3 scale = box.transform.lossyScale;
                halfExtents = Vector3.Scale(
                    box.size * 0.5f,
                    new Vector3(
                        Mathf.Abs(scale.x),
                        Mathf.Abs(scale.y),
                        Mathf.Abs(scale.z)));
                rotation = box.transform.rotation;
            }

            int hitCount = Physics.OverlapBoxNonAlloc(
                center,
                halfExtents,
                _rangeHits,
                rotation,
                ~0,
                QueryTriggerInteraction.Collide);
            for (int i = 0; i < hitCount; i++)
            {
                if (TryGetEnemyTarget(
                    _rangeHits[i], out TargetableObject enemy))
                {
                    _scannedEnemies.Add(enemy);
                }
                _rangeHits[i] = null;
            }

            ApplyScannedTargets();
        }

        private void ApplyScannedTargets()
        {
            _scannedEnemies.RemoveWhere(IsUnavailableEnemy);
            bool changed = !_enemiesInRange.SetEquals(_scannedEnemies);
            if (changed)
            {
                _enemiesInRange.Clear();
                _enemiesInRange.UnionWith(_scannedEnemies);
            }

            _canUseBareHand = _enemiesInRange.Count > 0;
            if (!_canUseBareHand && _isBareHandActive)
            {
                CancelBareHandAttack();
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
