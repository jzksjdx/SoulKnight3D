using System.Collections;
using UnityEngine;

namespace SoulKnight3D
{
    public sealed class PriestMeteorProjectile : MonoBehaviour
    {
        private Vector3 _startPosition;
        private Vector3 _targetPosition;
        private float _fallDuration;
        private float _radius;
        private int _damage;
        private GameObject _warningPrefab;
        private PooledGameObject _pooledObject;
        private PooledGameObject _warning;
        private Renderer _warningRenderer;
        private MaterialPropertyBlock _warningProperties;
        private Color _warningColor = new Color(1f, 0f, 0f, 0.48f);
        private bool _initialized;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        public void Initialize(Vector3 targetPosition, float height, float fallDuration,
            float radius, int damage, GameObject warningPrefab)
        {
            _targetPosition = targetPosition;
            _targetPosition.y = targetPosition.y;
            _startPosition = _targetPosition + Vector3.up * Mathf.Max(0f, height);
            transform.position = _startPosition;
            _fallDuration = Mathf.Max(0.05f, fallDuration);
            _radius = Mathf.Max(0.05f, radius);
            _damage = Mathf.Max(0, damage);
            _warningPrefab = warningPrefab;
            _initialized = true;
        }

        private void OnEnable()
        {
            if (_initialized)
            {
                StartCoroutine(FallRoutine());
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ReleaseWarning();
            _initialized = false;
        }

        private IEnumerator FallRoutine()
        {
            CreateWarning();
            float elapsed = 0f;
            while (elapsed < _fallDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _fallDuration);
                transform.position = Vector3.Lerp(_startPosition, _targetPosition, progress);
                UpdateWarning(progress);
                yield return null;
            }

            DamagePlayer();
            yield return FadeWarning();
            Release();
        }

        private void CreateWarning()
        {
            if (_warningPrefab == null || GameObjectsManager.Instance == null) { return; }

            _warning = GameObjectsManager.Instance.SpawnPooledObject(_warningPrefab,
                _targetPosition + Vector3.up * 0.02f, Quaternion.identity);
            if (_warning == null) { return; }

            _warningRenderer = _warning.GetComponentInChildren<Renderer>();
            if (_warningRenderer != null)
            {
                _warningProperties ??= new MaterialPropertyBlock();
                Material material = _warningRenderer.sharedMaterial;
                if (material != null)
                {
                    if (material.HasProperty(BaseColor))
                    {
                        _warningColor = material.GetColor(BaseColor);
                    }
                    else if (material.HasProperty(ColorProperty))
                    {
                        _warningColor = material.GetColor(ColorProperty);
                    }
                }
                SetWarningAlpha(_warningColor.a);
            }
            UpdateWarning(0f);
            _warning.ShowFromPool();
        }

        private void UpdateWarning(float progress)
        {
            if (_warning == null || _warning.IsReleased) { return; }

            float diameter = _radius * 2f;
            float scale = Mathf.Lerp(diameter * 0.08f, diameter, progress);
            _warning.transform.localScale = new Vector3(scale, 0.015f, scale);
        }

        private IEnumerator FadeWarning()
        {
            if (_warning == null || _warning.IsReleased) { yield break; }

            const float fadeDuration = 0.18f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                SetWarningAlpha(Mathf.Lerp(
                    _warningColor.a, 0f, elapsed / fadeDuration));
                yield return null;
            }

            ReleaseWarning();
        }

        private void DamagePlayer()
        {
            PlayerController player = PlayerController.Instance;
            if (player == null) { return; }

            Vector3 difference = player.transform.position - _targetPosition;
            difference.y = 0f;
            if (difference.sqrMagnitude <= _radius * _radius)
            {
                player.PlayerStats.ApplyDamage(_damage);
            }
        }

        private void SetWarningAlpha(float alpha)
        {
            if (_warningRenderer == null) { return; }

            _warningProperties ??= new MaterialPropertyBlock();
            _warningRenderer.GetPropertyBlock(_warningProperties);
            Color color = _warningColor;
            color.a = alpha;
            _warningProperties.SetColor(BaseColor, color);
            _warningProperties.SetColor(ColorProperty, color);
            _warningRenderer.SetPropertyBlock(_warningProperties);
        }

        private void ReleaseWarning()
        {
            if (_warningRenderer != null)
            {
                _warningRenderer.SetPropertyBlock(null);
            }
            if (_warning != null && !_warning.IsReleased)
            {
                _warning.ReleaseToPool();
            }

            _warning = null;
            _warningRenderer = null;
        }

        private void Release()
        {
            _initialized = false;
            ReleaseWarning();
            if (_pooledObject == null)
            {
                _pooledObject = GetComponent<PooledGameObject>();
            }

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
