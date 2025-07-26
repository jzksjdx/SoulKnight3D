using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using MoreMountains.Feedbacks;

namespace SoulKnight3D
{
    public class Werewolf : TargetableObject
    {
        public enum State
        {
            Idle, Chase, Attack, SkillJump, SkillCall
        }

        public FSM<State> FSM = new FSM<State>();

        [SerializeField] private int _attack = 3;
        [SerializeField] private float _range = 1f;
        [SerializeField] private float _chaseTime = 10f;
        public Rigidbody SelfRigidbody;
        public Animator SelfAnimator;
        public CapsuleCollider SelfCollider;
        [SerializeField] private Transform _meshRoot;
        [SerializeField] private Transform _minimapIcon;

        // feedbacks
        [SerializeField] private MMF_Player _feedbackAtk;
        [SerializeField] private MMF_Player _feedbackJump;
        [SerializeField] private MMF_Player _feedbackCall;

        // call skill enemy prefabs
        [SerializeField] private List<GameObject> _minionPrefabs;
        [SerializeField] private LayerMask _itemLayerMask;

        // animation IDs
        protected int _animIdWalk;
        protected int _animIdAttack;
        protected int _animIdDie;
        protected int _animIdSkillCall;
        protected int _animIdSkillJump;

        // FSM parameters
        private float _idleTime = 2f;
        protected Vector3 _moveDirection;
        protected Quaternion _currRotation;
        private int _attackCount = 0;

        // events
        public EasyEvent OnDeath = new EasyEvent();

        protected override void Start()
        {
            base.Start();
            _animIdWalk = Animator.StringToHash("Walk");
            _animIdAttack = Animator.StringToHash("Attack");
            _animIdDie = Animator.StringToHash("Die");
            _animIdSkillCall = Animator.StringToHash("Call");
            _animIdSkillJump = Animator.StringToHash("Jump");

            FSM.AddState(State.Idle, new IdleState(FSM, this));
            FSM.AddState(State.Chase, new ChaseState(FSM, this));
            FSM.AddState(State.Attack, new AttackState(FSM, this));
            FSM.AddState(State.SkillJump, new SkillJumpState(FSM, this));
            FSM.AddState(State.SkillCall, new SkillCallState(FSM, this));

            FSM.StartState(State.Idle);
        }

        private void Update()
        {
            FSM.Update();
        }

        private void FixedUpdate()
        {
            FSM.FixedUpdate();
        }

        private void OnDestroy()
        {
            FSM.Clear();
        }

        public override void ApplyDamage(int damage)
        {
            if (IsDead) { return; }
            base.ApplyDamage(damage);
            // update UI
            UIKit.GetPanel<UIGamePanel>().BossHealthBar.fillAmount = (float)Health.Value / MaxHealth;

            if (IsDead)
            {
                UIKit.GetPanel<UIGamePanel>().BossHealthRect.Hide();
                FSM.Clear();
                SelfAnimator.SetTrigger(_animIdDie);
                SelfCollider.enabled = false;
                SelfRigidbody.isKinematic = true;
                SelfRigidbody.DestroySelf();
                _minimapIcon.Hide();
                AudioKit.PlaySound("fx_wolf_growl");
                OnDeath.Trigger();

                // show portal

                // recycle status if any
                Status[] statuses = GetComponentsInChildren<Status>();
                foreach (Status status in statuses)
                {
                    GameObjectsManager.Instance.DespawnStatus(status);
                }

                Destroy(gameObject, 3);
            }
        }

        protected virtual void LookAtPlayer()
        {
            _moveDirection = PlayerController.Instance.transform.position - transform.position;
            Vector3 lookDirection = new Vector3(_moveDirection.x, 0, _moveDirection.z);
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = lookRotation;
            _currRotation = lookRotation;
        }

        public void AnimationAttackEffectEvent()
        {
            _feedbackAtk?.PlayFeedbacks();
            float distance = (PlayerController.Instance.transform.position - transform.position).magnitude;
            if (distance <= _range)
            {
                PlayerController.Instance.PlayerStats.ApplyDamage(_attack);
            }
        }

        public void AnimationJumpAtkEffectEvent()
        {
            // stop movements
            SelfRigidbody.velocity = new Vector3(0f, SelfRigidbody.velocity.y, 0f);
            SelfRigidbody.useGravity = true;
            // effects
            _feedbackJump?.PlayFeedbacks();
            // damage
            float distance = (PlayerController.Instance.transform.position - transform.position).magnitude;
            if (distance <= _range * 1.5f)
            {
                PlayerController.Instance.PlayerStats.ApplyDamage(_attack);
            }
        }

