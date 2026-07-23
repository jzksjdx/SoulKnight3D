using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;
using UnityEngine.Serialization;

namespace SoulKnight3D
{
    public abstract class BossEnemy : TargetableObject
    {
        [Header("Boss Enrage")]
        [SerializeField, Range(0.05f, 0.95f)] private float _enrageHealthFraction = 0.5f;
        [SerializeField, Min(0)] private int _enrageEnergyOrbCount = 10;
        [SerializeField, Range(0.1f, 1f)] private float _enragedAttackIntervalMultiplier = 0.65f;

        [Header("Boss Death")]
        [FormerlySerializedAs("_deathCleanupDelay")]
        [SerializeField, Min(0f)] private float _dissolveDelay = 3f;
        [SerializeField, Min(0.1f)] private float _dissolveDuration = 3f;

        private static readonly int DieTrigger = Animator.StringToHash("Die");

        public EasyEvent OnDeath = new EasyEvent();
        public EasyEvent OnEnraged = new EasyEvent();
        public bool IsEnraged { get; private set; }
        protected float AttackIntervalMultiplier => IsEnraged
            ? _enragedAttackIntervalMultiplier
            : 1f;

        private readonly List<Material> _dissolveMaterials = new List<Material>();
        private Rigidbody _deathRigidbody;
        private Collider _deathCollider;
        private Animator _deathAnimator;
        private Transform _deathMinimapIcon;
        private bool _deathSequenceStarted;

        protected override void Start()
        {
            base.Start();
            IsEnraged = false;
            _deathSequenceStarted = false;
            CacheDeathReferences();
            CacheDissolveMaterials();
        }

        public override void ApplyDamage(int damage)
        {
            if (IsDead) { return; }

            base.ApplyDamage(damage);
            if (IsDead)
            {
                BeginDeathSequence();
                return;
            }

            if (!IsEnraged &&
                Health.Value <= Mathf.CeilToInt(MaxHealth * _enrageHealthFraction))
            {
                IsEnraged = true;
                EnemyRewardDropSystem.DropEnergy(transform.position, _enrageEnergyOrbCount);
                OnBecameEnraged();
                OnEnraged.Trigger();
            }
        }

        protected virtual void OnBecameEnraged()
        {
        }

        protected virtual void OnDeathSequenceStarted()
        {
        }

        protected void ConfigureDeathReferences(
            Rigidbody rigidbodyReference,
            Collider colliderReference,
            Animator animatorReference,
            Transform minimapIconReference)
        {
            if (rigidbodyReference != null) { _deathRigidbody = rigidbodyReference; }
            if (colliderReference != null) { _deathCollider = colliderReference; }
            if (animatorReference != null) { _deathAnimator = animatorReference; }
            if (minimapIconReference != null) { _deathMinimapIcon = minimapIconReference; }
        }

        private void BeginDeathSequence()
        {
            if (_deathSequenceStarted) { return; }
            _deathSequenceStarted = true;

            OnDeathSequenceStarted();
            CacheDeathReferences();

            if (_deathAnimator != null)
            {
                _deathAnimator.SetTrigger(DieTrigger);
            }

            if (_deathCollider != null)
            {
                _deathCollider.enabled = false;
            }

            if (_deathRigidbody != null)
            {
                _deathRigidbody.velocity = Vector3.zero;
                _deathRigidbody.angularVelocity = Vector3.zero;
                _deathRigidbody.isKinematic = true;
            }

            if (_deathMinimapIcon != null)
            {
                _deathMinimapIcon.gameObject.Hide();
            }

            RecycleStatuses();
            OnDeath.Trigger();
            StartCoroutine(DissolveAndDestroy());
        }

        private void CacheDeathReferences()
        {
            if (_deathRigidbody == null)
            {
                _deathRigidbody = GetComponent<Rigidbody>();
            }

            if (_deathCollider == null)
            {
                _deathCollider = GetComponent<Collider>();
            }

            if (_deathAnimator == null)
            {
                _deathAnimator = GetComponentInChildren<Animator>(true);
            }

            if (_deathMinimapIcon != null) { return; }

            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == "EnemyMinimapIcon" ||
                    children[i].name == "MinimapIcon")
                {
                    _deathMinimapIcon = children[i];
                    break;
                }
            }
        }

        private void CacheDissolveMaterials()
        {
            _dissolveMaterials.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] materials = renderers[i].materials;
                for (int j = 0; j < materials.Length; j++)
                {
                    Material material = materials[j];
                    if (material == null || !material.HasProperty("_Dissolve")) { continue; }

                    material.SetFloat("_Dissolve", 0f);
                    _dissolveMaterials.Add(material);
                }
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

        private void SetDissolveValue(float value)
        {
            for (int i = 0; i < _dissolveMaterials.Count; i++)
            {
                Material material = _dissolveMaterials[i];
                if (material != null)
                {
                    material.SetFloat("_Dissolve", value);
                }
            }
        }

        private void RecycleStatuses()
        {
            Status[] statuses = GetComponentsInChildren<Status>();
            for (int i = 0; i < statuses.Length; i++)
            {
                GameObjectsManager.Instance?.DespawnStatus(statuses[i]);
            }
        }
    }
}
