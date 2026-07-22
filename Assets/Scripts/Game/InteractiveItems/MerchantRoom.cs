using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SoulKnight3D
{
    public class MerchantRoom : MonoBehaviour
    {
        [SerializeField] private List<Transform> _stockPoints = new List<Transform>();
        [SerializeField] private WeaponDropPoolSO _weaponPool;
        [SerializeField] private List<GameObject> _potionPrefabs = new List<GameObject>();
        [SerializeField, Min(1)] private int _level = 1;
        [SerializeField, Min(0f)] private float _priceIncreasePerLevel = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _twoPotionStockChance = 0.5f;

        private static readonly System.Random StockRandom = new System.Random(
            unchecked(Environment.TickCount ^ (int)DateTime.UtcNow.Ticks));
        private bool _isPopulated;

        public List<Transform> StockPoints => _stockPoints;
        public WeaponDropPoolSO WeaponPool
        {
            get => _weaponPool;
            set => _weaponPool = value;
        }
        public List<GameObject> PotionPrefabs => _potionPrefabs;
        public float PriceIncreasePerLevel
        {
            get => _priceIncreasePerLevel;
            set => _priceIncreasePerLevel = Mathf.Max(0f, value);
        }

        public void Configure(WeaponDropPoolSO weaponPool, IReadOnlyList<GameObject> potionPrefabs,
            int level, float priceIncreasePerLevel)
        {
            _weaponPool = weaponPool;
            _level = Mathf.Max(1, level);
            _priceIncreasePerLevel = Mathf.Max(0f, priceIncreasePerLevel);
            _potionPrefabs.Clear();
            if (potionPrefabs != null)
            {
                for (int i = 0; i < potionPrefabs.Count; i++)
                {
                    if (potionPrefabs[i] != null)
                    {
                        _potionPrefabs.Add(potionPrefabs[i]);
                    }
                }
            }
        }

        private void Start()
        {
            PopulateStock();
        }

        private void PopulateStock()
        {
            if (_isPopulated) { return; }
            _isPopulated = true;

            FindStockPointsIfNeeded();
            if (_stockPoints.Count == 0)
            {
                Debug.LogWarning($"Merchant layout '{name}' has no StockPoint transforms.");
                return;
            }

            _stockPoints.Sort((left, right) => left.position.x.CompareTo(right.position.x));
            GameObject previousWeapon = null;
            HashSet<GameObject> selectedPotions = new HashSet<GameObject>();
            HashSet<int> potionSlots = SelectPotionSlots();

            for (int i = 0; i < _stockPoints.Count; i++)
            {
                bool shouldSpawnPotion = potionSlots.Contains(i);
                GameObject itemPrefab = shouldSpawnPotion
                    ? GetRandomPotion(selectedPotions)
                    : GetRandomWeapon(previousWeapon);

                if (itemPrefab == null)
                {
                    itemPrefab = shouldSpawnPotion
                        ? GetRandomWeapon(previousWeapon)
                        : GetRandomPotion(selectedPotions);
                }
                if (itemPrefab == null) { continue; }

                if (itemPrefab.TryGetComponent(out PickupWeapon _))
                {
                    previousWeapon = itemPrefab;
                }

                int basePrice = GetBasePrice(itemPrefab);
                int price = CalculatePrice(basePrice, _level, _priceIncreasePerLevel);
                MerchantStockItem.Create(_stockPoints[i], itemPrefab, price);
            }
        }

        private HashSet<int> SelectPotionSlots()
        {
            HashSet<int> potionSlots = new HashSet<int>();
            if (_potionPrefabs.Count == 0 || _stockPoints.Count < 2)
            {
                return potionSlots;
            }

            int potionCount = _stockPoints.Count >= 3 && Random.value < _twoPotionStockChance
                ? 2
                : 1;
            potionCount = Mathf.Min(potionCount, _stockPoints.Count - 1);
            potionSlots.Add(_stockPoints.Count / 2);

            while (potionSlots.Count < potionCount)
            {
                potionSlots.Add(Random.Range(0, _stockPoints.Count));
            }
            return potionSlots;
        }

        private void FindStockPointsIfNeeded()
        {
            _stockPoints.RemoveAll(point => point == null);
            if (_stockPoints.Count > 0) { return; }

            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == "StockPoint")
                {
                    _stockPoints.Add(transforms[i]);
                }
            }
        }

        private GameObject GetRandomWeapon(GameObject excludedPrefab)
        {
            if (_weaponPool == null) { return null; }

            GameObject selected = null;
            lock (StockRandom)
            {
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    selected = _weaponPool.GetRandomPickupPrefab(_level, StockRandom);
                    if (selected == null || selected != excludedPrefab)
                    {
                        break;
                    }
                }
            }
            return selected;
        }

        private GameObject GetRandomPotion(HashSet<GameObject> selectedPotions)
        {
            List<GameObject> candidates = new List<GameObject>();
            for (int i = 0; i < _potionPrefabs.Count; i++)
            {
                GameObject potionPrefab = _potionPrefabs[i];
                if (potionPrefab != null && !selectedPotions.Contains(potionPrefab)
                    && !candidates.Contains(potionPrefab))
                {
                    candidates.Add(potionPrefab);
                }
            }

            if (candidates.Count == 0) { return null; }

            GameObject selectedPotion = candidates[Random.Range(0, candidates.Count)];
            selectedPotions.Add(selectedPotion);
            return selectedPotion;
        }

        private static int GetBasePrice(GameObject itemPrefab)
        {
            if (itemPrefab.TryGetComponent(out PickupWeapon pickupWeapon)
                && pickupWeapon.WeaponData != null)
            {
                return Mathf.Max(1, pickupWeapon.WeaponData.Price);
            }

            if (itemPrefab.TryGetComponent(out Potion potion))
            {
                return Mathf.Max(1, potion.BasePrice);
            }

            return 1;
        }

        public static int CalculatePrice(int basePrice, int level, float increasePerLevel)
        {
            float multiplier = 1f + Mathf.Max(0, level - 1) * Mathf.Max(0f, increasePerLevel);
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, basePrice) * multiplier));
        }
    }
}
