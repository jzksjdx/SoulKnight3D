using UnityEngine;

namespace SoulKnight3D
{
    public static class EnemyRewardDropSystem
    {
        public const int GoldIndex = 0;
        public const int SilverIndex = 1;
        public const int CopperIndex = 2;
        public const int EnergyIndex = 3;
        public const int RewardValueCount = 4;

        public static void Drop(Vector3 position, int rewardRate, int[] rewardValues)
        {
            if (rewardValues == null || rewardValues.Length < RewardValueCount ||
                GameObjectsManager.Instance == null)
            {
                return;
            }

            int clampedRate = Mathf.Clamp(rewardRate, 0, 100);

            // Soul Knight 1.8.4 rolls energy and coins independently using the same rate.
            int energyRoll = Random.Range(0, 100);
            int coinRoll = Random.Range(0, 100);

            if (energyRoll < clampedRate)
            {
                SpawnEnergy(position, Mathf.Max(0, rewardValues[EnergyIndex]));
            }

            if (coinRoll < clampedRate)
            {
                SpawnCoins(position, CoinPickup.CoinType.Gold,
                    Mathf.Max(0, rewardValues[GoldIndex]));
                SpawnCoins(position, CoinPickup.CoinType.Silver,
                    Mathf.Max(0, rewardValues[SilverIndex]));
                SpawnCoins(position, CoinPickup.CoinType.Copper,
                    Mathf.Max(0, rewardValues[CopperIndex]));
            }
        }

        public static void DropEnergy(Vector3 position, int count)
        {
            if (GameObjectsManager.Instance == null) { return; }

            for (int i = 0; i < count; i++)
            {
                Launch(GameObjectsManager.Instance.SpawnEnergyOrb(position));
            }
        }

        public static void DropCoins(Vector3 position, CoinPickup.CoinType type,
            int count)
        {
            if (GameObjectsManager.Instance == null) { return; }

            SpawnCoins(position, type, Mathf.Max(0, count));
        }

        private static void SpawnEnergy(Vector3 position, int count)
        {
            DropEnergy(position, count);
        }

        private static void SpawnCoins(Vector3 position, CoinPickup.CoinType type, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Launch(GameObjectsManager.Instance.SpawnCoin(position, type));
            }
        }

        private static void Launch(GameObject drop)
        {
            if (drop == null || !drop.TryGetComponent(out Rigidbody dropRigidbody))
            {
                return;
            }

            const float horizontalSpread = 0.5f;
            Vector3 direction = new Vector3(
                Random.Range(-horizontalSpread, horizontalSpread),
                0.5f,
                Random.Range(-horizontalSpread, horizontalSpread));
            dropRigidbody.AddForce(direction * 5f, ForceMode.Impulse);
        }
    }
}
