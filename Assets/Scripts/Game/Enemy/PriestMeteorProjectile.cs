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
        private GameObject _warning;
        private Renderer _warningRenderer;
        private Material _warningMaterial;

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
            StartCoroutine(FallRoutine());
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
            Destroy(gameObject);
        }

        private void CreateWarning()
        {
            if (_warningPrefab == null) { return; }

            _warning = Instantiate(_warningPrefab,
                _targetPosition + Vector3.up * 0.02f, Quaternion.identity);
            _warningRenderer = _warning.GetComponentInChildren<Renderer>();
            if (_warningRenderer != null)
            {
                _warningMaterial = _warningRenderer.material;
            }
            UpdateWarning(0f);
        }

        private void UpdateWarning(float progress)
        {
            if (_warning == null) { return; }

            float diameter = _radius * 2f;
            float scale = Mathf.Lerp(diameter * 0.08f, diameter, progress);
            _warning.transform.localScale = new Vector3(scale, 0.015f, scale);
        }

        private IEnumerator FadeWarning()
        {
            if (_warning == null) { yield break; }

            const float fadeDuration = 0.18f;
            float elapsed = 0f;
            Color startingColor = _warningMaterial != null
                ? _warningMaterial.color
                : Color.red;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                if (_warningMaterial != null)
                {
                    Color color = startingColor;
                    color.a = Mathf.Lerp(startingColor.a, 0f, elapsed / fadeDuration);
                    _warningMaterial.color = color;
                }
                yield return null;
            }

            if (_warningMaterial != null) { Destroy(_warningMaterial); }
            Destroy(_warning);
            _warningMaterial = null;
            _warning = null;
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

        private void OnDestroy()
        {
            if (_warning != null) { Destroy(_warning); }
            if (_warningMaterial != null) { Destroy(_warningMaterial); }
            _warning = null;
            _warningMaterial = null;
        }
    }
}
