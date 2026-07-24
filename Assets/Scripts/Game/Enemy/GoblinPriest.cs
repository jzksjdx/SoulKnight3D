using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using QFramework;
using UnityEngine;
using UnityEngine.Serialization;

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

        private enum MovementPattern
        {
            Orbit,
            CloseIn,
            FallBack,
            Reposition
        }

        [Header("References")]
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private CapsuleCollider _collider;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _attackOrigin;
        [SerializeField] private Transform _minimapIcon;

        [Header("Sound Feedbacks")]
        [SerializeField] private MMF_Player _soundFeedback;
        [SerializeField] private AudioClip _splitterAttackSound;
        [SerializeField] private AudioClip _lavaAttackSound;
        [SerializeField] private AudioClip _starFallAndProtectiveOrbSound;
        [SerializeField] private AudioClip _enragedSound;
        [SerializeField] private AudioClip _deathSound;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _moveSpeed = 1.2f;
        [SerializeField, Min(0f)] private float _preferredMinDistance = 3.5f;
        [SerializeField, Min(0f)] private float _preferredMaxDistance = 5.5f;
        [SerializeField] private Vector2 _directionChangeInterval = new Vector2(1.2f, 2.4f);
        [SerializeField] private Vector2 _moveSpeedMultiplierRange = new Vector2(0.75f, 1f);
        [SerializeField, Min(0f)] private float _movementAcceleration = 4.5f;
        [SerializeField, Min(0f)] private float _turnSpeed = 540f;
        [SerializeField, Range(0f, 1f)] private float _plannedRadialStrength = 0.4f;
        [SerializeField] private Vector2 _orbitWeightRange = new Vector2(0.55f, 1.1f);

        [Header("Movement Animation")]
        [SerializeField, Range(0f, 0.25f)] private float _animationInputDeadZone = 0.04f;
        [SerializeField, Min(0.01f)] private float _animationInputResponse = 8f;

        [Header("Attack Timing")]
        [SerializeField] private Vector2 _attackInterval = new Vector2(2.2f, 3.4f);

        [Header("Lava / Mother Bullets")]
        [SerializeField] private GameObject _swirlBulletPrefab;
        [SerializeField, Min(1)] private int _swirlBulletCount = 6;
        [SerializeField, Min(0f)] private float _swirlBulletSpeed = 4.5f;
        [SerializeField, Min(0)] private int _swirlBulletDamage = 3;
        [SerializeField, Range(0f, 1f)] private float _lavaRepeatChance = 0.5f;
        [SerializeField] private float _lavaRepeatRotationOffset = 30f;

        [Header("Splitter Mother Bullet")]
        [FormerlySerializedAs("_lineEmitterPrefab")]
        [SerializeField] private GameObject _splitterParentBulletPrefab;
        [SerializeField, Min(0f)] private float _splitterParentSpeed = 4.5f;
        [SerializeField, Min(0)] private int _splitterParentDamage = 3;
        [SerializeField, Range(0f, 1f)] private float _splitterRepeatChance = 0.5f;

        [Header("Ground Projectile Placement")]
        [SerializeField, Min(0f)] private float _groundProjectileHeight = 0.35f;

        [Header("Protective Orbs")]
        [SerializeField] private GameObject _protectiveOrbPrefab;
        [SerializeField, Min(1)] private int _protectiveOrbCount = 2;
        [SerializeField, Min(0f)] private float _protectiveOrbRadius = 1.15f;
        [FormerlySerializedAs("_protectiveOrbDegreesPerSecond")]
        [SerializeField] private float _protectiveOrbRotationSpeed = 85f;
        [SerializeField, Min(0f)] private float _protectiveOrbLifetime = 8f;
        [SerializeField, Min(0)] private int _protectiveOrbDamage = 4;
        [SerializeField, Min(0.1f)] private float _protectiveOrbChaseTimeout = 3.5f;
        [SerializeField, Min(0f)] private float _protectiveOrbChaseSpeedMultiplier = 1.35f;
        [SerializeField, Min(0f)] private float _protectiveOrbContactPadding = 0.2f;

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
        [SerializeField] private LayerMask _meteorFloorLayers = 1;
        [SerializeField, Min(0.1f)] private float _meteorFloorProbeHeight = 8f;
        [SerializeField, Min(0.1f)] private float _meteorFloorProbeDistance = 20f;

        [Header("Death Rewards")]
        [SerializeField, Range(0, 100)] private int _rewardRate = 100;
        [SerializeField] private int[] _rewardValues = { 3, 3, 0, 10 };

        private static readonly int MoveX = Animator.StringToHash("MoveX");
        private static readonly int MoveY = Animator.StringToHash("MoveY");
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int LavaTrigger = Animator.StringToHash("LavaBulletAttack");
        private static readonly int SplitTrigger = Animator.StringToHash("SplitBulletAttack");
        private static readonly int ProtectiveOrbTrigger = Animator.StringToHash("ProtectiveOrbAttack");
        private static readonly int StarFallTrigger = Animator.StringToHash("StarFallAttack");
        private static readonly int MoveState = Animator.StringToHash("Move");
        private static readonly int LavaState = Animator.StringToHash("LavaBulletAttack");
        private static readonly int SplitState = Animator.StringToHash("SplitBulletAttack");
        private static readonly int ProtectiveOrbState = Animator.StringToHash("ProtectiveOrbAttack");
        private static readonly int StarFallState = Animator.StringToHash("StarFallAttack");

        private PlayerController _player;
        private float _attackTimer;
        private float _directionTimer;
        private float _strafeDirection = 1f;
        private float _desiredDistance;
        private float _radialPlanBias;
        private float _orbitWeight = 1f;
        private float _moveSpeedMultiplier = 1f;
        private Vector3 _planarVelocity;
        private Vector2 _animationInput;
        private float _animationSpeed;
        private MovementPattern _movementPattern;
        private bool _isAttacking;
        private bool _attackStateStarted;
        private bool _isRepeatAttack;
        private bool _isProtectiveOrbChasing;
        private int _activeAttackState;
        private float _lavaPatternPhase;
        private float _protectiveOrbChaseTimer;
        private AttackType _activeAttackType;
        private AttackType _lastAttack = (AttackType)(-1);
        private MMF_MMSoundManagerSound _soundFeedbackSound;
        private readonly List<PriestOrbitalProjectile> _activeProtectiveOrbs =
            new List<PriestOrbitalProjectile>();

        protected override void Start()
        {
            base.Start();
            CacheReferences();
            ConfigureDeathReferences(_rigidbody, _collider, _animator, _minimapIcon);
            CacheSoundFeedback();
            _player = PlayerController.Instance;
            ScheduleAttack();
            ChooseMovementDirection();
        }

        private void Update()
        {
            if (IsDead)
            {
                return;
            }
            if (_animator == null) { return; }

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

            if (_isProtectiveOrbChasing)
            {
                UpdateProtectiveOrbChase();
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
            if (IsDead && _rigidbody != null && !_rigidbody.isKinematic)
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
        }

        protected override void OnDeathSequenceStarted()
        {
            StopAllCoroutines();
            StopMoving();
            ResetAttackTriggers();
            PlaySoundFeedback(_deathSound);

            UIGamePanel gamePanel = UIKit.GetPanel<UIGamePanel>();
            if (gamePanel != null) { gamePanel.BossHealthRect.Hide(); }

            EnemyRewardDropSystem.Drop(transform.position, _rewardRate, _rewardValues);
        }

        public void AnimationLavaBulletAttack()
        {
            PlaySoundFeedback(_lavaAttackSound);
            if (_swirlBulletPrefab == null) { return; }

            int count = Mathf.Max(1, _swirlBulletCount);
            float phase = _lavaPatternPhase +
                (_isRepeatAttack ? _lavaRepeatRotationOffset : 0f);
            for (int i = 0; i < count; i++)
            {
                float angle = phase + i * 360f / count;
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                SpawnPooledBullet(_swirlBulletPrefab, direction, _swirlBulletSpeed,
                    _swirlBulletDamage, 1f, GroundProjectileOrigin());
            }
        }

        public void AnimationSplitBulletAttack()
        {
            PlaySoundFeedback(_splitterAttackSound);
            SpawnSplitterParent(DirectionToPlayer());
        }

        public void AnimationProtectiveOrbAttack()
        {
            PlaySoundFeedback(_starFallAndProtectiveOrbSound);
            if (_protectiveOrbPrefab == null) { return; }

            int count = Mathf.Max(1, _protectiveOrbCount);
            for (int i = 0; i < count; i++)
            {
                PooledGameObject pooledOrb =
                    GameObjectsManager.Instance?.SpawnPooledObject(
                        _protectiveOrbPrefab, transform.position, Quaternion.identity);
                if (pooledOrb == null) { continue; }
                if (!pooledOrb.TryGetComponent(out PriestOrbitalProjectile orb))
                {
                    pooledOrb.ReleaseToPool();
                    continue;
                }

                orb.Initialize(this, i * 360f / count, _protectiveOrbRadius,
                    _protectiveOrbRotationSpeed, _protectiveOrbLifetime,
                    _protectiveOrbDamage);
                _activeProtectiveOrbs.Add(orb);
                pooledOrb.ShowFromPool();
            }
        }

        public void AnimationStarFallAttack()
        {
            PlaySoundFeedback(_starFallAndProtectiveOrbSound);
            StartCoroutine(SpawnMeteorVolley());
        }

        public void ConfigureReferences(Rigidbody body, CapsuleCollider capsule, Animator animator,
            Transform attackOrigin, Transform minimapIcon, GameObject swirlBulletPrefab,
            GameObject splitterParentBulletPrefab, GameObject protectiveOrbPrefab,
            GameObject meteorPrefab, GameObject meteorWarningPrefab)
        {
            _rigidbody = body;
            _collider = capsule;
            _animator = animator;
            _attackOrigin = attackOrigin;
            _minimapIcon = minimapIcon;
            _swirlBulletPrefab = swirlBulletPrefab;
            _splitterParentBulletPrefab = splitterParentBulletPrefab;
            _protectiveOrbPrefab = protectiveOrbPrefab;
            _meteorPrefab = meteorPrefab;
            _meteorWarningPrefab = meteorWarningPrefab;
        }

        public void ConfigureSoundFeedback(MMF_Player soundFeedback,
            AudioClip splitterAttackSound, AudioClip lavaAttackSound,
            AudioClip starFallAndProtectiveOrbSound, AudioClip enragedSound,
            AudioClip deathSound)
        {
            _soundFeedback = soundFeedback;
            _splitterAttackSound = splitterAttackSound;
            _lavaAttackSound = lavaAttackSound;
            _starFallAndProtectiveOrbSound = starFallAndProtectiveOrbSound;
            _enragedSound = enragedSound;
            _deathSound = deathSound;
        }

        protected override void OnBecameEnraged()
        {
            PlaySoundFeedback(_enragedSound);
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
                target = ResolveFloorPosition(target);

                PooledGameObject pooledMeteor =
                    GameObjectsManager.Instance?.SpawnPooledObject(_meteorPrefab,
                    target + Vector3.up * _meteorHeight, Quaternion.identity);
                if (pooledMeteor != null)
                {
                    if (pooledMeteor.TryGetComponent(
                        out PriestMeteorProjectile meteor))
                    {
                        meteor.Initialize(target, _meteorHeight, _meteorFallDuration,
                            _meteorRadius, _meteorDamage, _meteorWarningPrefab);
                        pooledMeteor.ShowFromPool();
                    }
                    else
                    {
                        pooledMeteor.ReleaseToPool();
                    }
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
            StartAttack(nextAttack, false);
        }

        private void StartAttack(AttackType attackType, bool isRepeat)
        {
            int trigger;
            switch (attackType)
            {
                case AttackType.Lava:
                    trigger = LavaTrigger;
                    _activeAttackState = LavaState;
                    if (!isRepeat)
                    {
                        _lavaPatternPhase = Random.Range(0f, 360f);
                    }
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

            _activeAttackType = attackType;
            _isRepeatAttack = isRepeat;
            _isProtectiveOrbChasing = false;
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
                if (TryStartImmediateRepeat()) { return; }

                _isRepeatAttack = false;
                if (_activeAttackType == AttackType.ProtectiveOrb &&
                    HasActiveProtectiveOrbs())
                {
                    BeginProtectiveOrbChase();
                    return;
                }
                ScheduleAttack();
            }
        }

        private bool TryStartImmediateRepeat()
        {
            if (_isRepeatAttack) { return false; }

            float repeatChance;
            switch (_activeAttackType)
            {
                case AttackType.Lava:
                    repeatChance = _lavaRepeatChance;
                    break;
                case AttackType.Split:
                    repeatChance = _splitterRepeatChance;
                    break;
                default:
                    return false;
            }

            if (Random.value > Mathf.Clamp01(repeatChance)) { return false; }

            StartAttack(_activeAttackType, true);
            return true;
        }

        private void BeginProtectiveOrbChase()
        {
            _isProtectiveOrbChasing = true;
            _protectiveOrbChaseTimer = Mathf.Max(0.1f, _protectiveOrbChaseTimeout);
        }

        private void UpdateProtectiveOrbChase()
        {
            if (!HasActiveProtectiveOrbs())
            {
                EndProtectiveOrbChase();
                return;
            }

            Vector3 toPlayer = _player.transform.position - transform.position;
            toPlayer.y = 0f;
            float contactDistance =
                Mathf.Max(0f, _protectiveOrbRadius + _protectiveOrbContactPadding);
            _protectiveOrbChaseTimer -= Time.deltaTime;
            if (toPlayer.sqrMagnitude <= contactDistance * contactDistance ||
                _protectiveOrbChaseTimer <= 0f)
            {
                EndProtectiveOrbChase();
                return;
            }

            Vector3 direction = toPlayer.normalized;
            Vector3 desiredVelocity = direction *
                (_moveSpeed * MovementSpeedMultiplier *
                 Mathf.Max(0f, _protectiveOrbChaseSpeedMultiplier));
            _planarVelocity = Vector3.MoveTowards(
                _planarVelocity, desiredVelocity, _movementAcceleration * Time.deltaTime);
            if (_rigidbody != null)
            {
                _rigidbody.velocity = new Vector3(
                    _planarVelocity.x, _rigidbody.velocity.y, _planarVelocity.z);
            }
            FacePlayer();
            UpdateMoveAnimation(_planarVelocity);
        }

        private bool HasActiveProtectiveOrbs()
        {
            for (int i = _activeProtectiveOrbs.Count - 1; i >= 0; i--)
            {
                PriestOrbitalProjectile orb = _activeProtectiveOrbs[i];
                if (orb == null || !orb.IsActive)
                {
                    _activeProtectiveOrbs.RemoveAt(i);
                }
            }
            return _activeProtectiveOrbs.Count > 0;
        }

        private void EndProtectiveOrbChase()
        {
            _isProtectiveOrbChasing = false;
            ChooseMovementDirection();
            ScheduleAttack();
        }

        private void MoveAroundPlayer()
        {
            _directionTimer -= Time.deltaTime;
            if (_directionTimer <= 0f)
            {
                ChooseMovementDirection();
            }

            Vector3 toPlayer = _player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            Vector3 radial = distance > 0.001f ? toPlayer / distance : transform.forward;
            Vector3 tangent = Vector3.Cross(Vector3.up, radial) * _strafeDirection;

            float distanceRange = Mathf.Max(
                0.5f, Mathf.Abs(_preferredMaxDistance - _preferredMinDistance));
            float radialCorrection = Mathf.Clamp(
                (distance - _desiredDistance) / distanceRange, -1f, 1f);
            float radialIntent = Mathf.Clamp(
                radialCorrection + _radialPlanBias, -1f, 1f);
            Vector3 direction = radial * radialIntent + tangent * _orbitWeight;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = tangent.sqrMagnitude > 0.0001f ? tangent : transform.forward;
            }
            direction.Normalize();

            Vector3 desiredVelocity = direction *
                (_moveSpeed * _moveSpeedMultiplier * MovementSpeedMultiplier);
            _planarVelocity = Vector3.MoveTowards(
                _planarVelocity, desiredVelocity, _movementAcceleration * Time.deltaTime);
            if (_rigidbody != null)
            {
                _rigidbody.velocity = new Vector3(
                    _planarVelocity.x, _rigidbody.velocity.y, _planarVelocity.z);
            }
            FacePlayer();
            UpdateMoveAnimation(_planarVelocity);
        }

        private void FacePlayer()
        {
            Vector3 direction = DirectionToPlayer();
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRotation, _turnSpeed * Time.deltaTime);
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
            _planarVelocity = Vector3.zero;
            if (_rigidbody != null)
            {
                _rigidbody.velocity = new Vector3(0f, _rigidbody.velocity.y, 0f);
                _rigidbody.angularVelocity = Vector3.zero;
            }
            SetMoveAnimationImmediate(Vector2.zero, 0f);
        }

        private void UpdateMoveAnimation(Vector3 worldVelocity)
        {
            if (_animator == null) { return; }

            Vector3 planarVelocity = new Vector3(worldVelocity.x, 0f, worldVelocity.z);
            float speed = planarVelocity.magnitude;
            float targetSpeed = _moveSpeed > 0.001f
                ? Mathf.Clamp01(speed / _moveSpeed)
                : 0f;
            Vector2 targetInput = Vector2.zero;
            if (targetSpeed <= _animationInputDeadZone)
            {
                targetSpeed = 0f;
            }
            else
            {
                Vector3 localDirection = transform.InverseTransformDirection(
                    planarVelocity / speed);
                targetInput = Vector2.ClampMagnitude(
                    new Vector2(localDirection.x, localDirection.z), 1f);
                targetInput.x = StabilizeAnimationComponent(targetInput.x);
                targetInput.y = StabilizeAnimationComponent(targetInput.y);
            }

            float maximumDelta = _animationInputResponse * Time.deltaTime;
            _animationInput = Vector2.MoveTowards(
                _animationInput, targetInput, maximumDelta);
            _animationInput = Vector2.ClampMagnitude(_animationInput, 1f);
            _animationInput.x = SnapAnimationComponentWhenSettled(
                _animationInput.x, targetInput.x);
            _animationInput.y = SnapAnimationComponentWhenSettled(
                _animationInput.y, targetInput.y);
            _animationSpeed = Mathf.MoveTowards(
                _animationSpeed, targetSpeed, maximumDelta);
            if (targetSpeed == 0f && _animationSpeed < _animationInputDeadZone)
            {
                _animationSpeed = 0f;
            }

            float displayedSpeed = _animationInput.sqrMagnitude > 0.000001f
                ? Mathf.Clamp01(_animationSpeed)
                : 0f;
            _animator.SetFloat(MoveX, _animationInput.x);
            _animator.SetFloat(MoveY, _animationInput.y);
            _animator.SetFloat(SpeedParameter, displayedSpeed);
        }

        private float StabilizeAnimationComponent(float value)
        {
            return Mathf.Abs(value) < _animationInputDeadZone
                ? 0f
                : Mathf.Clamp(value, -1f, 1f);
        }

        private float SnapAnimationComponentWhenSettled(float value, float target)
        {
            if (target == 0f && Mathf.Abs(value) < _animationInputDeadZone)
            {
                return 0f;
            }
            return Mathf.Clamp(value, -1f, 1f);
        }

        private void SetMoveAnimationImmediate(Vector2 input, float speed)
        {
            _animationInput = Vector2.ClampMagnitude(input, 1f);
            _animationSpeed = Mathf.Clamp01(speed);
            if (_animator == null) { return; }

            _animator.SetFloat(MoveX, _animationInput.x);
            _animator.SetFloat(MoveY, _animationInput.y);
            _animator.SetFloat(SpeedParameter, _animationSpeed);
        }

        private void SpawnPooledBullet(GameObject prefab, Vector3 direction, float speed,
            int damage, float size, Vector3 spawnPosition)
        {
            if (GameObjectsManager.Instance == null || prefab == null) { return; }

            GameObject bulletObject = GameObjectsManager.Instance.SpawnBullet(prefab);
            if (bulletObject == null || !bulletObject.TryGetComponent(out Bullet bullet)) { return; }

            direction.y = 0f;
            direction.Normalize();
            bulletObject.transform.SetPositionAndRotation(
                spawnPosition,
                Quaternion.LookRotation(direction, Vector3.up));
            bullet.InitializeBullet("Enemy", damage, false, prefab, size);
            bullet.SelfRigidbody.velocity = direction * speed;
            bullet.ShowFromPool();
            if (_collider != null)
            {
                bullet.IgnoreCollisionUntilSeparated(_collider, 0.05f);
            }
        }

        private void SpawnSplitterParent(Vector3 direction)
        {
            if (_splitterParentBulletPrefab == null ||
                GameObjectsManager.Instance == null)
            {
                return;
            }

            GameObject bulletObject =
                GameObjectsManager.Instance.SpawnBullet(_splitterParentBulletPrefab);
            if (bulletObject == null) { return; }

            Bullet bullet = bulletObject.GetComponent<Bullet>();
            PriestSplitterParentBullet splitterParent =
                bulletObject.GetComponent<PriestSplitterParentBullet>();
            if (bullet == null || splitterParent == null)
            {
                if (bullet != null)
                {
                    GameObjectsManager.Instance.DespawnBullet(bullet);
                }
                return;
            }

            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : transform.forward;
            bulletObject.transform.SetPositionAndRotation(
                GroundProjectileOrigin(),
                Quaternion.LookRotation(direction, Vector3.up));
            bullet.InitializeBullet("Enemy", _splitterParentDamage, false,
                _splitterParentBulletPrefab);
            splitterParent.Arm(direction);
            bullet.SelfRigidbody.velocity = direction * _splitterParentSpeed;
            bullet.ShowFromPool();
            if (_collider != null)
            {
                bullet.IgnoreCollisionUntilSeparated(_collider, 0.05f);
            }
        }

        private Vector3 GroundProjectileOrigin()
        {
            Vector3 origin = _attackOrigin != null
                ? _attackOrigin.position
                : transform.position;
            origin.y = transform.position.y + Mathf.Max(0f, _groundProjectileHeight);
            return origin;
        }

        private Vector3 ResolveFloorPosition(Vector3 target)
        {
            float fallbackFloorY = transform.position.y;
            float rayStartY = Mathf.Max(target.y, fallbackFloorY) +
                Mathf.Max(0.1f, _meteorFloorProbeHeight);
            Vector3 rayOrigin = new Vector3(target.x, rayStartY, target.z);
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                Mathf.Max(0.1f, _meteorFloorProbeDistance), _meteorFloorLayers,
                QueryTriggerInteraction.Ignore))
            {
                target.y = hit.point.y;
            }
            else
            {
                target.y = fallbackFloorY;
            }
            return target;
        }

        private void ScheduleAttack()
        {
            float minimum = Mathf.Max(0.1f, Mathf.Min(_attackInterval.x, _attackInterval.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(_attackInterval.x, _attackInterval.y));
            _attackTimer = Random.Range(minimum, maximum) * AttackIntervalMultiplier;
        }

        private void ChooseMovementDirection()
        {
            if (_directionTimer <= 0f && Random.value < 0.55f)
            {
                _strafeDirection *= -1f;
            }

            _movementPattern = (MovementPattern)Random.Range(0, 4);
            _desiredDistance = Random.Range(
                Mathf.Min(_preferredMinDistance, _preferredMaxDistance),
                Mathf.Max(_preferredMinDistance, _preferredMaxDistance));
            float plannedStrength = Mathf.Clamp01(_plannedRadialStrength);
            switch (_movementPattern)
            {
                case MovementPattern.CloseIn:
                    _radialPlanBias = plannedStrength;
                    break;
                case MovementPattern.FallBack:
                    _radialPlanBias = -plannedStrength;
                    break;
                case MovementPattern.Reposition:
                    _radialPlanBias = Random.Range(
                        -plannedStrength * 0.5f, plannedStrength * 0.5f);
                    break;
                default:
                    _radialPlanBias = 0f;
                    break;
            }

            float minimumOrbitWeight = Mathf.Max(
                0.05f, Mathf.Min(_orbitWeightRange.x, _orbitWeightRange.y));
            float maximumOrbitWeight = Mathf.Max(
                minimumOrbitWeight, Mathf.Max(_orbitWeightRange.x, _orbitWeightRange.y));
            _orbitWeight = Random.Range(minimumOrbitWeight, maximumOrbitWeight);
            if (_movementPattern == MovementPattern.Reposition)
            {
                _orbitWeight *= 0.6f;
            }

            float minimumSpeed = Mathf.Clamp01(
                Mathf.Min(_moveSpeedMultiplierRange.x, _moveSpeedMultiplierRange.y));
            float maximumSpeed = Mathf.Clamp(
                Mathf.Max(_moveSpeedMultiplierRange.x, _moveSpeedMultiplierRange.y),
                minimumSpeed, 1f);
            _moveSpeedMultiplier = Random.Range(minimumSpeed, maximumSpeed);

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
            if (_minimapIcon == null)
            {
                Transform[] children = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i].name == "EnemyMinimapIcon" ||
                        children[i].name == "MinimapIcon")
                    {
                        _minimapIcon = children[i];
                        break;
                    }
                }
            }
        }

        private void CacheSoundFeedback()
        {
            _soundFeedbackSound = _soundFeedback != null
                ? _soundFeedback.GetFeedbackOfType<MMF_MMSoundManagerSound>()
                : null;
        }

        private void PlaySoundFeedback(AudioClip clip)
        {
            if (clip == null || _soundFeedback == null) { return; }
            if (_soundFeedbackSound == null) { CacheSoundFeedback(); }
            if (_soundFeedbackSound == null) { return; }

            _soundFeedbackSound.Sfx = clip;
            _soundFeedback.PlayFeedbacks();
        }

        private void UpdateBossHealthUI()
        {
            UIGamePanel gamePanel = UIKit.GetPanel<UIGamePanel>();
            if (gamePanel != null && MaxHealth > 0)
            {
                gamePanel.BossHealthBar.fillAmount = (float)Health.Value / MaxHealth;
            }
        }

    }
}
