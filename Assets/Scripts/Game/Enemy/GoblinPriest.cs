using System.Collections;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public sealed class GoblinPriest : BossEnemy
    {
        private enum AttackType
        {
            Lava,
            Split,
            ProtectiveOrb,
            StarFall
        }

        [Header("References")]
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private CapsuleCollider _collider;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _attackOrigin;
        [SerializeField] private Transform _minimapIcon;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _moveSpeed = 1.2f;
        [SerializeField, Min(0f)] private float _preferredMinDistance = 3.5f;
        [SerializeField, Min(0f)] private float _preferredMaxDistance = 5.5f;
        [SerializeField] private Vector2 _directionChangeInterval = new Vector2(1.2f, 2.4f);

        [Header("Attack Timing")]
        [SerializeField] private Vector2 _attackInterval = new Vector2(2.2f, 3.4f);

        [Header("Lava / Mother Bullets")]
        [SerializeField] private GameObject _swirlBulletPrefab;
        [SerializeField, Min(1)] private int _swirlBulletCount = 6;
        [SerializeField, Min(0f)] private float _swirlBulletSpeed = 4.5f;
        [SerializeField, Min(0)] private int _swirlBulletDamage = 3;

        [Header("Split Line Emitters")]
        [SerializeField] private GameObject _lineEmitterPrefab;
        [SerializeField, Min(1)] private int _lineEmitterCount = 4;
        [SerializeField, Min(0f)] private float _lineEmitterSpeed = 5f;
        [SerializeField, Min(0)] private int _lineEmitterDamage = 2;
        [SerializeField, Range(0f, 90f)] private float _lineEmitterArc = 36f;

        [Header("Protective Orbs")]
        [SerializeField] private GameObject _protectiveOrbPrefab;
        [SerializeField, Min(1)] private int _protectiveOrbCount = 2;
        [SerializeField, Min(0f)] private float _protectiveOrbRadius = 1.15f;
        [SerializeField] private float _protectiveOrbDegreesPerSecond = 85f;
        [SerializeField, Min(0f)] private float _protectiveOrbLifetime = 8f;
        [SerializeField, Min(0)] private int _protectiveOrbDamage = 4;

        [Header("Star Fall")]
        [SerializeField] private GameObject _meteorPrefab;
        [SerializeField] private GameObject _meteorWarningPrefab;
        [SerializeField, Min(1)] private int _normalMeteorCount = 3;
        [SerializeField, Min(1)] private int _enragedMeteorCount = 5;
        [SerializeField, Min(0f)] private float _meteorSpawnInterval = 0.22f;
        [SerializeField, Min(0f)] private float _meteorTargetSpread = 1.6f;
        [SerializeField, Min(0f)] private float _meteorHeight = 6f;
        [SerializeField, Min(0.05f)] private float _meteorFallDuration = 1.05f;
        [SerializeField, Min(0.05f)] private float _meteorRadius = 0.85f;
        [SerializeField, Min(0)] private int _meteorDamage = 5;

        [Header("Death Rewards")]
        [SerializeField, Range(0, 100)] private int _rewardRate = 100;
        [SerializeField] private int[] _rewardValues = { 3, 3, 0, 10 };
        [SerializeField, Min(0f)] private float _deathCleanupDelay = 4f;

        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int LavaTrigger = Animator.StringToHash("LavaBulletAttack");
        private static readonly int SplitTrigger = Animator.StringToHash("SplitBulletAttack");
        private static readonly int ProtectiveOrbTrigger = Animator.StringToHash("ProtectiveOrbAttack");
        private static readonly int StarFallTrigger = Animator.StringToHash("StarFallAttack");
        private static readonly int DeathTrigger = Animator.StringToHash("Die");
        private static readonly int MoveState = Animator.StringToHash("Move");
        private static readonly int LavaState = Animator.StringToHash("LavaBulletAttack");
        private static readonly int SplitState = Animator.StringToHash("SplitBulletAttack");
        private static readonly int ProtectiveOrbState = Animator.StringToHash("ProtectiveOrbAttack");
        private static readonly int StarFallState = Animator.StringToHash("StarFallAttack");

        private PlayerController _player;
        private float _attackTimer;
        private float _directionTimer;
        private float _strafeDirection = 1f;
        private bool _isAttacking;
        private bool _attackStateStarted;
        private int _activeAttackState;
        private AttackType _lastAttack = (AttackType)(-1);

        protected override void Start()
        {
            base.Start();
            CacheReferences();
            _player = PlayerController.Instance;
            ScheduleAttack();
            ChooseMovementDirection();
        }

        private void Update()
        {
            if (IsDead || _animator == null) { return; }

            if (_player == null)
            {
                _player = PlayerController.Instance;
                if (_player == null) { return; }
            }

            if (_isAttacking)
            {
                StopMoving();
                FacePlayer();
                UpdateAttackAnimation();
                return;
            }

            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                StartRandomAttack();
                return;
            }

            MoveAroundPlayer();
        }

        private void FixedUpdate()
        {
            if (IsDead && _rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }

        public override void ApplyDamage(int damage)
        {
            if (IsDead) { return; }

            base.ApplyDamage(damage);
            UpdateBossHealthUI();
            if (!IsDead) { return; }

            StopAllCoroutines();
            StopMoving();
            ResetAttackTriggers();
            _animator?.SetTrigger(DeathTrigger);
            if (_collider != null) { _collider.enabled = false; }
            if (_rigidbody != null) { _rigidbody.isKinematic = true; }
            if (_minimapIcon != null) { _minimapIcon.gameObject.Hide(); }

            UIGamePanel gamePanel = UIKit.GetPanel<UIGamePanel>();
            if (gamePanel != null) { gamePanel.BossHealthRect.Hide(); }

            NotifyDeath();
            EnemyRewardDropSystem.Drop(transform.position, _rewardRate, _rewardValues);
            RecycleStatuses();
            Destroy(gameObject, _deathCleanupDelay);
        }

        public void AnimationLavaBulletAttack()
        {
            if (_swirlBulletPrefab == null) { return; }

            int count = Mathf.Max(1, _swirlBulletCount);
            float phase = Random.Range(0f, 360f);
            for (int i = 0; i < count; i++)
            {
                float angle = phase + i * 360f / count;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                SpawnPooledBullet(_swirlBulletPrefab, direction, _swirlBulletSpeed,
                    _swirlBulletDamage, 1f);
            }
        }

        public void AnimationSplitBulletAttack()
        {
            if (_lineEmitterPrefab == null) { return; }

            Vector3 aimDirection = DirectionToPlayer();
            int count = Mathf.Max(1, _lineEmitterCount);
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float angle = Mathf.Lerp(-_lineEmitterArc * 0.5f, _lineEmitterArc * 0.5f, t);
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * aimDirection;
                SpawnPooledBullet(_lineEmitterPrefab, direction, _lineEmitterSpeed,
                    _lineEmitterDamage, 1f);
            }
        }

        public void AnimationProtectiveOrbAttack()
        {
            if (_protectiveOrbPrefab == null) { return; }

            int count = Mathf.Max(1, _protectiveOrbCount);
            for (int i = 0; i < count; i++)
            {
                GameObject orbObject = Instantiate(_protectiveOrbPrefab, transform.position,
                    Quaternion.identity);
                if (orbObject.TryGetComponent(out PriestOrbitalProjectile orb))
                {
                    orb.Initialize(this, i * 360f / count, _protectiveOrbRadius,
                        _protectiveOrbDegreesPerSecond, _protectiveOrbLifetime,
                        _protectiveOrbDamage);
                }
            }
        }

        public void AnimationStarFallAttack()
        {
            StartCoroutine(SpawnMeteorVolley());
        }

        public void ConfigureReferences(Rigidbody body, CapsuleCollider capsule, Animator animator,
            Transform attackOrigin, Transform minimapIcon, GameObject swirlBulletPrefab,
            GameObject lineEmitterPrefab, GameObject protectiveOrbPrefab, GameObject meteorPrefab,
            GameObject meteorWarningPrefab)
        {
            _rigidbody = body;
            _collider = capsule;
            _animator = animator;
            _attackOrigin = attackOrigin;
            _minimapIcon = minimapIcon;
            _swirlBulletPrefab = swirlBulletPrefab;
            _lineEmitterPrefab = lineEmitterPrefab;
            _protectiveOrbPrefab = protectiveOrbPrefab;
            _meteorPrefab = meteorPrefab;
            _meteorWarningPrefab = meteorWarningPrefab;
        }

        protected override void OnBecameEnraged()
        {
            ScheduleAttack();
        }

        private IEnumerator SpawnMeteorVolley()
        {
            if (_meteorPrefab == null) { yield break; }

            int count = IsEnraged ? _enragedMeteorCount : _normalMeteorCount;
            for (int i = 0; i < count; i++)
            {
                if (IsDead) { yield break; }

                Vector3 target = _player != null ? _player.transform.position : transform.position;
                if (i > 0)
                {
                    Vector2 offset = Random.insideUnitCircle * _meteorTargetSpread;
                    target += new Vector3(offset.x, 0f, offset.y);
                }

                GameObject meteorObject = Instantiate(_meteorPrefab,
                    target + Vector3.up * _meteorHeight, Quaternion.identity);
                if (meteorObject.TryGetComponent(out PriestMeteorProjectile meteor))
                {
                    meteor.Initialize(target, _meteorHeight, _meteorFallDuration,
                        _meteorRadius, _meteorDamage, _meteorWarningPrefab);
                }

                yield return new WaitForSeconds(_meteorSpawnInterval);
            }
        }

        private void StartRandomAttack()
        {
            AttackType nextAttack = (AttackType)Random.Range(0, 4);
            if (nextAttack == _lastAttack)
            {
                nextAttack = (AttackType)(((int)nextAttack + Random.Range(1, 4)) % 4);
            }
            _lastAttack = nextAttack;

            int trigger;
            switch (nextAttack)
            {
                case AttackType.Lava:
                    trigger = LavaTrigger;
                    _activeAttackState = LavaState;
                    break;
                case AttackType.Split:
                    trigger = SplitTrigger;
                    _activeAttackState = SplitState;
                    break;
                case AttackType.ProtectiveOrb:
                    trigger = ProtectiveOrbTrigger;
                    _activeAttackState = ProtectiveOrbState;
                    break;
                default:
                    trigger = StarFallTrigger;
                    _activeAttackState = StarFallState;
                    break;
            }

            FacePlayer();
            StopMoving();
            ResetAttackTriggers();
            _animator.SetTrigger(trigger);
            _isAttacking = true;
            _attackStateStarted = false;
        }

        private void UpdateAttackAnimation()
        {
            AnimatorStateInfo current = _animator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(0);
            if (current.shortNameHash == _activeAttackState ||
                (_animator.IsInTransition(0) && next.shortNameHash == _activeAttackState))
            {
                _attackStateStarted = true;
            }

            if (_attackStateStarted && current.shortNameHash == MoveState &&
                !_animator.IsInTransition(0))
            {
                _isAttacking = false;
                _attackStateStarted = false;
                ScheduleAttack();
            }
        }

        private void MoveAroundPlayer()
        {
            Vector3 toPlayer = _player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            Vector3 radial = distance > 0.001f ? toPlayer / distance : transform.forward;
            Vector3 tangent = Vector3.Cross(Vector3.up, radial) * _strafeDirection;
            Vector3 direction;
            if (distance > _preferredMaxDistance)
            {
                direction = (radial + tangent * 0.35f).normalized;
            }
            else if (distance < _preferredMinDistance)
            {
                direction = (-radial + tangent * 0.35f).normalized;
            }
            else
            {
                direction = tangent;
            }

            _directionTimer -= Time.deltaTime;
            if (_directionTimer <= 0f)
            {
                ChooseMovementDirection();
            }

            Vector3 velocity = direction * _moveSpeed;
            velocity.y = _rigidbody != null ? _rigidbody.velocity.y : 0f;
            if (_rigidbody != null) { _rigidbody.velocity = velocity; }
            FacePlayer();
            UpdateMoveAnimation(velocity);
        }

        private void FacePlayer()
        {
            Vector3 direction = DirectionToPlayer();
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }

        private Vector3 DirectionToPlayer()
        {
            if (_player == null) { return transform.forward; }

            Vector3 direction = _player.transform.position - transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        }

        private void StopMoving()
        {
            if (_rigidbody != null)
            {
                _rigidbody.velocity = new Vector3(0f, _rigidbody.velocity.y, 0f);
                _rigidbody.angularVelocity = Vector3.zero;
            }
            UpdateMoveAnimation(Vector3.zero);
        }

        private void UpdateMoveAnimation(Vector3 worldVelocity)
        {
            if (_animator == null) { return; }

            Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);
            float speed = new Vector2(worldVelocity.x, worldVelocity.z).magnitude;
            float normalizedSpeed = _moveSpeed > 0.001f ? Mathf.Clamp01(speed / _moveSpeed) : 0f;
            _animator.SetFloat(MoveX, localVelocity.x);
            _animator.SetFloat(MoveY, localVelocity.z);
            _animator.SetFloat(SpeedParameter, normalizedSpeed);
        }

        private void SpawnPooledBullet(GameObject prefab, Vector3 direction, float speed,
            int damage, float size)
        {
            if (GameObjectsManager.Instance == null || prefab == null) { return; }

            GameObject bulletObject = GameObjectsManager.Instance.SpawnBullet(prefab);
            if (bulletObject == null || !bulletObject.TryGetComponent(out Bullet bullet)) { return; }

            direction.y = 0f;
            direction.Normalize();
            bulletObject.transform.SetPositionAndRotation(
                _attackOrigin != null ? _attackOrigin.position : transform.position + Vector3.up,
                Quaternion.LookRotation(direction, Vector3.up));
            bullet.InitializeBullet("Enemy", damage, false, prefab, size);
            bullet.SelfRigidbody.velocity = direction * speed;
            bullet.ShowFromPool();
            if (_collider != null)
            {
                bullet.IgnoreCollisionUntilSeparated(_collider, 0.05f);
            }
        }

        private void ScheduleAttack()
        {
            float minimum = Mathf.Max(0.1f, Mathf.Min(_attackInterval.x, _attackInterval.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(_attackInterval.x, _attackInterval.y));
            _attackTimer = Random.Range(minimum, maximum) * AttackIntervalMultiplier;
        }

        private void ChooseMovementDirection()
        {
            _strafeDirection = Random.value < 0.5f ? -1f : 1f;
            float minimum = Mathf.Max(0.1f,
                Mathf.Min(_directionChangeInterval.x, _directionChangeInterval.y));
            float maximum = Mathf.Max(minimum,
                Mathf.Max(_directionChangeInterval.x, _directionChangeInterval.y));
            _directionTimer = Random.Range(minimum, maximum);
        }

        private void ResetAttackTriggers()
        {
            if (_animator == null) { return; }
            _animator.ResetTrigger(LavaTrigger);
            _animator.ResetTrigger(SplitTrigger);
            _animator.ResetTrigger(ProtectiveOrbTrigger);
            _animator.ResetTrigger(StarFallTrigger);
        }

        private void CacheReferences()
        {
            if (_rigidbody == null) { _rigidbody = GetComponent<Rigidbody>(); }
            if (_collider == null) { _collider = GetComponent<CapsuleCollider>(); }
            if (_animator == null) { _animator = GetComponentInChildren<Animator>(); }
            if (_attackOrigin == null) { _attackOrigin = transform; }
        }

        private void UpdateBossHealthUI()
        {
            UIGamePanel gamePanel = UIKit.GetPanel<UIGamePanel>();
            if (gamePanel != null && MaxHealth > 0)
            {
                gamePanel.BossHealthBar.fillAmount = (float)Health.Value / MaxHealth;
            }
        }

        private void RecycleStatuses()
        {
            Status[] statuses = GetComponentsInChildren<Status>();
            for (int i = 0; i < statuses.Length; i++)
            {
                GameObjectsManager.Instance?.DespawnStatus(statuses[i]);
            }
        }
    }
}
