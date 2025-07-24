using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public class SpikeTilesController : MonoBehaviour
    {
        public List<SpikeTile> SpikeTiles = new List<SpikeTile>();

        private bool _isSpikeOut = false;
        private bool _isPlayerInRoom = false;
        private List<TargetableObject> targets = new List<TargetableObject>();

        // timers 
        private float _spikeOutTimeout = 1f;
        private float _spikeOutTimeoutDelta;
        private float _spikeCooldown = 2f;
        private float _spikeCooldownDelta;

        void Start()
        {
            _spikeOutTimeoutDelta = _spikeOutTimeout;
            _spikeCooldownDelta = _spikeCooldown;

            foreach(SpikeTile spikeTile in SpikeTiles)
            {
                spikeTile.OnTargetEnter.Register((target) =>
                {
                    NewTargetEntered(target, spikeTile.Damage);
                }).UnRegisterWhenGameObjectDestroyed(spikeTile);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!_isPlayerInRoom) { return; }
            if (!_isSpikeOut)
            {
                if (_spikeOutTimeoutDelta >= 0f)
                {
                    _spikeOutTimeoutDelta -= Time.deltaTime;
                } else
                {
                    _isSpikeOut = true;
                    _spikeOutTimeoutDelta = _spikeOutTimeout;
                    AudioKit.PlaySound("fx_spike", false, null, 0.1f);
                    foreach (SpikeTile spikeTile in SpikeTiles)
                    {
                        spikeTile.ToggleSpike(true);
                    }
                }
            }
            else
            {
                if (_spikeCooldownDelta >= 0f)
                {
                    _spikeCooldownDelta -= Time.deltaTime;
                } else
                {
                    _isSpikeOut = false;
                    _spikeCooldownDelta = _spikeCooldown;
                    foreach (SpikeTile spikeTile in SpikeTiles)
                    {
                        spikeTile.ToggleSpike(false);
                    }
                }
            }
        }

        private void NewTargetEntered(TargetableObject target, int damage)
        {
            if (targets.Contains(target)) { return; }
            targets.Add(target);
            target.ApplyDamage(damage);
            GameController.Instance.SpawnDamageText(damage, target.transform.position + new Vector3(0, 0.5f, 0));
            ActionKit.Delay(0.4f, () =>
            {
                targets.Remove(target);
            }).Start(this);
        }

        public void ToggleSpikeTiles(bool isEnabled)
        {
            _isPlayerInRoom = isEnabled;
        }
    }

}
