using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using MoreMountains.Feedbacks;

namespace SoulKnight3D
{
    public class Zombie : TargetableObject
    {
        public Rigidbody SelfRigidbody;

        public Animator SelfAnimator;

        public Collider SelfCollider;

        public int Attack = 3;
        public float Range = 1f;

        public ZombieEffects Effects;

        public EasyEvent OnDeath = new EasyEvent();

        public enum EnemyState
        {
            Chasing, Attacking
        }

        public EnemyState State = EnemyState.Chasing;

        // timeout deltatime
        private float _attackTimeoutDelta;
        private float _attackTimeout = 3f;

        // animation IDs
        private int _animIdMove;
        private int _animIdAttack;
        private int _animIdDie;


        protected override void Start()
        {
            base.Start();
            // set animations
            _animIdMove = Animator.StringToHash("Move");
            _animIdAttack = Animator.StringToHash("Attack");
            _animIdDie = Animator.StringToHash("Die");
            SelfAnimator.SetTrigger(_animIdMove);

            // set time out delta
        }

        Quaternion _currRotation;

        private void Update()
        {
            if (IsDead)
            {
                transform.rotation = _currRotation;
                return;
            }
            // look at player
            Vector3 moveDirection = PlayerController.Instance.transform.position - transform.position;
            Vector3 lookDirection = new Vector3(moveDirection.x, 0, moveDirection.z);
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = lookRotation;
            _currRotation = lookRotation;

            switch (State)
            {
                case EnemyState.Chasing:
                    if (moveDirection.magnitude <= Range)
                    {
                        // attack
                        SelfAnimator.SetTrigger(_animIdAttack);
                        State = EnemyState.Attacking;
                        _attackTimeoutDelta = _attackTimeout;
                    }
                    else
                    {
                        // move
                        Vector3 moveSpeed = new Vector3(moveDirection.normalized.x * Speed, SelfRigidbody.velocity.y, moveDirection.normalized.z * Speed);
                        SelfRigidbody.velocity = moveSpeed;
                    }
                    break;

                case EnemyState.Attacking:
                    if (_attackTimeoutDelta > 0)
                    {
                        _attackTimeoutDelta -= Time.deltaTime;
                    }
                    else
                    {
                        SelfAnimator.SetTrigger(_animIdMove);
                        State = EnemyState.Chasing;
                    }
                    break;
            }

        }

        public override void ApplyDamage(int damage)
        {
            if (IsDead) { return; }
            base.ApplyDamage(damage);

            if (Health.Value <= MaxHealth / 2)
            {
                Effects.PopLimb();
            }

            if (IsDead)
            {
                Effects.PopHead();

                SelfAnimator.SetTrigger(_animIdDie);
                SelfCollider.enabled = false;
                SelfRigidbody.isKinematic = true;
                OnDeath.Trigger();

                Destroy(gameObject, 3);
            }
        }

        public void MeleeAttackAnimationEffect()
        {
            Effects.PlayAttackSound();
            float distance = (PlayerController.Instance.transform.position - transform.position).magnitude;
            if (distance <= Range)
            {
                PlayerController.Instance.PlayerStats.ApplyDamage(Attack);
            }
        }
    }

}
