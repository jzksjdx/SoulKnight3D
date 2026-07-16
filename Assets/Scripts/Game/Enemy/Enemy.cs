using UnityEngine;
using QFramework;
using System.Collections.Generic;
using MoreMountains.Feedbacks;

namespace SoulKnight3D
{
	public partial class Enemy : TargetableObject
	{
        public int Attack = 3;
        public float Range = 1f;

        public EasyEvent OnDeath = new EasyEvent();

        public enum EnemyState
        {
            Chasing, Attacking, Patroling
        }

        public EnemyState State = EnemyState.Chasing;
        public Transform MinimapIcon;

        // timeout deltatime
        protected float _patrolTimeoutDelta;
        [SerializeField] protected float _patrolTimeout = 2f;
        protected float _attackTimeoutDelta;
         [SerializeField] protected float _attackTimeout = 3f;
        [SerializeField, Min(0f)] private float _deathCleanupDelay = 3f;
        [SerializeField] private MMF_Player _deadFeedbacks;
        private float _dissolveTimeoutDelta;
        private float _dissolveTimeout = 3f;

        // animation IDs
        protected int _animIdMove;
        protected int _animIdAttack;
        protected int _animIdDie;

        protected Vector3 _patrolDirection;
        private List<Material> _dissolveMaterials = new List<Material>();

        protected Vector3 _moveDirection;
        private PlayerController _player;

        protected override void Start()
		{
            base.Start();
            // set animations
            _animIdMove = Animator.StringToHash("Move");
            _animIdAttack = Animator.StringToHash("Attack");
            _animIdDie = Animator.StringToHash("Die");
            SelfAnimator.SetTrigger(_animIdMove);
            _player = PlayerController.Instance;

            // set time out delta
            _dissolveTimeoutDelta = 0f;
            // set dissolve materials
            var renders = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renders.Length; i++)
            {
                _dissolveMaterials.AddRange(renders[i].materials);
            }
        }

        protected Quaternion _currRotation;

        protected virtual void Update()
        {
            if (IsDead)
            {
                _dissolveTimeoutDelta += Time.deltaTime;
                var value = Mathf.Lerp(0f, 1f, _dissolveTimeoutDelta / _dissolveTimeout);
                SetDissolveValue(value);
                //SelfRigidbody.isKinematic = true;
                //SelfCollider.enabled = false;
                transform.rotation = _currRotation;
                return;
            }

            if (Player == null) { return; }
            LookAtPlayer();

            switch (State)
            {
                case EnemyState.Chasing:
                    HandleChasing();
                    break;

                case EnemyState.Attacking:
                    HandleAttacking();
                    break;

                case EnemyState.Patroling:
                    HandlePatroling();
                    break;
            }
        }

        protected virtual void LookAtPlayer()
        {
            _moveDirection = Player.transform.position - transform.position;
            Vector3 lookDirection = new Vector3(_moveDirection.x, 0, _moveDirection.z);
            if (lookDirection.sqrMagnitude <= 0.0001f) { return; }
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = lookRotation;
            _currRotation = lookRotation;
        }

        protected virtual void HandleChasing()
        {
            if (_moveDirection.sqrMagnitude <= Range * Range)
            {
                // attack
                SelfRigidbody.velocity = new Vector3(0f, SelfRigidbody.velocity.y, 0f);
                SelfAnimator.SetTrigger(_animIdAttack);
                State = EnemyState.Attacking;
                _attackTimeoutDelta = _attackTimeout;
            }
            else
            {
                // move
                Vector3 chaseDirection = new Vector3(_moveDirection.x, 0f, _moveDirection.z).normalized;
                Vector3 moveSpeed = new Vector3(chaseDirection.x * Speed, SelfRigidbody.velocity.y, chaseDirection.z * Speed);
                SelfRigidbody.velocity = moveSpeed;
            }
        }

        protected virtual void HandleAttacking()
        {
            if (_attackTimeoutDelta > 0)
            {
                _attackTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _patrolDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;

                SelfAnimator.SetTrigger(_animIdMove);
                _patrolTimeoutDelta = _patrolTimeout;
                State = EnemyState.Patroling;
            }
        }

        protected virtual void HandlePatroling()
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
                State = EnemyState.Chasing;
            }
        }

        public override void ApplyDamage(int damage)
        {
            if (IsDead) { return; }
            base.ApplyDamage(damage);

            //AudioKit.PlaySound("human1 hurt2");
            if (IsDead)
            {
                SelfAnimator.SetTrigger(_animIdDie);
                SelfCollider.enabled = false;
                SelfRigidbody.isKinematic = true;
                SelfRigidbody.DestroySelf();
                MinimapIcon.Hide();
                _deadFeedbacks?.PlayFeedbacks();
                OnDeath.Trigger();
                // spawn energy orb
                if (0.2f >= Random.Range(0f, 1f))
                {
                    GameObject newOrb = GameObjectsManager.Instance.SpawnEnergyOrb(transform.position);
                    Rigidbody rb = newOrb.GetComponent<Rigidbody>();
                    float randomScale = 0.5f;
                    Vector3 randomDirection = new Vector3(Random.Range(-randomScale, randomScale), 0.5f, Random.Range(-randomScale, randomScale));
                    rb.AddForce(randomDirection * 5, ForceMode.Impulse);
                }

                // recycle status if any
                Status[] statuses = GetComponentsInChildren<Status>();
                foreach(Status status in statuses)
                {
                    GameObjectsManager.Instance.DespawnStatus(status);
                }

                Destroy(gameObject, _deathCleanupDelay);
            }
        }

        public void MeleeAttackAnimationEffect()
        {
            AudioKit.PlaySound("fx_sword");
            if (Player == null) { return; }
            if ((Player.transform.position - transform.position).sqrMagnitude <= Range * Range)
            {
                Player.PlayerStats.ApplyDamage(Attack);
            }
		}

        public void SetDissolveValue(float value)
        {
            for (int i = 0; i < _dissolveMaterials.Count; i++)
            {
                _dissolveMaterials[i].SetFloat("_Dissolve", value);
            }
        }

        protected PlayerController Player
        {
            get
            {
                if (_player == null)
                {
                    _player = PlayerController.Instance;
                }
                return _player;
            }
        }

    }

    //[CustomEditor(typeof(Enemy))]
    //public class MyScriptEditor : Editor
    //{
    //    public override void OnInspectorGUI()
    //    {
    //        if (GUILayout.Button("装备武器"))
    //        {

    //        }
    //    }
    //}
}
