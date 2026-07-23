using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using QFramework;
using UnityEngine;
using UnityEngine.Serialization;

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
        [Header("Death")]
        [FormerlySerializedAs("_deathCleanupDelay")]
        [SerializeField, Min(0f)] private float _dissolveDelay = 3f;
        [SerializeField, Min(0.1f)] private float _dissolveDuration = 3f;
        [SerializeField] private MMF_Player _deadFeedbacks;

        [Header("Death Rewards (Soul Knight 1.8.4)")]
        [SerializeField, Range(0, 100)] private int _rewardRate = 20;
        [SerializeField] private int[] _rewardValues = { 0, 0, 1, 1 };

        // animation IDs
        protected int _animIdMove;
        protected int _animIdAttack;
        protected int _animIdDie;

        protected Vector3 _patrolDirection;
        private readonly List<Material> _dissolveMaterials = new List<Material>();

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

            Renderer[] renders = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renders.Length; i++)
            {
                Material[] materials = renders[i].materials;
                for (int j = 0; j < materials.Length; j++)
                {
                    if (materials[j] != null && materials[j].HasProperty("_Dissolve"))
                    {
                        materials[j].SetFloat("_Dissolve", 0f);
                        _dissolveMaterials.Add(materials[j]);
                    }
                }
            }
        }

        protected Quaternion _currRotation;

        protected virtual void Update()
        {
            if (IsDead)
            {
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
                EnemyRewardDropSystem.Drop(transform.position, _rewardRate, _rewardValues);

                // recycle status if any
                Status[] statuses = GetComponentsInChildren<Status>();
                foreach(Status status in statuses)
                {
                    GameObjectsManager.Instance.DespawnStatus(status);
                }

                StartCoroutine(DissolveAndDestroy());
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

        private IEnumerator DissolveAndDestroy()
        {
            if (_dissolveDelay > 0f)
            {
                yield return new WaitForSeconds(_dissolveDelay);
            }

            float elapsed = 0f;
            while (elapsed < _dissolveDuration)
            {
                elapsed += Time.deltaTime;
                SetDissolveValue(Mathf.Clamp01(elapsed / _dissolveDuration));
                yield return null;
            }

            SetDissolveValue(1f);
            Destroy(gameObject);
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
