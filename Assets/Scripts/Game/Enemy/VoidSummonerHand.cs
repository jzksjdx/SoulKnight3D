using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public sealed class VoidSummonerHand : TargetableObject
    {
        private enum HandState
        {
            Chase,
            Patrol,
            Lunge,
            Gripped,
            Thrown,
            Dead
        }

        [Header("References")]
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private CapsuleCollider _collider;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _minimapIcon;
        [SerializeField] private GameObject _gripStatusPrefab;
        [SerializeField] private MMF_Player _deadFeedback;

        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float _patrolDuration = 1.1f;
        [SerializeField, Min(0f)] private float _patrolDurationRandomness = 0.6f;
        [SerializeField, Range(0f, 1f)] private float _initialPatrolChance = 0.65f;
        [SerializeField, Min(0f)] private float _hoverHeight = 0.65f;
        [SerializeField, Min(0f)] private float _turnSpeed = 12f;

        [Header("Lunge (exported defaults)")]
        [SerializeField, Min(0.1f)] private float _dashTriggerDistance = 5f;
        [SerializeField, Min(0.1f)] private float _dashSpeed = 40f;
        [SerializeField, Min(0.01f)] private float _dashArrivalDistance = 0.1f;
        [SerializeField, Min(0f)] private float _dashCooldown = 4f;
        [SerializeField, Min(0.05f)] private float _grabDistance = 0.65f;
        [SerializeField, Min(0.1f)] private float _maxLungeDuration = 0.5f;
        [SerializeField, Min(0)] private int _occupiedGripDamage = 2;

        [Header("Grip")]
        [SerializeField] private Vector3 _gripOffset = Vector3.zero;

        [Header("Return throw")]
        [SerializeField, Min(0.1f)] private float _throwSpeed = 20f;
        [SerializeField, Min(0.05f)] private float _throwDuration = 0.42f;
        [SerializeField, Min(0)] private int _throwDamage = 6;
        [SerializeField, Min(0f)] private float _throwArcHeight = 0.75f;
        [SerializeField, Min(0.05f)] private float _throwHitRadius = 0.3f;
        [SerializeField] private LayerMask _throwHitLayers;

        private readonly Collider[] _throwHits = new Collider[24];
        private readonly HashSet<TargetableObject> _damagedTargets =
            new HashSet<TargetableObject>();

        private PooledGameObject _pooledObject;
        private VoidSummoner _owner;
        private PlayerController _player;
        private VoidSummonerGripStatus _gripStatus;
        private HandState _state;
        private Vector3 _patrolDirection;
        private Vector3 _lungeTarget;
        private Vector3 _throwStart;
        private Vector3 _throwTarget;
        private float _stateTimer;
        private float _dashCooldownTimer;
        private float _throwElapsed;
        private int _gripAnimatorId;
        private bool _registeredWithOwner;
        private bool _isCombatActive;

        public bool IsCombatActive => _isCombatActive &&
                                      _state != HandState.Dead;

        private void Awake()
        {
            CacheComponents();
            _gripAnimatorId = Animator.StringToHash("Gripped");
        }

        protected override void Start()
        {
            base.Start();
            CacheComponents();
        }

        private void Update()
        {
            if (!_isCombatActive || _state == HandState.Dead) { return; }

            _player = PlayerController.Instance;
            _dashCooldownTimer = Mathf.Max(
                0f, _dashCooldownTimer - Time.deltaTime);

            switch (_state)
            {
                case HandState.Chase:
                    UpdateChase();
                    break;
                case HandState.Patrol:
                    UpdatePatrol();
                    break;
                case HandState.Lunge:
                    UpdateLunge();
                    break;
                case HandState.Thrown:
                    UpdateThrown();
                    break;
            }
        }

        private void LateUpdate()
        {
            if (_state != HandState.Gripped || _player == null) { return; }

            Transform anchor = GetGripAnchor();
            if (transform.parent != anchor)
            {
                transform.SetParent(anchor, false);
            }
            transform.localPosition = _gripOffset;
            transform.localRotation = Quaternion.identity;
        }

        private void OnDisable()
        {
            if (_isCombatActive)
            {
                ReleaseGripStatus();
                UnregisterFromOwner();
            }
            _isCombatActive = false;
        }

        public void Initialize(VoidSummoner owner, Vector3 position,
            int spawnIndex)
        {
            CacheComponents();
            ReleaseGripStatus();
            UnregisterFromOwner();

            _owner = owner;
            _player = PlayerController.Instance;
            Health.Value = MaxHealth;
            _state = HandState.Chase;
            _stateTimer = 0f;
            _dashCooldownTimer = Random.Range(0.15f, 0.85f) +
                                 spawnIndex * 0.15f;
            _throwElapsed = 0f;
            _isCombatActive = true;
            _damagedTargets.Clear();

            transform.SetParent(
                GameObjectsManager.Instance != null
                    ? GameObjectsManager.Instance.transform
                    : null, true);
            transform.position = position;

            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.useGravity = false;
                _rigidbody.isKinematic = false;
            }
            if (_collider != null)
            {
                _collider.enabled = true;
                _collider.isTrigger = false;
            }
            if (_animator != null)
            {
                _animator.SetBool(_gripAnimatorId, false);
            }
            _minimapIcon?.gameObject.Show();

            _owner?.RegisterHand(this);
            _registeredWithOwner = _owner != null;
            if (Random.value < _initialPatrolChance)
            {
                EnterPatrol();
            }
            _pooledObject.ShowFromPool();
        }

        public override void ApplyDamage(int damage)
        {
            if (!_isCombatActive || IsDead || damage <= 0) { return; }

            base.ApplyDamage(damage);
            if (!IsDead) { return; }

            _state = HandState.Dead;
            _isCombatActive = false;
            StopMovement();
            if (_collider != null)
            {
                _collider.enabled = false;
            }
            _minimapIcon?.gameObject.Hide();
            ReleaseGripStatus();
            UnregisterFromOwner();
            _deadFeedback?.PlayFeedbacks();
            StartCoroutine(ReleaseAfterDelay(0.25f));
        }

        public void ThrowBackAtSummoner()
        {
            if (_state != HandState.Gripped) { return; }

            _gripStatus = null;
            transform.SetParent(
                GameObjectsManager.Instance != null
                    ? GameObjectsManager.Instance.transform
                    : null, true);

            _throwStart = transform.position;
            Vector3 fallbackDirection = _player != null
                ? _player.transform.forward
                : transform.forward;
            _throwTarget = _owner != null && !_owner.IsDead
                ? _owner.transform.position + Vector3.up * _hoverHeight
                : _throwStart + fallbackDirection * _throwSpeed *
                  _throwDuration;
            _throwElapsed = 0f;
            _damagedTargets.Clear();
            _state = HandState.Thrown;

            if (_animator != null)
            {
                _animator.SetBool(_gripAnimatorId, false);
            }
            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }
            if (_collider != null)
            {
                _collider.enabled = false;
            }
            _minimapIcon?.gameObject.Show();
        }

        public void ConfigureReferences(Rigidbody body,
            CapsuleCollider capsule, Animator animator, Transform minimapIcon,
            GameObject gripStatusPrefab, MMF_Player deadFeedback)
        {
            _rigidbody = body;
            _collider = capsule;
            _animator = animator;
            _minimapIcon = minimapIcon;
            _gripStatusPrefab = gripStatusPrefab;
            _deadFeedback = deadFeedback;
        }

        private void UpdateChase()
        {
            if (_player == null)
            {
                StopMovement();
                return;
            }

            Vector3 toPlayer = GetPlayerAimPoint() - transform.position;
            float distance = toPlayer.magnitude;
            RotateToward(toPlayer);

            if (_dashCooldownTimer <= 0f &&
                distance <= _dashTriggerDistance)
            {
                BeginLunge();
                return;
            }

            Vector3 horizontal = new Vector3(
                toPlayer.x, 0f, toPlayer.z).normalized;
            Vector3 hoverCorrection = Vector3.up *
                Mathf.Clamp(toPlayer.y, -1f, 1f);
            SetVelocity((horizontal + hoverCorrection * 0.35f).normalized *
                        Speed);
        }

        private void UpdatePatrol()
        {
            if (_player != null)
            {
                RotateToward(GetPlayerAimPoint() - transform.position);
            }

            _stateTimer -= Time.deltaTime;
            SetVelocity(_patrolDirection * Speed);
            if (_stateTimer <= 0f)
            {
                _state = HandState.Chase;
            }
        }

        private void BeginLunge()
        {
            _state = HandState.Lunge;
            _stateTimer = _maxLungeDuration;
            _lungeTarget = GetPlayerAimPoint();
            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }
        }

        private void UpdateLunge()
        {
            if (_player == null)
            {
                FinishFailedLunge();
                return;
            }

            _stateTimer -= Time.deltaTime;
            Vector3 position = Vector3.MoveTowards(
                transform.position, _lungeTarget,
                _dashSpeed * Time.deltaTime);
            transform.position = position;
            RotateToward(_lungeTarget - position);

            float playerDistance =
                Vector3.Distance(position, GetPlayerAimPoint());
            if (playerDistance <= _grabDistance)
            {
                TryGripPlayer();
                return;
            }

            if (_stateTimer <= 0f ||
                Vector3.Distance(position, _lungeTarget) <=
                _dashArrivalDistance)
            {
                FinishFailedLunge();
            }
        }

        private void TryGripPlayer()
        {
            PlayerStats playerStats =
                _player != null ? _player.PlayerStats : null;
            if (playerStats == null)
            {
                FinishFailedLunge();
                return;
            }

            if (playerStats.Statuses.Contains(Status.StatusType.Restrained))
            {
                playerStats.ApplyDamage(_occupiedGripDamage);
                FinishFailedLunge();
                return;
            }

            GameObject statusObject = GameObjectsManager.Instance != null
                ? GameObjectsManager.Instance.SpawnStatus(
                    _gripStatusPrefab, playerStats)
                : null;
            _gripStatus = statusObject != null
                ? statusObject.GetComponent<VoidSummonerGripStatus>()
                : null;
            if (_gripStatus == null)
            {
                playerStats.ApplyDamage(_occupiedGripDamage);
                FinishFailedLunge();
                return;
            }

            _gripStatus.BindHand(this);
            _state = HandState.Gripped;
            StopMovement();
            transform.SetParent(GetGripAnchor(), false);
            transform.localPosition = _gripOffset;
            transform.localRotation = Quaternion.identity;
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
            }
            if (_collider != null)
            {
                _collider.enabled = false;
            }
            if (_animator != null)
            {
                _animator.SetBool(_gripAnimatorId, true);
            }
            _minimapIcon?.gameObject.Hide();
        }

        private void FinishFailedLunge()
        {
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
            }
            _dashCooldownTimer = _dashCooldown;
            EnterPatrol();
        }

        private void UpdateThrown()
        {
            _throwElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(
                _throwElapsed / Mathf.Max(0.05f, _throwDuration));
            Vector3 currentTarget =
                _owner != null && !_owner.IsDead
                    ? _owner.transform.position + Vector3.up * _hoverHeight
                    : _throwTarget;
            Vector3 linear = Vector3.Lerp(_throwStart, currentTarget, t);
            linear.y += 4f * _throwArcHeight * t * (1f - t);
            transform.position = linear;

            Vector3 direction = currentTarget - transform.position;
            RotateToward(direction);
            DamageTargetsAlongThrow();

            if (t >= 1f)
            {
                FinishThrow();
            }
        }

        private void DamageTargetsAlongThrow()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position, _throwHitRadius, _throwHits,
                _throwHitLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hitCount; i++)
            {
                TargetableObject target =
                    _throwHits[i].GetComponentInParent<TargetableObject>();
                if (target == null || target == this ||
                    !_damagedTargets.Add(target))
                {
                    continue;
                }

                if (target == _owner)
                {
                    _owner.ReceiveReturnedHand(_throwDamage);
                }
                else
                {
                    target.ApplyDamage(_throwDamage);
                }
            }
        }

        private void FinishThrow()
        {
            _dashCooldownTimer = _dashCooldown;
            EnterPatrol();
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.velocity = Vector3.zero;
            }
            if (_collider != null)
            {
                _collider.enabled = true;
            }
        }

        private Vector3 GetPlayerAimPoint()
        {
            return _player != null
                ? _player.transform.position + Vector3.up * _hoverHeight
                : transform.position;
        }

        private Transform GetGripAnchor()
        {
            if (_player == null) { return transform.parent; }
            return _player.ModelRoot != null
                ? _player.ModelRoot
                : _player.transform;
        }

        private void EnterPatrol(float minimumDuration = 0f)
        {
            _state = HandState.Patrol;
            float duration = Random.Range(
                Mathf.Max(0.1f,
                    _patrolDuration - _patrolDurationRandomness),
                Mathf.Max(0.1f,
                    _patrolDuration + _patrolDurationRandomness));
            _stateTimer = Mathf.Max(minimumDuration, duration);

            Vector2 horizontal = Random.insideUnitCircle.normalized;
            _patrolDirection = new Vector3(
                horizontal.x,
                Random.Range(-0.15f, 0.15f),
                horizontal.y).normalized;
            if (_patrolDirection.sqrMagnitude <= 0.0001f)
            {
                _patrolDirection = transform.right;
            }
        }

        private void RotateToward(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f) { return; }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation,
                _turnSpeed * Time.deltaTime);
        }

        private void SetVelocity(Vector3 velocity)
        {
            if (_rigidbody == null || _rigidbody.isKinematic) { return; }
            _rigidbody.velocity = velocity;
        }

        private void StopMovement()
        {
            if (_rigidbody == null) { return; }
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        private void ReleaseGripStatus()
        {
            if (_gripStatus != null)
            {
                VoidSummonerGripStatus status = _gripStatus;
                _gripStatus = null;
                status.RemoveStatus();
            }
        }

        private void UnregisterFromOwner()
        {
            if (!_registeredWithOwner) { return; }
            _registeredWithOwner = false;
            _owner?.UnregisterHand(this);
        }

        private IEnumerator ReleaseAfterDelay(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (_pooledObject != null)
            {
                _pooledObject.ReleaseToPool();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void CacheComponents()
        {
            if (_pooledObject == null)
            {
                _pooledObject = GetComponent<PooledGameObject>();
            }
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }
            if (_collider == null)
            {
                _collider = GetComponent<CapsuleCollider>();
            }
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
        }
    }
}
