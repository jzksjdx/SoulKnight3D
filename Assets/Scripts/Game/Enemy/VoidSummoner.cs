using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public sealed class VoidSummoner : Enemy
    {
        [Header("Void Summoner References")]
        [SerializeField] private Transform _attackOrigin;
        [SerializeField] private GameObject _handPrefab;
        [SerializeField] private GameObject _orbPrefab;
        [SerializeField] private GameObject _shieldVisual;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _preferredMinRange = 5f;
        [SerializeField, Min(0f)] private float _preferredMaxRange = 8f;
        [SerializeField, Min(0.1f)] private float _strafeDecisionMin = 1.5f;
        [SerializeField, Min(0.1f)] private float _strafeDecisionMax = 3.5f;
        [SerializeField, Min(0f)] private float _turnSpeed = 9f;
        [SerializeField, Min(0f)] private float _obstacleProbeDistance = 1f;
        [SerializeField, Min(0f)] private float _obstacleProbeRadius = 0.25f;
        [SerializeField] private LayerMask _obstacleLayers;
        [SerializeField, Range(0f, 0.25f)] private float _animationDeadZone = 0.04f;
        [SerializeField, Min(0f)] private float _animationDampTime = 0.12f;

        [Header("Attacks")]
        [SerializeField, Min(1)] private int _handCount = 3;
        [SerializeField, Min(0f)] private float _handSpawnRadius = 1.2f;
        [SerializeField, Min(0f)] private float _handSpawnHeight = 0.35f;
        [SerializeField, Min(1)] private int _orbCount = 3;
        [SerializeField, Min(0f)] private float _orbShotInterval = 0.15f;
        [SerializeField, Min(0f)] private float _orbLandingSpread = 1.5f;
        [SerializeField, Min(0f)] private float _attackCooldown = 3f;
        [SerializeField, Min(0f)] private float _initialAttackDelay = 0.6f;
        [SerializeField, Min(0.1f)] private float _attackAnimationTimeout = 8f;
        [SerializeField] private LayerMask _floorLayers;
        [SerializeField, Min(0.1f)] private float _floorProbeHeight = 8f;
        [SerializeField, Min(0.1f)] private float _floorProbeDistance = 20f;

        [Header("Void Shield")]
        [SerializeField, Min(0)] private int _shieldLayers = 3;
        [SerializeField, Min(1)] private int _shieldDamageCap = 1;
        [SerializeField, Min(0.1f)] private float _shieldWeaknessDuration = 2f;

        [Header("Sound Feedback")]
        [SerializeField] private MMF_Player _soundFeedback;
        [SerializeField] private AudioClip _summonSound;
        [SerializeField] private AudioClip _orbSound;
        [SerializeField] private AudioClip _deathSound;

        private readonly List<VoidSummonerHand> _activeHands =
            new List<VoidSummonerHand>();

        private MMF_MMSoundManagerSound _soundFeedbackSound;
        private float _attackTimer;
        private float _strafeTimer;
        private int _strafeSign = 1;
        private int _remainingShieldLayers;
        private float _shieldWeaknessTimer;
        private bool _isShieldWeak;
        private bool _isPerformingAttack;
        private bool _hasSummonedOnce;
        private bool _forceSummon;
        private Coroutine _orbVolleyCoroutine;

        private int _summonTriggerId;
        private int _orbTriggerId;
        private int _speedParameterId;
        private int _moveXParameterId;
        private int _moveYParameterId;

        protected override void Start()
        {
            base.Start();

            _summonTriggerId = Animator.StringToHash("Summon");
            _orbTriggerId = Animator.StringToHash("Orb");
            _speedParameterId = Animator.StringToHash("Speed");
            _moveXParameterId = Animator.StringToHash("MoveX");
            _moveYParameterId = Animator.StringToHash("MoveY");
            _attackTimer = _initialAttackDelay;
            _remainingShieldLayers = Mathf.Max(0, _shieldLayers);
            _strafeTimer = 0f;
            CacheSoundFeedback();
            UpdateShieldVisual();
            SetMovementAnimation(Vector3.zero, true);
        }

        protected override void Update()
        {
            if (IsDead)
            {
                StopHorizontalMovement();
                SetMovementAnimation(Vector3.zero, true);
                return;
            }

            UpdateShieldWeakness();
            CleanupHands();

            if (Player == null)
            {
                StopHorizontalMovement();
                SetMovementAnimation(Vector3.zero, true);
                return;
            }

            FacePlayer();
            if (_isPerformingAttack)
            {
                StopHorizontalMovement();
                SetMovementAnimation(Vector3.zero, true);
                return;
            }

            UpdateRangedMovement();
            UpdateAttackSelection();
        }

        public override void ApplyDamage(int damage)
        {
            if (IsDead || damage <= 0) { return; }

            bool consumeWeakness = _isShieldWeak &&
                                   _remainingShieldLayers > 0;
            int appliedDamage = IsShieldActive
                ? Mathf.Min(damage, _shieldDamageCap)
                : damage;

            PrepareDeathSoundIfNeeded(appliedDamage);
            base.ApplyDamage(appliedDamage);

            if (IsDead)
            {
                _isShieldWeak = false;
                _shieldVisual?.Hide();
                return;
            }

            if (consumeWeakness)
            {
                _remainingShieldLayers =
                    Mathf.Max(0, _remainingShieldLayers - 1);
                EndShieldWeakness();
            }
        }

        public void ReceiveReturnedHand(int damage)
        {
            if (IsDead || damage <= 0) { return; }

            if (IsShieldActive)
            {
                int cappedDamage = Mathf.Min(damage, _shieldDamageCap);
                PrepareDeathSoundIfNeeded(cappedDamage);
                base.ApplyDamage(cappedDamage);
                if (!IsDead)
                {
                    BeginShieldWeakness();
                }
                return;
            }

            ApplyDamage(damage);
        }

        public void RegisterHand(VoidSummonerHand hand)
        {
            if (hand == null || _activeHands.Contains(hand)) { return; }
            _activeHands.Add(hand);
        }

        public void UnregisterHand(VoidSummonerHand hand)
        {
            if (hand == null) { return; }
            _activeHands.Remove(hand);
            if (_hasSummonedOnce && CountActiveHands() == 0 && !IsDead)
            {
                _forceSummon = true;
            }
        }

        public void AnimationSummonHands()
        {
            if (IsDead || CountActiveHands() > 0) { return; }

            PlaySoundFeedback(_summonSound);
            _hasSummonedOnce = true;
            _forceSummon = false;

            for (int i = 0; i < _handCount; i++)
            {
                float angle = 360f * i / Mathf.Max(1, _handCount);
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) *
                                 Vector3.forward * _handSpawnRadius;
                Vector3 position = transform.position + offset +
                                   Vector3.up * _handSpawnHeight;
                PooledGameObject pooledHand =
                    GameObjectsManager.Instance?.SpawnPooledObject(
                        _handPrefab, position, Quaternion.identity);
                if (pooledHand == null) { continue; }

                if (pooledHand.TryGetComponent(
                    out VoidSummonerHand hand))
                {
                    hand.Initialize(this, position, i);
                }
                else
                {
                    pooledHand.ReleaseToPool();
                }
            }
        }

        // The assigned orb clip is shared with the priest and already emits
        // this event name.
        public void AnimationSplitBulletAttack()
        {
            CastOrbVolley();
        }

        public void AnimationCastOrbs()
        {
            CastOrbVolley();
        }

        public void ConfigureReferences(Rigidbody body,
            CapsuleCollider capsule, Animator animator, Transform attackOrigin,
            Transform minimapIcon, GameObject handPrefab, GameObject orbPrefab,
            GameObject shieldVisual)
        {
            SelfRigidbody = body;
            SelfCollider = capsule;
            SelfAnimator = animator;
            _attackOrigin = attackOrigin;
            MinimapIcon = minimapIcon;
            _handPrefab = handPrefab;
            _orbPrefab = orbPrefab;
            _shieldVisual = shieldVisual;
        }

        public void ConfigureSounds(MMF_Player soundFeedback,
            AudioClip summonSound, AudioClip orbSound, AudioClip deathSound)
        {
            _soundFeedback = soundFeedback;
            _summonSound = summonSound;
            _orbSound = orbSound;
            _deathSound = deathSound;
            CacheSoundFeedback();
        }

        private bool IsShieldActive =>
            _remainingShieldLayers > 0 && !_isShieldWeak;

        private void UpdateRangedMovement()
        {
            Vector3 toPlayer = Player.transform.position - transform.position;
            Vector3 horizontal = new Vector3(toPlayer.x, 0f, toPlayer.z);
            float distance = horizontal.magnitude;
            Vector3 toward = distance > 0.001f
                ? horizontal / distance
                : transform.forward;

            _strafeTimer -= Time.deltaTime;
            if (_strafeTimer <= 0f)
            {
                _strafeSign = Random.value < 0.5f ? -1 : 1;
                _strafeTimer = Random.Range(
                    Mathf.Min(_strafeDecisionMin, _strafeDecisionMax),
                    Mathf.Max(_strafeDecisionMin, _strafeDecisionMax));
            }

            Vector3 strafe = Vector3.Cross(Vector3.up, toward) * _strafeSign;
            Vector3 desired;
            if (distance < _preferredMinRange)
            {
                desired = -toward + strafe * 0.35f;
            }
            else if (distance > _preferredMaxRange)
            {
                desired = toward + strafe * 0.25f;
            }
            else
            {
                float midpoint =
                    (_preferredMinRange + _preferredMaxRange) * 0.5f;
                float radialCorrection = Mathf.Clamp(
                    (distance - midpoint) /
                    Mathf.Max(0.1f, _preferredMaxRange - _preferredMinRange),
                    -0.35f, 0.35f);
                desired = strafe + toward * radialCorrection;
            }

            desired = desired.normalized;
            if (desired.sqrMagnitude > 0.0001f &&
                Physics.SphereCast(transform.position + Vector3.up * 0.4f,
                    _obstacleProbeRadius, desired, out _,
                    _obstacleProbeDistance, _obstacleLayers,
                    QueryTriggerInteraction.Ignore))
            {
                _strafeSign *= -1;
                desired = (Vector3.Cross(Vector3.up, toward) *
                           _strafeSign - toward * 0.25f).normalized;
                _strafeTimer = _strafeDecisionMin;
            }

            Vector3 velocity = desired * Speed;
            velocity.y = SelfRigidbody != null
                ? SelfRigidbody.velocity.y
                : 0f;
            if (SelfRigidbody != null)
            {
                SelfRigidbody.velocity = velocity;
            }
            SetMovementAnimation(desired, false);
        }

        private void UpdateAttackSelection()
        {
            _attackTimer -= Time.deltaTime;
            int handCount = CountActiveHands();
            if (_forceSummon || (!_hasSummonedOnce &&
                _attackTimer <= 0f))
            {
                StartAttack(true);
                return;
            }

            if (handCount > 0 && _attackTimer <= 0f)
            {
                StartAttack(false);
            }
        }

        private void StartAttack(bool summon)
        {
            if (_isPerformingAttack || SelfAnimator == null) { return; }

            _isPerformingAttack = true;
            StopHorizontalMovement();
            SetMovementAnimation(Vector3.zero, true);
            int trigger = summon ? _summonTriggerId : _orbTriggerId;
            string stateName = summon ? "SummonAttack" : "OrbAttack";
            SelfAnimator.ResetTrigger(
                summon ? _orbTriggerId : _summonTriggerId);
            SelfAnimator.SetTrigger(trigger);
            StartCoroutine(WaitForAttackAnimation(stateName));
        }

        private IEnumerator WaitForAttackAnimation(string stateName)
        {
            bool enteredAttack = false;
            float timeout = _attackAnimationTimeout;
            yield return null;

            while (timeout > 0f && !IsDead)
            {
                timeout -= Time.deltaTime;
                AnimatorStateInfo current =
                    SelfAnimator.GetCurrentAnimatorStateInfo(0);
                bool currentIsAttack = current.IsName(stateName);
                bool nextIsAttack = SelfAnimator.IsInTransition(0) &&
                    SelfAnimator.GetNextAnimatorStateInfo(0).IsName(stateName);
                enteredAttack |= currentIsAttack || nextIsAttack;

                if (enteredAttack && !currentIsAttack && !nextIsAttack &&
                    !SelfAnimator.IsInTransition(0))
                {
                    break;
                }
                yield return null;
            }

            _isPerformingAttack = false;
            _attackTimer = _attackCooldown;
        }

        private void CastOrbVolley()
        {
            if (IsDead || _orbPrefab == null ||
                GameObjectsManager.Instance == null)
            {
                return;
            }

            if (_orbVolleyCoroutine != null)
            {
                StopCoroutine(_orbVolleyCoroutine);
            }
            _orbVolleyCoroutine = StartCoroutine(FireOrbVolley());
        }

        private IEnumerator FireOrbVolley()
        {
            PlaySoundFeedback(_orbSound);

            for (int i = 0; i < _orbCount; i++)
            {
                if (IsDead || GameObjectsManager.Instance == null)
                {
                    break;
                }

                Vector3 start = _attackOrigin != null
                    ? _attackOrigin.position
                    : transform.position + Vector3.up * 0.8f;
                Vector3 playerPosition = Player != null
                    ? Player.transform.position
                    : transform.position + transform.forward * 5f;
                Vector3 centerTarget = ResolveFloorPosition(playerPosition);
                Vector3 forward = centerTarget - transform.position;
                forward.y = 0f;
                forward = forward.sqrMagnitude > 0.0001f
                    ? forward.normalized
                    : transform.forward;
                Vector3 lateral = Vector3.Cross(Vector3.up, forward);
                float normalized = _orbCount <= 1
                    ? 0f
                    : i / (float)(_orbCount - 1) * 2f - 1f;
                Vector3 target = ResolveFloorPosition(
                    centerTarget + lateral *
                    normalized * _orbLandingSpread);
                PooledGameObject pooledOrb =
                    GameObjectsManager.Instance.SpawnPooledObject(
                        _orbPrefab, start, Quaternion.identity);
                if (pooledOrb == null) { continue; }

                if (pooledOrb.TryGetComponent(out VoidSummonerOrb orb))
                {
                    orb.Initialize(start, target);
                }
                else
                {
                    pooledOrb.ReleaseToPool();
                }

                if (i < _orbCount - 1 && _orbShotInterval > 0f)
                {
                    yield return new WaitForSeconds(_orbShotInterval);
                }
            }

            _orbVolleyCoroutine = null;
        }

        private Vector3 ResolveFloorPosition(Vector3 position)
        {
            Vector3 origin = new Vector3(
                position.x,
                Mathf.Max(position.y, transform.position.y) +
                _floorProbeHeight,
                position.z);
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                _floorProbeDistance, _floorLayers,
                QueryTriggerInteraction.Ignore))
            {
                position.y = hit.point.y + 0.02f;
            }
            else
            {
                position.y = transform.position.y + 0.02f;
            }
            return position;
        }

        private void FacePlayer()
        {
            Vector3 direction =
                Player.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f) { return; }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation,
                _turnSpeed * Time.deltaTime);
            _currRotation = transform.rotation;
        }

        private void SetMovementAnimation(Vector3 worldDirection,
            bool immediate)
        {
            if (SelfAnimator == null) { return; }

            Vector3 localDirection = transform.InverseTransformDirection(
                worldDirection);
            float speed = Mathf.Clamp01(worldDirection.magnitude);
            float moveX = Mathf.Clamp(localDirection.x, -1f, 1f);
            float moveY = Mathf.Clamp(localDirection.z, -1f, 1f);
            if (speed <= _animationDeadZone)
            {
                speed = 0f;
                moveX = 0f;
                moveY = 0f;
                immediate = true;
            }

            if (immediate)
            {
                SelfAnimator.SetFloat(_speedParameterId, speed);
                SelfAnimator.SetFloat(_moveXParameterId, moveX);
                SelfAnimator.SetFloat(_moveYParameterId, moveY);
                return;
            }

            SelfAnimator.SetFloat(
                _speedParameterId, speed, _animationDampTime, Time.deltaTime);
            SelfAnimator.SetFloat(
                _moveXParameterId, moveX, _animationDampTime, Time.deltaTime);
            SelfAnimator.SetFloat(
                _moveYParameterId, moveY, _animationDampTime, Time.deltaTime);
        }

        private void StopHorizontalMovement()
        {
            if (SelfRigidbody == null) { return; }
            Vector3 velocity = SelfRigidbody.velocity;
            SelfRigidbody.velocity = new Vector3(0f, velocity.y, 0f);
        }

        private int CountActiveHands()
        {
            CleanupHands();
            return _activeHands.Count;
        }

        private void CleanupHands()
        {
            _activeHands.RemoveAll(hand =>
                hand == null || !hand.IsCombatActive);
        }

        private void BeginShieldWeakness()
        {
            if (_remainingShieldLayers <= 0) { return; }
            _isShieldWeak = true;
            _shieldWeaknessTimer = _shieldWeaknessDuration;
            UpdateShieldVisual();
        }

        private void UpdateShieldWeakness()
        {
            if (!_isShieldWeak) { return; }
            _shieldWeaknessTimer -= Time.deltaTime;
            if (_shieldWeaknessTimer <= 0f)
            {
                EndShieldWeakness();
            }
        }

        private void EndShieldWeakness()
        {
            _isShieldWeak = false;
            _shieldWeaknessTimer = 0f;
            UpdateShieldVisual();
        }

        private void UpdateShieldVisual()
        {
            if (_shieldVisual != null)
            {
                _shieldVisual.SetActive(IsShieldActive);
            }
        }

        private void PrepareDeathSoundIfNeeded(int damage)
        {
            if (Health.Value - damage > 0 || _deathSound == null ||
                _soundFeedback == null)
            {
                return;
            }

            if (_soundFeedbackSound == null)
            {
                CacheSoundFeedback();
            }
            if (_soundFeedbackSound != null)
            {
                _soundFeedbackSound.Sfx = _deathSound;
            }
        }

        private void CacheSoundFeedback()
        {
            _soundFeedbackSound = _soundFeedback != null
                ? _soundFeedback
                    .GetFeedbackOfType<MMF_MMSoundManagerSound>()
                : null;
        }

        private void PlaySoundFeedback(AudioClip clip)
        {
            if (clip == null || _soundFeedback == null) { return; }
            if (_soundFeedbackSound == null)
            {
                CacheSoundFeedback();
            }
            if (_soundFeedbackSound == null) { return; }

            _soundFeedbackSound.Sfx = clip;
            _soundFeedback.PlayFeedbacks();
        }
    }
}
