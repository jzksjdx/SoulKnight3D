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

        public void InitializeBullet(string weaponTag, int damage, bool isCritHit, GameObject prefabRef, float bulletSize = 1f)
		{
            _weaponTag = weaponTag;
            _isCritHit = isCritHit;
            _damage = isCritHit ? damage * 2 : damage;
            PrefabRef = prefabRef;

            _destroyTimeoutDelta = _destroyTimeout;
            _didHit = false;
            if (TrailRenderer)
            {
                TrailRenderer.emitting = false;
                TrailRenderer.Clear();
            }

            transform.localScale = _originalScale * bulletSize;
        }

        protected virtual void Awake()
        {
            _originalScale = transform.localScale;
            SelfCapsuleCollider.OnCollisionEnterEvent((other) =>
            {
                if (_didHit) { return; }
                _didHit = true;
                OnBulletCollision(other);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        protected virtual void OnBulletCollision(Collision other)
        {
            HandleCollision(other);
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

        public void Reset()
        {
            _destroyTimeoutDelta = _destroyTimeout;
            _isCritHit = false;
            _didHit = false;
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

            TrailRenderer.Clear();
            TrailRenderer.emitting = true;
        }
    }
}
