using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class Sunflower : Weapon
    {
        public Transform SunSpawnPoint;
        private MeshRenderer _mesh;

        private Material _meshMat;
        private Color _originalEmissionColor = new Color(191f / 255f, 149f / 255f, 0f);

        private float _chargeTime;
        private float _chargeProgress = 0f;

        private float _chargeResetDelta = 0f;
        private float _chargeResetTimeout = 0.2f;

        private void Start()
        {
            _chargeTime = Data.Cooldown;
            _mesh = GetComponentInChildren<MeshRenderer>();
            _meshMat = _mesh.material;
            _meshMat.SetVector("_EmissionColor", _originalEmissionColor);
        }

        protected override void Update()
        {
            _chargeResetDelta += Time.deltaTime;
            if (_chargeResetDelta > _chargeResetTimeout)
            {
                _chargeProgress = 0f;
                _meshMat.SetVector("_EmissionColor", _originalEmissionColor);
            }
        }

        public override void Attack()
        {
            _chargeResetDelta = 0f;
            if (_chargeProgress < _chargeTime)
            {
                _chargeProgress += Time.deltaTime;
                //Debug.Log("_chargeProgress: " + _chargeProgress);
                _meshMat.SetVector("_EmissionColor",
                    Vector4.Lerp(_originalEmissionColor,
                    _originalEmissionColor * Mathf.Pow(2, 4f),
                    Mathf.Pow(_chargeProgress / _chargeTime, 2)));
                if (_chargeProgress >= _chargeTime)
                {
                    _chargeProgress = 0f;
                    AudioKit.PlaySound("ProduceSun");
                    GameObject newSun = GameObjectsManager.Instance.SpawnSun(SunSpawnPoint.position);
                    Rigidbody rb = newSun.GetComponent<Rigidbody>();
                    float randomScale = 0.3f;
                    Vector3 randomDirection = Vector3.up + new Vector3(Random.Range(-randomScale, randomScale), 0f, Random.Range(-randomScale, randomScale));
                    rb.AddForce(randomDirection * 5, ForceMode.Impulse);
                }
            }
            
        }
    }

}
