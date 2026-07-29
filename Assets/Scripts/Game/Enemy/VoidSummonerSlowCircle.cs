using MoreMountains.Feedbacks;
using UnityEngine;

namespace SoulKnight3D
{
    public sealed class VoidSummonerSlowCircle : StatusZone
    {
        [SerializeField] private MMF_Player _spawnFeedback;

        private ParticleSystem[] _particles;

        protected override void Start()
        {
            _particles = GetComponentsInChildren<ParticleSystem>(true);
            base.Start();
        }

        public override void ActivateStatusZone(Vector3 position)
        {
            base.ActivateStatusZone(position);

            if (_particles == null)
            {
                _particles = GetComponentsInChildren<ParticleSystem>(true);
            }

            foreach (ParticleSystem particle in _particles)
            {
                particle.Play(true);
            }
            _spawnFeedback?.PlayFeedbacks();
        }

        public override void Reset()
        {
            if (_particles != null)
            {
                foreach (ParticleSystem particle in _particles)
                {
                    particle.Stop(true,
                        ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            base.Reset();
        }

        public void Configure(Collider zoneCollider, GameObject statusPrefab,
            float duration, MMF_Player spawnFeedback)
        {
            Type = Status.StatusType.SpeedDown;
            _collider = zoneCollider;
            _statusPrefab = statusPrefab;
            _duration = duration;
            _spawnFeedback = spawnFeedback;
        }
    }
}
