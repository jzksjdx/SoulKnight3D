using System.Collections;
using MoreMountains.Feedbacks;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public sealed class Mine : TargetableObject
    {
        public enum MineRewardType
        {
            EnergyOrbs,
            CopperCoins
        }

        [SerializeField] private MineRewardType _rewardType;
        [SerializeField, Min(1)] private int _dropCount = 10;
        [SerializeField, Min(0f)] private float _dropHeight = 0.5f;
        [SerializeField] private MMF_Player _brokenFeedbacks;

        public EasyEvent OnBroken = new EasyEvent();

        private bool _hasBroken;

        public MineRewardType RewardType => _rewardType;
        public int DropCount => _dropCount;

        private void Awake()
        {
            if (_brokenFeedbacks == null)
            {
                _brokenFeedbacks = GetComponentInChildren<MMF_Player>(true);
            }
        }

        public override void ApplyDamage(int damage)
        {
            if (_hasBroken || IsDead || damage <= 0) { return; }

            base.ApplyDamage(damage);
            if (!IsDead) { return; }

            _hasBroken = true;
            DisableColliders();
            HideMesh();
            StartCoroutine(PlayBrokenFeedbackAndDestroy());

            Vector3 dropPosition = transform.position + Vector3.up * _dropHeight;
            if (_rewardType == MineRewardType.EnergyOrbs)
            {
                EnemyRewardDropSystem.DropEnergy(dropPosition, _dropCount);
            }
            else
            {
                EnemyRewardDropSystem.DropCoins(
                    dropPosition, CoinPickup.CoinType.Copper, _dropCount);
            }

            OnBroken.Trigger();
        }

        private void DisableColliders()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private void HideMesh()
        {
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }
        }

        private IEnumerator PlayBrokenFeedbackAndDestroy()
        {
            if (_brokenFeedbacks == null)
            {
                Destroy(gameObject);
                yield break;
            }

            _brokenFeedbacks.PlayFeedbacks();

            // Particle instantiation happens during feedback playback. Waiting one
            // frame lets nested particle systems enter their playing state.
            yield return null;
            while (_brokenFeedbacks != null &&
                   (_brokenFeedbacks.IsPlaying || HasLiveBrokenParticles()))
            {
                yield return null;
            }

            Destroy(gameObject);
        }

        private bool HasLiveBrokenParticles()
        {
            ParticleSystem[] particleSystems =
                _brokenFeedbacks.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                if (particleSystems[i].IsAlive(true))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
