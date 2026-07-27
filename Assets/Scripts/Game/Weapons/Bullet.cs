using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using MoreMountains.Feedbacks;

namespace SoulKnight3D
{
	public partial class Bullet : ViewController, IPoolable
	{
		protected string _weaponTag;
		protected int _damage;
        protected bool _isCritHit = false;

        // timeouts
        protected float _destroyTimeout = 3f;
        protected float _destroyTimeoutDelta;

        public bool _didHit = false;

        private Vector3 _originalScale = Vector3.one;
        private IBulletImpactBehavior _impactBehavior;
        private List<Collider> _ignoredColliders;
        private Vector3 _preCollisionVelocity;
        private Quaternion _preCollisionRotation;
        private bool _hasPreCollisionPose;

        public string WeaponTag => _weaponTag;
        public Vector3 PreCollisionVelocity => _hasPreCollisionPose
            ? _preCollisionVelocity
            : (SelfRigidbody != null ? SelfRigidbody.velocity : Vector3.zero);
        public Quaternion PreCollisionRotation => _hasPreCollisionPose
            ? _preCollisionRotation
            : transform.rotation;

        public void InitializeBullet(string weaponTag, int damage, bool isCritHit, GameObject prefabRef, float bulletSize = 1f)
		{
            _weaponTag = weaponTag;
            _isCritHit = isCritHit;
            _damage = isCritHit ? damage * 2 : damage;
            PrefabRef = prefabRef;

            _destroyTimeoutDelta = _destroyTimeout;
            _didHit = false;
            _preCollisionVelocity = Vector3.zero;
            _hasPreCollisionPose = false;
            RestoreIgnoredCollisions();
            if (TrailRenderer)
            {
                TrailRenderer.emitting = false;
                TrailRenderer.Clear();
            }

            transform.localScale = _originalScale * bulletSize;
        }

        public void SetRemainingLifetime(float lifetime)
        {
            _destroyTimeoutDelta = Mathf.Max(0.05f, lifetime);
        }

