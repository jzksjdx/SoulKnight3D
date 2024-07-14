using UnityEngine;
using QFramework;
using System;
using System.Collections;
using MoreMountains.Feedbacks;
using Unity.VisualScripting;

namespace SoulKnight3D
{
	public partial class Bullet : ViewController
	{
        private string _weaponTag;
		private int _damage;
        private bool _isCritHit = false;

        // timeouts
        private float _destroyTimeout = 3f;
        private float _destroyTimeoutDelta;

        public void InitializeBullet(string weaponTag, int damage, bool isCritHit, GameObject prefabRef)
		{
            _weaponTag = weaponTag;
            _isCritHit = isCritHit;
            _damage = isCritHit ? damage * 2 : damage;
            PrefabRef = prefabRef;

            _destroyTimeoutDelta = _destroyTimeout;
        }

        private void Awake()
        {
            SelfCapsuleCollider.OnTriggerEnterEvent((other) =>
			{
				if (other.tag == "Bullet") { return; }

                bool enemyHasHitFeedback = false;

                if (other.TryGetComponent(out TargetableObject targetableObject))
                {
                    if (other.tag == _weaponTag) { return; }
                    if (targetableObject.IsDead) { return; }
                    targetableObject.ApplyDamage(_damage);

                    if (_weaponTag == "Player" && other.tag == "Enemy")
                    {
                        // player attaking other objects
                        //if (_isCritHit)
                        //{
                        //    GameController.Instance.SpawnCritText(_damage, transform.position);
                        //    AudioKit.PlaySound("fx_hit");
                        //} else
                        //{
                        //    GameController.Instance.SpawnDamageText(_damage, transform.position);
                        //}

                        if (other.TryGetComponent(out Zombie zombie))
                        {
                            enemyHasHitFeedback = true;
                            zombie.Effects.PlayHitSound();
                        }
                    }
                }

                if (ImpactFeedback)
                {
                    MMF_MMSoundManagerSound soundFeedback = ImpactFeedback.GetFeedbackOfType<MMF_MMSoundManagerSound>();
                    if (enemyHasHitFeedback)
                    { // impact audio already played, disable default audio
                        soundFeedback.Active = false;
                    } else
                    {
                        soundFeedback.Active = true;
                    }
                    ImpactFeedback.GetFeedbackOfType<MMF_ParticlesInstantiation>().TargetWorldPosition = transform.position;
                    //ImpactFeedback.GetFeedbackOfType<MMF_ParticlesInstantiation>().ParentTransform = GameObjectsManager.Instance.transform;
                    ImpactFeedback.PlayFeedbacks();
                }
                DestroyBullet();
                //Destroy(gameObject);

            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void OnEnable()
        {
            if (TrailRenderer)
            {
                TrailRenderer.Clear();
            }
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

        private void DestroyBullet()
        {
            
            GameObjectsManager.Instance.DespawnBullet(this);

        }

        public void Reset()
        {
            _destroyTimeoutDelta = _destroyTimeout;
            _isCritHit = false;
            gameObject.Hide();
        }
	}
}