        public void AnimationCallEvent()
        {
            _feedbackCall?.PlayFeedbacks();
            // spawn enemies
            float spawnRadius = 3f;
            int enemyCoutn = Random.Range(3, 5);
            for(int i = 0; i < enemyCoutn; i++)
            {
                Vector3 spawnPosition;
                bool isValidPosition;
                do
                {
                    Vector3 randomOffset = new Vector3(Random.Range(-spawnRadius, spawnRadius), 0.05f, Random.Range(-spawnRadius, spawnRadius));
                    spawnPosition = transform.position + randomOffset;
                    isValidPosition = !Physics.CheckSphere(spawnPosition, 0.5f, _itemLayerMask);
                }
                while (!isValidPosition);
                Instantiate(_minionPrefabs[Random.Range(0, _minionPrefabs.Count)], spawnPosition, Quaternion.identity);
            }
            AudioKit.PlaySound("fx_show_up");
        }

        public void AnimationAttackFinishEvent()
        {
            FSM.ChangeState(State.Idle);
        }

        // IDLE STATE
        public class IdleState : AbstractState<State, Werewolf>
        {
            
            private float _idleTimer = 0f;

            public IdleState(FSM<State> fsm, Werewolf target) : base(fsm, target)
            {
                
            }

            protected override void OnEnter()
            {
                _idleTimer = mTarget._idleTime;
            }

            protected override void OnExit()
            {
                base.OnExit();
                mTarget._idleTime = 1f;
            }

            protected override void OnUpdate()
            {
                if (_idleTimer >= 0f)
                {
                    _idleTimer -= Time.deltaTime;
                    if (_idleTimer <= 0f)
                    {
                        EnterNextState();
                    }
                } else
                {
                    EnterNextState();
                }
            }

            private void EnterNextState()
            {
                mFSM.ChangeState(State.Chase);
            }
        }

        // CHASE STATE
        public class ChaseState : AbstractState<State, Werewolf>
        {
            private float _chaseTimer;

            public ChaseState(FSM<State> fsm, Werewolf target) : base(fsm, target)
            {
            }

            protected override void OnEnter()
            {
                _chaseTimer = Random.Range(mTarget._chaseTime * 0.5f, mTarget._chaseTime * 1.5f);
                mTarget.SelfAnimator.SetTrigger(mTarget._animIdWalk);

                if (mTarget._attackCount >= 3f) // force use skill if attacked too many times
                {
                    if (Random.Range(0, 1f) >= 0.5f) // 50% chance to use each skill
                    {
                        mFSM.ChangeState(State.SkillCall);
                    }
                    else
                    {
                        mFSM.ChangeState(State.SkillJump);
                    }
                }
            }

            protected override void OnFixedUpdate()
            {
                HandleChasing();
            }

            protected override void OnExit()
            {
                base.OnExit();
                mTarget.SelfRigidbody.velocity = new Vector3(0f, mTarget.SelfRigidbody.velocity.y, 0f);
                mTarget.SelfRigidbody.angularVelocity = Vector3.zero;
            }

            private void HandleChasing()
            {
                if (_chaseTimer >= 0f)
                {
                    _chaseTimer -= Time.deltaTime;

                    // move
                    mTarget.LookAtPlayer();
                    Vector3 moveSpeed = new Vector3(
                        mTarget._moveDirection.normalized.x * mTarget.Speed,
                        mTarget.SelfRigidbody.velocity.y,
                        mTarget._moveDirection.normalized.z * mTarget.Speed);
                    mTarget.SelfRigidbody.velocity = moveSpeed;

                    
                    if (mTarget._moveDirection.magnitude <= mTarget._range)
                    {
                        // attack stage
                        mFSM.ChangeState(State.Attack);
                    }

                    // skill
                    if (_chaseTimer <= 0f)
                    {
                        if (Random.Range(0, 1f) >= 0.5f) // 50% chance to use each skill
                        {
                            mFSM.ChangeState(State.SkillCall);
                        } else
                        {
                            mFSM.ChangeState(State.SkillJump);
                        }
                    }
                }
            }
        }

        // ATTACK STATE
        public class AttackState : AbstractState<State, Werewolf>
        {
            
            public AttackState(FSM<State> fsm, Werewolf target) : base(fsm, target)
            {
                
            }

            protected override void OnEnter()
            {
                mTarget._attackCount++;
                mTarget.SelfAnimator.SetTrigger(mTarget._animIdAttack);
            }
        }

        // SKILL JUMP STATE
        public class SkillJumpState : AbstractState<State, Werewolf>
        {
            
            public SkillJumpState(FSM<State> fsm, Werewolf target) : base(fsm, target)
            {
                
            }

            protected override void OnEnter()
            {
                mTarget.LookAtPlayer();
                mTarget._attackCount = 0;
                mTarget.SelfAnimator.SetTrigger(mTarget._animIdSkillJump);
                mTarget.SelfRigidbody.useGravity = false;
            }

            protected override void OnUpdate()
            {
                base.OnUpdate();
                
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();
            }
        }

        // SKILL CALL STATE
        public class SkillCallState : AbstractState<State, Werewolf>
        {
            
            public SkillCallState(FSM<State> fsm, Werewolf target) : base(fsm, target)
            {
                
            }

            protected override void OnEnter()
            {
                mTarget._attackCount = 0;
                mTarget.SelfAnimator.SetTrigger(mTarget._animIdSkillCall);
            }
        }
    }

}
