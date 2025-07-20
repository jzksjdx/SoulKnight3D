using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Feedbacks;
using QFramework;

namespace SoulKnight3D
{
    public class RammingEnemy : Enemy
    {
        [SerializeField] private MMF_Player RammingFeedbacks;
        [SerializeField] private GameObject RammingParticles;
        [SerializeField] private float RamSpeedFactor = 5;

        private Vector3 _rammingDirection;

        private float _ramTimeoutDelta;
        private float _ramTimeout = 1.5f;
        private float _hitForce = 7;

        protected override void Start()
        {
            base.Start();

            State = EnemyState.Attacking;

            _attackTimeout = 1.5f;
            _ramTimeoutDelta = _ramTimeout;
            _animIdAttack = Animator.StringToHash("Move");

            SelfCollider.OnCollisionEnterEvent((other) =>
            {
                if (other.gameObject.CompareTag("Player") && State == EnemyState.Chasing)
                {
                    PlayerController.Instance.PlayerStats.ApplyDamage(Attack);
                    Vector3 hitForce = new Vector3(_moveDirection.x * _hitForce, _hitForce / 5, _moveDirection.z * _hitForce);
                    PlayerController.Instance.SelfRigidbody.AddForce(hitForce, ForceMode.Impulse);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            OnDeath.Register(() =>
            {
                RammingParticles.Hide();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        override protected void LookAtPlayer()
        {
            _moveDirection = PlayerController.Instance.transform.position - transform.position;
            Vector3 lookDirection = new Vector3(_moveDirection.x, 0, _moveDirection.z);
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
            if (State != EnemyState.Chasing)
            {
                transform.rotation = lookRotation;
                _currRotation = lookRotation;
            } else
            {
                
                transform.rotation = Quaternion.LookRotation(_rammingDirection);
                _currRotation = Quaternion.LookRotation(_rammingDirection);
            }
        }

        protected override void HandleChasing()
        {
            if (_ramTimeoutDelta >= 0f)
            {
                _ramTimeoutDelta -= Time.deltaTime;
                Vector3 moveSpeed = new Vector3(
                    _rammingDirection.normalized.x * Speed * RamSpeedFactor,
                    SelfRigidbody.velocity.y,
                    _rammingDirection.normalized.z * Speed * RamSpeedFactor);
                SelfRigidbody.velocity = moveSpeed;
            } else
            {
                State = EnemyState.Attacking;
                RammingParticles.Hide();
            }
        }

        override protected void HandleAttacking()
        {
            _attackTimeoutDelta = 0f;
            _patrolDirection = new Vector3(
                   transform.position.x + Random.Range(-5, 5),
                   transform.position.y,
                   transform.position.z + Random.Range(-5, 5)).normalized;

            SelfAnimator.SetTrigger(_animIdMove);
            _patrolTimeoutDelta = _patrolTimeout;
            State = EnemyState.Patroling;
        }

        protected override void HandlePatroling()
        {
            if (_patrolTimeoutDelta > 0)
            {
                // patrol
                _patrolTimeoutDelta -= Time.deltaTime;

                Vector3 patrolVelocity = new Vector3(_patrolDirection.x * Speed, SelfRigidbody.velocity.y, _patrolDirection.z * Speed);
                SelfRigidbody.velocity = patrolVelocity;
            }
            else
            {
                _rammingDirection = PlayerController.Instance.transform.position - transform.position;
                _ramTimeoutDelta = _ramTimeout;
                RammingFeedbacks?.PlayFeedbacks();
                RammingParticles.Show();
                State = EnemyState.Chasing;
            }
        }
    }

}
