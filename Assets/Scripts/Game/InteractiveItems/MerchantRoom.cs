using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    public class MerchantRoom : MonoBehaviour
    {
        [SerializeField] private List<Transform> _stockPoints = new List<Transform>();
        [SerializeField] private WeaponDropPoolSO _weaponPool;
        [SerializeField] private List<GameObject> _potionPrefabs = new List<GameObject>();
        [SerializeField] private GameObject _priceLabelPrefab;
        [SerializeField, Min(0f)] private float _potionStockYOffset = 0.2f;
        [SerializeField, Min(1)] private int _level = 1;
        [SerializeField, Min(0f)] private float _priceIncreasePerLevel = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _twoPotionStockChance = 0.5f;

        [Header("Merchant Animation")]
        [SerializeField] private Animator _merchantAnimator;
        [SerializeField, Min(0f)] private float _playerProximityDistance = 2.5f;
        [SerializeField] private Vector2 _talkIntervalRange = new Vector2(4f, 9f);

        private static readonly int IdleState = Animator.StringToHash("Idle");
        private static readonly int TalkingState = Animator.StringToHash("Talking");
        private static readonly int ThankfulState = Animator.StringToHash("Thankful");
        private static readonly int HandGestureState = Animator.StringToHash("HandGesture");
        private static readonly int TalkTrigger = Animator.StringToHash("Talk");
        private static readonly int ThankfulTrigger = Animator.StringToHash("Thankful");
        private static readonly int HandGestureTrigger = Animator.StringToHash("HandGesture");

        private bool _isPopulated;
        private PlayerController _player;
        private bool _playerNearby;
        private bool _waitingForOneShot;
        private bool _oneShotStarted;
        private int _awaitedState;
        private float _nextTalkTime;

        public List<Transform> StockPoints => _stockPoints;
        public WeaponDropPoolSO WeaponPool
        {
            get => _weaponPool;
            set => _weaponPool = value;
        }
        public List<GameObject> PotionPrefabs => _potionPrefabs;
        public GameObject PriceLabelPrefab
        {
            get => _priceLabelPrefab;
            set => _priceLabelPrefab = value;
        }
        public float PotionStockYOffset
        {
            get => _potionStockYOffset;
            set => _potionStockYOffset = Mathf.Max(0f, value);
        }
        public float PriceIncreasePerLevel
        {
            get => _priceIncreasePerLevel;
            set => _priceIncreasePerLevel = Mathf.Max(0f, value);
        }

        public void Configure(WeaponDropPoolSO weaponPool, IReadOnlyList<GameObject> potionPrefabs,
            int level, float priceIncreasePerLevel, GameObject priceLabelPrefab,
            float potionStockYOffset)
        {
            _weaponPool = weaponPool;
            _level = Mathf.Max(1, level);
            _priceIncreasePerLevel = Mathf.Max(0f, priceIncreasePerLevel);
            _priceLabelPrefab = priceLabelPrefab;
            _potionStockYOffset = Mathf.Max(0f, potionStockYOffset);
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
            InitializeAnimation();
            PopulateStock();
        }

        private void Update()
        {
            UpdateAnimation();
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

            _stockPoints.Sort((left, right) =>
                transform.InverseTransformPoint(left.position).x.CompareTo(
                    transform.InverseTransformPoint(right.position).x));
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
                MerchantStockItem stockItem = MerchantStockItem.Create(
                    _stockPoints[i], itemPrefab, price, _priceLabelPrefab, _potionStockYOffset);
                stockItem.Purchased += PlayPurchaseReaction;
                Debug.Log($"Merchant stock {i + 1}: '{itemPrefab.name}' " +
                    $"for {price} coins (seed {GameRandom.LevelSeed}).");
            }
        }

        private void InitializeAnimation()
        {
            if (_merchantAnimator == null)
            {
                _merchantAnimator = GetComponentInChildren<Animator>(true);
            }
            if (_merchantAnimator == null || _merchantAnimator.runtimeAnimatorController == null)
            {
                return;
            }

            ScheduleNextTalk();
        }

        private void UpdateAnimation()
        {
            if (_merchantAnimator == null || _merchantAnimator.runtimeAnimatorController == null)
            {
                return;
            }

            if (_player == null)
            {
                _player = PlayerController.Instance;
                if (_player == null) { return; }
            }

            Vector3 playerOffset = _player.transform.position - _merchantAnimator.transform.position;
            playerOffset.y = 0f;
            bool isNearby = playerOffset.sqrMagnitude <= _playerProximityDistance * _playerProximityDistance;

            UpdateOneShotState();

            if (!_waitingForOneShot && isNearby && !_playerNearby)
            {
                RequestOneShot(TalkTrigger, TalkingState);
            }
            else if (!_waitingForOneShot && isNearby && Time.time >= _nextTalkTime)
            {
                RequestOneShot(TalkTrigger, TalkingState);
            }

            _playerNearby = isNearby;
        }

        private void PlayPurchaseReaction()
        {
            bool isThankful = GameRandom.Chance(
                GameRandomStream.Presentation, 0.5f);
            RequestOneShot(
                isThankful ? ThankfulTrigger : HandGestureTrigger,
                isThankful ? ThankfulState : HandGestureState);
        }

        private void RequestOneShot(int triggerHash, int stateHash)
        {
            ResetAnimationTriggers();
            _merchantAnimator.SetTrigger(triggerHash);
            _awaitedState = stateHash;
            _waitingForOneShot = true;
            _oneShotStarted = false;
        }

        private void UpdateOneShotState()
        {
            if (!_waitingForOneShot) { return; }

            AnimatorStateInfo currentState = _merchantAnimator.GetCurrentAnimatorStateInfo(0);
            AnimatorStateInfo nextState = _merchantAnimator.GetNextAnimatorStateInfo(0);
            if (currentState.shortNameHash == _awaitedState
                || (_merchantAnimator.IsInTransition(0) && nextState.shortNameHash == _awaitedState))
            {
                _oneShotStarted = true;
            }

            if (_oneShotStarted
                && currentState.shortNameHash == IdleState
                && !_merchantAnimator.IsInTransition(0))
            {
                _waitingForOneShot = false;
                _oneShotStarted = false;
                ScheduleNextTalk();
            }
        }

        private void ResetAnimationTriggers()
        {
            _merchantAnimator.ResetTrigger(TalkTrigger);
            _merchantAnimator.ResetTrigger(ThankfulTrigger);
            _merchantAnimator.ResetTrigger(HandGestureTrigger);
        }

        private void ScheduleNextTalk()
        {
            float minimum = Mathf.Max(0f, _talkIntervalRange.x);
            float maximum = Mathf.Max(minimum, _talkIntervalRange.y);
            _nextTalkTime = Time.time + GameRandom.Range(
                GameRandomStream.Presentation, minimum, maximum);
        }

        private HashSet<int> SelectPotionSlots()
        {
            HashSet<int> potionSlots = new HashSet<int>();
            if (_potionPrefabs.Count == 0 || _stockPoints.Count < 2)
            {
                return potionSlots;
            }

            int potionCount = _stockPoints.Count >= 3 &&
                GameRandom.Chance(GameRandomStream.Merchant, _twoPotionStockChance)
                ? 2
                : 1;
            potionCount = Mathf.Min(potionCount, _stockPoints.Count - 1);
            potionSlots.Add(_stockPoints.Count / 2);

            while (potionSlots.Count < potionCount)
            {
                potionSlots.Add(GameRandom.Range(
                    GameRandomStream.Merchant, 0, _stockPoints.Count));
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

            int poolLevel = GetCurrentWeaponPoolLevel();
            GameObject selected = null;
            System.Random stockRandom = GameRandom.GetStream(
                GameRandomStream.Merchant);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                selected = _weaponPool.GetRandomPickupPrefabAtOrBelow(
                    poolLevel, stockRandom);
                if (selected == null || selected != excludedPrefab)
                {
                    break;
                }
            }
            return selected;
        }

        private int GetCurrentWeaponPoolLevel()
        {
            GameController gameController = GameController.Instance;
            if (gameController == null || gameController.GameFloor == null)
            {
                return _level;
            }

            return gameController.GameFloor.GetWeaponPoolLevel(_level);
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

            GameObject selectedPotion = candidates[GameRandom.Range(
                GameRandomStream.Merchant, 0, candidates.Count)];
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
