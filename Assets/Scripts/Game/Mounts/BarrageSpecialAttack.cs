using System.Collections;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ArmorMount))]
    public sealed class BarrageSpecialAttack : MountSpecialAttack
    {
        [Header("References")]
        [SerializeField] private ArmorMount _mount;
        [SerializeField] private ConsecutiveGun _weapon;
        [SerializeField] private Transform _swarmLaunchPoint;
        [SerializeField] private GameObject _homingRocketPrefab;

        [Header("Cooldown")]
        [SerializeField, Min(0.1f)] private float _cooldown = 10f;

        [Header("Enhanced Main Weapon")]
        [SerializeField, Min(1)] private int _enhancedBurstCount = 3;
        [SerializeField, Min(2)] private int _enhancedShotsPerBurst = 8;
        [SerializeField, Min(1f)] private float _enhancedDamageMultiplier = 1.3f;

        [Header("Rocket Swarm")]
        [SerializeField, Min(1)] private int _swarmRocketCount = 10;
        [SerializeField, Min(0.01f)] private float _swarmShotInterval = 0.1f;
        [SerializeField, Range(0f, 90f)] private float _swarmSpreadAngle = 90f;
        [SerializeField, Min(0.01f)] private float _swarmLaunchSpeed = 8f;
        [SerializeField, Min(1f)] private float _swarmDamageMultiplier = 1.3f;
        [SerializeField] private string _activationSound = "fx_ice_shock";

        private float _cooldownRemaining;
        private float _lastReportedCharge = -1f;
        private Coroutine _swarmRoutine;

        public override float ChargeNormalized =>
            _cooldown <= 0f
                ? 1f
                : Mathf.Clamp01(1f - _cooldownRemaining / _cooldown);

        private void Awake()
        {
            if (_mount == null) { _mount = GetComponent<ArmorMount>(); }
            if (_weapon == null)
            {
                _weapon = GetComponentInChildren<ConsecutiveGun>(true);
            }
        }

        private void OnEnable()
        {
            ReportCharge(true);
        }

        private void Update()
        {
            if (_cooldownRemaining <= 0f) { return; }

            _cooldownRemaining =
                Mathf.Max(0f, _cooldownRemaining - Time.deltaTime);
            ReportCharge(false);
        }

        private void OnDisable()
        {
            CancelActiveEffect();
        }

        public override bool TryActivate()
        {
            if (_mount == null || !_mount.IsMounted ||
                _weapon == null || _homingRocketPrefab == null ||
                _swarmLaunchPoint == null || _cooldownRemaining > 0f)
            {
                return false;
            }

            _cooldownRemaining = Mathf.Max(0.1f, _cooldown);
            ReportCharge(true);
            _weapon.ActivateEnhancedBursts(
                _enhancedBurstCount,
                _enhancedShotsPerBurst,
                _enhancedDamageMultiplier);

            if (!string.IsNullOrWhiteSpace(_activationSound))
            {
                AudioKit.PlaySound(_activationSound);
            }

            if (_swarmRoutine != null)
            {
                StopCoroutine(_swarmRoutine);
            }
            _swarmRoutine = StartCoroutine(FireSwarm());
            return true;
        }

        public override void HandleRideEnded()
        {
            CancelActiveEffect();
        }

        private IEnumerator FireSwarm()
        {
            WaitForSeconds wait =
                new WaitForSeconds(Mathf.Max(0.01f, _swarmShotInterval));
            for (int i = 0; i < Mathf.Max(1, _swarmRocketCount); i++)
            {
                if (_mount == null || !_mount.IsMounted)
                {
                    break;
                }

                _weapon.SpawnSpecialRocket(
                    _homingRocketPrefab,
                    _swarmLaunchPoint.position,
                    GetHemisphereDirection(),
                    _swarmLaunchSpeed,
                    _swarmDamageMultiplier);
                yield return wait;
            }

            _swarmRoutine = null;
        }

        private Vector3 GetHemisphereDirection()
        {
            float angle = Mathf.Clamp(_swarmSpreadAngle, 0f, 90f);
            float minimumCosine = Mathf.Cos(angle * Mathf.Deg2Rad);
            float cosine = Random.Range(minimumCosine, 1f);
            float sine = Mathf.Sqrt(Mathf.Max(0f, 1f - cosine * cosine));
            float azimuth = Random.Range(0f, Mathf.PI * 2f);
            Vector3 localDirection = new Vector3(
                sine * Mathf.Cos(azimuth),
                sine * Mathf.Sin(azimuth),
                cosine);
            return _swarmLaunchPoint.TransformDirection(localDirection)
                .normalized;
        }

        private void CancelActiveEffect()
        {
            if (_swarmRoutine != null)
            {
                StopCoroutine(_swarmRoutine);
                _swarmRoutine = null;
            }
            _weapon?.CancelEnhancedBursts();
        }

        private void ReportCharge(bool force)
        {
            float charge = ChargeNormalized;
            if (!force && Mathf.Abs(charge - _lastReportedCharge) < 0.001f)
            {
                return;
            }

            _lastReportedCharge = charge;
            OnChargeChanged.Trigger(charge);
        }
    }
}
