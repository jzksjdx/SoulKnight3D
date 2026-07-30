using System.Collections;
using MoreMountains.Feedbacks;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public sealed class BlindBox : InteractiveItem
    {
        [Header("Rewards")]
        [SerializeField] private WeaponDropPoolSO _weaponPool;
        [SerializeField, Range(0f, 1f)] private float _weaponRewardChance = 0.5f;
        [SerializeField, Min(1)] private int _minimumCoinValue = 5;
        [SerializeField, Min(1)] private int _maximumCoinValue = 25;
        [SerializeField] private Transform _rewardSpawnPoint;

        [Header("Repeat")]
        [SerializeField, Range(0f, 1f)] private float _remainChance = 0.5f;
        [SerializeField, Min(0f)] private float _interactionCooldown = 1f;

        [Header("Presentation")]
        [SerializeField] private MMF_Player _openFeedbacks;
        [SerializeField, Min(0f)] private float _despawnDelay = 0.5f;

        protected override void Start()
        {
            base.Start();

            if (_rewardSpawnPoint == null)
            {
                _rewardSpawnPoint = transform;
            }
            if (_openFeedbacks == null)
            {
                _openFeedbacks = GetComponentInChildren<MMF_Player>(true);
            }

            string labelText =
                _languageSystem.CurrentLanguage == LanguageSystem.Languages.Chinese
                    ? "盲盒"
                    : "Blind Box";
            Label?.SetLabelText(labelText, WeaponData.WeaponRarity.White);
        }

        public override void Interact()
        {
            if (!IsInteractable)
            {
                return;
            }

            SetInteractable(false);
            _openFeedbacks?.PlayFeedbacks();
            GrantReward();

            bool remains = GameRandom.Value(GameRandomStream.Rewards) < _remainChance;
            StartCoroutine(ResolveOpen(remains));
        }

        private void GrantReward()
        {
            Vector3 spawnPosition = _rewardSpawnPoint.position;
            bool shouldSpawnWeapon =
                GameRandom.Value(GameRandomStream.Rewards) < _weaponRewardChance;
            if (shouldSpawnWeapon && TrySpawnWeapon(spawnPosition))
            {
                return;
            }

            int minimumValue = Mathf.Max(1, _minimumCoinValue);
            int maximumValue = Mathf.Max(minimumValue, _maximumCoinValue);
            int totalValue = GameRandom.Range(
                GameRandomStream.Rewards, minimumValue, maximumValue + 1);
            EnemyRewardDropSystem.DropRandomCoinValue(spawnPosition, totalValue);
        }

        private bool TrySpawnWeapon(Vector3 spawnPosition)
        {
            if (_weaponPool == null)
            {
                Debug.LogWarning($"Blind Box '{name}' has no weapon pool configured.");
                return false;
            }

            GameObject pickupPrefab = _weaponPool.GetRandomDistinctPickupPrefab(
                GameRandom.GetStream(GameRandomStream.Rewards));
            if (pickupPrefab == null)
            {
                Debug.LogWarning(
                    $"Blind Box '{name}' found no available weapons in '{_weaponPool.name}'.");
                return false;
            }

            GameObject pickup = Instantiate(
                pickupPrefab, spawnPosition, Quaternion.identity);
            if (pickup.TryGetComponent(out Rigidbody pickupBody))
            {
                Vector3 launchDirection = Vector3.up + new Vector3(
                    GameRandom.Range(GameRandomStream.Gameplay, -0.25f, 0.25f),
                    0f,
                    GameRandom.Range(GameRandomStream.Gameplay, -0.25f, 0.25f));
                pickupBody.AddForce(launchDirection * 2f, ForceMode.Impulse);
            }

            return true;
        }

        private IEnumerator ResolveOpen(bool remains)
        {
            float delay = remains ? _interactionCooldown : _despawnDelay;
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (remains)
            {
                SetInteractable(true);
            }
            else
            {
                Destroy(gameObject);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _minimumCoinValue = Mathf.Max(1, _minimumCoinValue);
            _maximumCoinValue = Mathf.Max(_minimumCoinValue, _maximumCoinValue);
        }
#endif
    }
}
