using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class SunflowerGene : MonoBehaviour
    {
        public float SunSpawnChance = 0.2f;
        private Weapon _weapon;
        private bool _isProducingSun = false;

        private MeshRenderer _mesh;
        private Material _meshMat;
        private Color _originalEmissionColor = new Color(191f / 255f, 149f / 255f, 0f);

        private float _emissionTimeout = 0.5f;
        private float _emissionTimeoutDelta = 0.5f;

        private void Start()
        {
            _weapon = GetComponent<Weapon>();
            _mesh = GetComponentInChildren<MeshRenderer>();
            _meshMat = _mesh.material;
            _meshMat.SetVector("_EmissionColor", _originalEmissionColor);

            _weapon.OnWeaponFired.Register(() =>
            {
                if (SunSpawnChance >= Random.Range(0f, 1f))
                {
                    _isProducingSun = true;
                    _emissionTimeoutDelta = _emissionTimeout;
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void Update()
        {
            if (_isProducingSun) // light up
            {
                if (_emissionTimeoutDelta >= 0f)
                {
                    _emissionTimeoutDelta -= Time.deltaTime;
                    _meshMat.SetVector("_EmissionColor",
                        Vector4.Lerp(_originalEmissionColor,
                        _originalEmissionColor * Mathf.Pow(2, 4f),
                        Mathf.Pow(1 - _emissionTimeoutDelta / _emissionTimeout, 2)));
                    if (_emissionTimeoutDelta <= 0f)
                    {
                        // produce sun
                        _isProducingSun = false;
                        AudioKit.PlaySound("ProduceSun");
                        GameObject newSun = GameObjectsManager.Instance.SpawnSun(transform.position);
                        Rigidbody rb = newSun.GetComponent<Rigidbody>();
                        float randomScale = 0.3f;
                        Vector3 randomDirection = Vector3.up + new Vector3(Random.Range(-randomScale, randomScale), 0f, Random.Range(-randomScale, randomScale));
                        rb.AddForce(randomDirection * 5, ForceMode.Impulse);
                        _emissionTimeoutDelta = _emissionTimeout;
                    }
                }
            } else // dim
            {
                if (_emissionTimeoutDelta >= 0f)
                {
                    _emissionTimeoutDelta -= Time.deltaTime;
                    _meshMat.SetVector("_EmissionColor",
                        Vector4.Lerp(_originalEmissionColor * Mathf.Pow(2, 4f),
                        _originalEmissionColor,
                        Mathf.Pow(1 - _emissionTimeoutDelta / _emissionTimeout, 2)));
                }
            }
        }

    }

}
