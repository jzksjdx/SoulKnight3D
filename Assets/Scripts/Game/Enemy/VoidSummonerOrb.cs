using MoreMountains.Feedbacks;
using UnityEngine;

namespace SoulKnight3D
{
    public sealed class VoidSummonerOrb : MonoBehaviour
    {
        [Header("Flight")]
        [SerializeField, Min(0.1f)] private float _speed = 10f;
        [SerializeField, Min(0f)] private float _arcHeight = 2.5f;

        [Header("Impact")]
        [SerializeField] private GameObject _slowCirclePrefab;
        [SerializeField, Min(0)] private int _landingDamage = 2;
        [SerializeField, Min(0f)] private float _landingDamageRadius = 1.2f;
        [SerializeField, Min(0f)] private float _impactHoldDuration = 0.2f;
        [SerializeField] private MMF_Player _impactFeedback;

        private PooledGameObject _pooledObject;
        private Rigidbody _rigidbody;
        private Collider _collider;
        private ParticleSystem[] _particles;
        private Renderer[] _renderers;
        private bool[] _rendererStates;

        private Vector3 _start;
        private Vector3 _control;
        private Vector3 _target;
        private float _travelDuration;
        private float _elapsed;
        private float _impactTimer;
        private bool _isFlying;
        private bool _isHoldingImpact;

        private void Awake()
        {
            CacheComponents();
        }

        private void Update()
        {
            if (_isFlying)
            {
                UpdateFlight();
            }
            else if (_isHoldingImpact)
            {
                _impactTimer -= Time.deltaTime;
                if (_impactTimer <= 0f)
                {
                    Release();
                }
            }
        }

        private void OnDisable()
        {
            _isFlying = false;
            _isHoldingImpact = false;
            _elapsed = 0f;
            _impactTimer = 0f;
        }

        public void Initialize(Vector3 start, Vector3 target)
        {
            CacheComponents();

            _start = start;
            _target = target;
            _control = Vector3.Lerp(start, target, 0.5f) +
                       Vector3.up * _arcHeight;
            _travelDuration = Mathf.Max(0.1f,
                Vector3.Distance(start, target) / Mathf.Max(0.1f, _speed));
            _elapsed = 0f;
            _isFlying = true;
            _isHoldingImpact = false;

            transform.position = start;
            if (_rigidbody != null)
            {
                _rigidbody.velocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.useGravity = false;
                _rigidbody.isKinematic = true;
            }
            if (_collider != null)
            {
                _collider.enabled = true;
            }

            RestoreRenderers();
            foreach (ParticleSystem particle in _particles)
            {
                particle.Play(true);
            }

            _pooledObject.ShowFromPool();
        }

        public void Configure(float speed, float arcHeight,
            GameObject slowCirclePrefab, int landingDamage,
            float landingDamageRadius, float impactHoldDuration,
            MMF_Player impactFeedback)
        {
            _speed = speed;
            _arcHeight = arcHeight;
            _slowCirclePrefab = slowCirclePrefab;
            _landingDamage = landingDamage;
            _landingDamageRadius = landingDamageRadius;
            _impactHoldDuration = impactHoldDuration;
            _impactFeedback = impactFeedback;
        }

        private void UpdateFlight()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _travelDuration);
            float inverse = 1f - t;
            Vector3 position = inverse * inverse * _start +
                               2f * inverse * t * _control +
                               t * t * _target;
            Vector3 tangent = 2f * inverse * (_control - _start) +
                              2f * t * (_target - _control);

            transform.position = position;
            if (tangent.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(tangent.normalized);
            }

            if (t >= 1f)
            {
                Impact();
            }
        }

        private void Impact()
        {
            if (!_isFlying) { return; }
            _isFlying = false;

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            PlayerController player = PlayerController.Instance;
            if (player != null)
            {
                Vector3 offset = player.transform.position - _target;
                offset.y = 0f;
                if (offset.sqrMagnitude <=
                    _landingDamageRadius * _landingDamageRadius)
                {
                    player.PlayerStats.ApplyDamage(_landingDamage);
                }
            }

            if (_slowCirclePrefab != null && GameObjectsManager.Instance != null)
            {
                GameObjectsManager.Instance.SpawnStatusZone(
                    _slowCirclePrefab, _target);
            }

            foreach (ParticleSystem particle in _particles)
            {
                particle.Stop(true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            HideRenderers();
            _impactFeedback?.PlayFeedbacks();

            _impactTimer = _impactHoldDuration;
            _isHoldingImpact = _impactTimer > 0f;
            if (!_isHoldingImpact)
            {
                Release();
            }
        }

        private void CacheComponents()
        {
            if (_pooledObject == null)
            {
                _pooledObject = GetComponent<PooledGameObject>();
            }
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }
            if (_collider == null)
            {
                _collider = GetComponent<Collider>();
            }
            if (_particles == null)
            {
                _particles = GetComponentsInChildren<ParticleSystem>(true);
            }
            if (_renderers == null)
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
                _rendererStates = new bool[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                {
                    _rendererStates[i] = _renderers[i].enabled;
                }
            }
        }

        private void HideRenderers()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].enabled = false;
                }
            }
        }

        private void RestoreRenderers()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].enabled = _rendererStates[i];
                }
            }
        }

        private void Release()
        {
            _isHoldingImpact = false;
            if (_pooledObject != null)
            {
                _pooledObject.ReleaseToPool();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