        protected virtual void Awake()
        {
            _originalScale = transform.localScale;
            _impactBehavior = GetComponent<IBulletImpactBehavior>();
            ConfigureTrailForPooling();
            SelfCapsuleCollider.OnCollisionEnterEvent((other) =>
            {
                if (_didHit || PassThroughFriendlyCollision(other)) { return; }
                _didHit = true;
                OnBulletCollision(other);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void ConfigureTrailForPooling()
        {
            if (!TrailRenderer) { return; }

            // Auto Destruct removes the trail object after its first use, which
            // leaves later pool checkouts without a renderer.
            TrailRenderer.autodestruct = false;
            TrailRenderer.emitting = false;
            TrailRenderer.Clear();
        }

        private bool PassThroughFriendlyCollision(Collision other)
        {
            if (string.IsNullOrEmpty(_weaponTag) || other.collider == null)
            {
                return false;
            }

            TargetableObject target =
                other.collider.GetComponentInParent<TargetableObject>();
            bool isFriendly = other.collider.CompareTag(_weaponTag) ||
                (target != null && target.CompareTag(_weaponTag));
            if (!isFriendly) { return false; }

            IgnoreCollisionUntilSeparated(other.collider, 0.02f);
            if (SelfRigidbody != null && _hasPreCollisionPose)
            {
                SelfRigidbody.velocity = _preCollisionVelocity;
                transform.rotation = _preCollisionRotation;
            }
            return true;
        }

        private void FixedUpdate()
        {
            if (_didHit || SelfRigidbody == null ||
                SelfRigidbody.velocity.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            _preCollisionVelocity = SelfRigidbody.velocity;
            _preCollisionRotation = transform.rotation;
            _hasPreCollisionPose = true;
        }

        protected virtual void OnBulletCollision(Collision other)
        {
            HandleCollision(other);
            _impactBehavior?.OnBulletImpact(this, other);
            PlayImpactFeedback();
            DestroyBullet();
        }

        protected void HandleCollision(Collision other)
        {
            if (other.collider.TryGetComponent(out TargetableObject targetableObject))
            {
                if (other.collider.CompareTag(_weaponTag)) { return; }

                if (targetableObject.IsDead) { return; }

                targetableObject.ApplyDamage(_damage);

                if (_weaponTag == "Player" && other.collider.CompareTag("Enemy"))
                {
                    // player attaking other objects
                    if (_isCritHit)
                    {
                        GameController.Instance.SpawnCritText(_damage, transform.position);
                        AudioKit.PlaySound("fx_hit");
                    }
                    else
                    {
                        GameController.Instance.SpawnDamageText(_damage, transform.position);
                    }
                }
            }
        }

        protected void PlayImpactFeedback()
        {
            if (!ImpactFeedback) { return; }

            MMF_ParticlesInstantiation particles = ImpactFeedback.GetFeedbackOfType<MMF_ParticlesInstantiation>();
            if (particles != null)
            {
                particles.TargetWorldPosition = transform.position;
            }
            ImpactFeedback.PlayFeedbacks();
        }

        private void Update()
        {
            if (_destroyTimeoutDelta >= 0)
            {
                _destroyTimeoutDelta -= Time.deltaTime;
                if (_destroyTimeoutDelta <= 0)
                {
                    DestroyBullet();
                }
            }
        }

        public void DestroyBullet()
        {
            if (!gameObject.activeSelf) { return; }
            if (GameObjectsManager.Instance)
            {
                GameObjectsManager.Instance.DespawnBullet(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void IgnoreCollisionUntilSeparated(Collider other, float separationPadding)
        {
            if (SelfCapsuleCollider == null || other == null)
            {
                return;
            }

            if (_ignoredColliders == null)
            {
                _ignoredColliders = new List<Collider>();
            }

            if (!_ignoredColliders.Contains(other))
            {
                _ignoredColliders.Add(other);
                Physics.IgnoreCollision(SelfCapsuleCollider, other, true);
                StartCoroutine(RestoreCollisionWhenSeparated(other,
                    Mathf.Max(0f, separationPadding)));
            }
        }

        private IEnumerator RestoreCollisionWhenSeparated(Collider other, float separationPadding)
        {
            WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
            yield return waitForFixedUpdate;

            while (other != null && other.enabled && other.gameObject.activeInHierarchy &&
                !HasSeparatedFrom(other, separationPadding))
            {
                yield return waitForFixedUpdate;
            }

            if (SelfCapsuleCollider != null && other != null)
            {
                Physics.IgnoreCollision(SelfCapsuleCollider, other, false);
            }
            _ignoredColliders?.Remove(other);
        }

        private bool HasSeparatedFrom(Collider other, float separationPadding)
        {
            Vector3 bulletCenter = SelfCapsuleCollider.bounds.center;
            Vector3 closestPoint = other.ClosestPoint(bulletCenter);
            Vector3 towardCollider = closestPoint - bulletCenter;
            float bulletExtent = SelfCapsuleCollider.bounds.extents.magnitude;
            float requiredSeparation = bulletExtent + separationPadding;
            if (towardCollider.sqrMagnitude < requiredSeparation * requiredSeparation)
            {
                return false;
            }

            Vector3 velocity = SelfRigidbody != null
                ? SelfRigidbody.velocity
                : Vector3.zero;
            return velocity.sqrMagnitude <= 0.0001f ||
                Vector3.Dot(velocity, towardCollider) <= 0f;
        }

        private void RestoreIgnoredCollisions()
        {
            if (_ignoredColliders == null)
            {
                return;
            }

            for (int i = 0; i < _ignoredColliders.Count; i++)
            {
                Collider other = _ignoredColliders[i];
                if (SelfCapsuleCollider != null && other != null)
                {
                    Physics.IgnoreCollision(SelfCapsuleCollider, other, false);
                }
            }
            _ignoredColliders.Clear();
        }

        public void Reset()
        {
            RestoreIgnoredCollisions();
            _destroyTimeoutDelta = _destroyTimeout;
            _isCritHit = false;
            _didHit = false;
            _preCollisionVelocity = Vector3.zero;
            _hasPreCollisionPose = false;
            if (SelfRigidbody != null)
            {
                SelfRigidbody.velocity = Vector3.zero;
                SelfRigidbody.angularVelocity = Vector3.zero;
            }
            transform.localScale = _originalScale;
            if (TrailRenderer)
            {
                TrailRenderer.emitting = false;
                TrailRenderer.Clear();
            }
            gameObject.Hide();
        }

        public void ShowFromPool()
        {
            gameObject.Show();
            if (!TrailRenderer) { return; }

            TrailRenderer.autodestruct = false;
            TrailRenderer.enabled = true;
            TrailRenderer.Clear();
            TrailRenderer.emitting = true;
        }
    }
}
