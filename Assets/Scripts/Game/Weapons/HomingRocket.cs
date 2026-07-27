using UnityEngine;

namespace SoulKnight3D
{
    [RequireComponent(typeof(Bullet))]
    public sealed class HomingRocket : MonoBehaviour
    {
        [Header("Homing")]
        [SerializeField, Min(0.01f)] private float _speed = 16f;
        [SerializeField, Min(0f)] private float _turnDegreesPerSecond = 300f;
        [SerializeField, Min(0f)] private float _homingDuration = 1.2f;
        [SerializeField, Min(0.05f)] private float _retargetInterval = 0.1f;
        [SerializeField, Min(0f)] private float _targetRange = 20f;
        [SerializeField] private float _targetHeightOffset = 0.5f;
        [SerializeField] private LayerMask _targetLayers = 1 << 8;

        private readonly Collider[] _targetBuffer = new Collider[64];
        private Bullet _bullet;
        private TargetableObject _target;
        private float _elapsed;
        private float _retargetTimer;

        private void Awake()
        {
            _bullet = GetComponent<Bullet>();
        }

        private void OnEnable()
        {
            _target = null;
            _elapsed = 0f;
            _retargetTimer = 0f;
        }

        private void FixedUpdate()
        {
            if (_bullet == null || _bullet._didHit ||
                _bullet.SelfRigidbody == null)
            {
                return;
            }

            Vector3 currentDirection = _bullet.SelfRigidbody.velocity;
            if (currentDirection.sqrMagnitude <= 0.0001f)
            {
                currentDirection = transform.forward;
            }
            currentDirection.Normalize();

            _elapsed += Time.fixedDeltaTime;
            if (_elapsed <= _homingDuration)
            {
                _retargetTimer -= Time.fixedDeltaTime;
                if (_retargetTimer <= 0f || !IsValidTarget(_target))
                {
                    _target = FindClosestTarget();
                    _retargetTimer = _retargetInterval;
                }

                if (IsValidTarget(_target))
                {
                    Vector3 targetPosition =
                        _target.transform.position +
                        Vector3.up * _targetHeightOffset;
                    Vector3 desiredDirection =
                        targetPosition - transform.position;
                    if (desiredDirection.sqrMagnitude > 0.0001f)
                    {
                        float maxRadians =
                            _turnDegreesPerSecond * Mathf.Deg2Rad *
                            Time.fixedDeltaTime;
                        currentDirection = Vector3.RotateTowards(
                            currentDirection, desiredDirection.normalized,
                            maxRadians, 0f).normalized;
                    }
                }
            }

            _bullet.SelfRigidbody.velocity = currentDirection * _speed;
            transform.rotation =
                Quaternion.LookRotation(currentDirection, Vector3.up);
        }

        public void Configure(float speed, float turnDegreesPerSecond,
            float homingDuration, float retargetInterval, float targetRange,
            float targetHeightOffset)
        {
            _speed = Mathf.Max(0.01f, speed);
            _turnDegreesPerSecond = Mathf.Max(0f, turnDegreesPerSecond);
            _homingDuration = Mathf.Max(0f, homingDuration);
            _retargetInterval = Mathf.Max(0.05f, retargetInterval);
            _targetRange = Mathf.Max(0f, targetRange);
            _targetHeightOffset = targetHeightOffset;
        }

        private TargetableObject FindClosestTarget()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position, _targetRange, _targetBuffer,
                _targetLayers, QueryTriggerInteraction.Ignore);
            TargetableObject closest = null;
            float closestDistance = _targetRange * _targetRange;

            for (int i = 0; i < hitCount; i++)
            {
                Collider targetCollider = _targetBuffer[i];
                _targetBuffer[i] = null;
                if (targetCollider == null) { continue; }

                TargetableObject target =
                    targetCollider.GetComponentInParent<TargetableObject>();
                if (!IsValidTarget(target)) { continue; }

                float distance =
                    (target.transform.position - transform.position).sqrMagnitude;
                if (distance >= closestDistance) { continue; }

                closest = target;
                closestDistance = distance;
            }

            return closest;
        }

        private static bool IsValidTarget(TargetableObject target)
        {
            bool isEnemyTarget = target is Enemy || target is BossEnemy;
            return isEnemyTarget && target.isActiveAndEnabled && !target.IsDead;
        }
    }
}
