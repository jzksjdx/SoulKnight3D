using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using Random = UnityEngine.Random;

namespace SoulKnight3D
{
    public class Chest : InteractiveItem
    {
        private Animator _animator;
        private int _animIdOpen;

        public ChestRewardData ChestReward;

        // for weapon reward
        private GameObject _chestItem;
        private Vector3 _startPos;
        private Vector3 _finalPos;
        private Vector3 _startScale = new Vector3(0, 0, 0);
        private float _lerpTimeout = 1f;
        private float _lerpTimeoutDelta;

        // reward rate calculation
        private ChestRewardData.ChestRewardType _selectedType;
        private int _rewardTypeIndex = 0;
        private int _rewardItemIndex = 0;

        protected override void Start()
        {
            base.Start();
            _animator = GetComponent<Animator>();
            _animIdOpen = Animator.StringToHash("Open");

            string chestLabelText = _languageSystem.CurrentLanguage == LanguageSystem.Languages.Chinese ? "宝箱" : "Chest";
            Label.SetLabelText(chestLabelText, WeaponData.WeaponRarity.White);

            SelectReward();
            // for weapon reward
            _startPos = transform.position;
            _finalPos = _startPos + new Vector3(0, 0.4f, 0);
        }

        public override void Interact()
        {
            if (!IsInteractable) { return; }

            SetInteractable(false);
            AudioKit.PlaySound("fx_chest_open");
            _animator.SetTrigger(_animIdOpen);

            switch(_selectedType)
            {
                case ChestRewardData.ChestRewardType.EnergyAndCoin:
                    for (int i = 0; i < 4; i++)
                    {
                        GameObject newOrb = GameObjectsManager.Instance.SpawnEnergyOrb(transform.position);
                        Rigidbody rb = newOrb.GetComponent<Rigidbody>();
                        float randomScale = 0.3f;
                        Vector3 randomDirection = Vector3.up + new Vector3(Random.Range(-randomScale, randomScale), 0f, Random.Range(-randomScale, randomScale));
                        rb.AddForce(randomDirection * 5, ForceMode.Impulse);
                    }
                    break;
                case ChestRewardData.ChestRewardType.Potion:
                    GameObject potionReward = GetSelectedRewardItem();
                    if (potionReward != null)
                    {
                        Instantiate(potionReward, transform);
                    }
                    break;
                case ChestRewardData.ChestRewardType.Weapon:
                    GameObject weaponReward = GetSelectedRewardItem();
                    if (weaponReward != null)
                    {
                        _chestItem = Instantiate(weaponReward, transform);
                        _chestItem.GetComponent<PickupWeapon>().SelfRigidBody.isKinematic = true;
                        _lerpTimeoutDelta = _lerpTimeout;
                    }
                    break;
            }
        }

        private void Update()
        {
            if (_chestItem && _lerpTimeoutDelta >= 0f)
            {
                _lerpTimeoutDelta -= Time.deltaTime;
                float percent = (_lerpTimeout - _lerpTimeoutDelta) / _lerpTimeout;
                _chestItem.transform.position = Vector3.Lerp(_startPos, _finalPos, percent);
                _chestItem.transform.localScale = Vector3.Lerp(_startScale, Vector3.one, percent);
            }
        }

        private void SelectReward()
        {
            if (ChestReward == null || ChestReward.ChestRewards.Count == 0) { return; }

            _rewardTypeIndex = SelectRewardCategoryIndex();
            _selectedType = ChestReward.ChestRewards[_rewardTypeIndex].Type;

            List<ChestRewardData.RewardItem> rewardItems = ChestReward.ChestRewards[_rewardTypeIndex].Items;
            _rewardItemIndex = rewardItems.Count == 0 ? 0 : SelectRewardItemIndex(rewardItems);
        }

        private int SelectRewardCategoryIndex()
        {
            float totalRate = 0f;
            foreach (ChestRewardData.RewardCategory category in ChestReward.ChestRewards)
            {
                totalRate += Mathf.Max(0f, category.Rate);
            }

            if (totalRate <= 0f) { return 0; }

            float randomRate = Random.Range(0f, totalRate);
            float currentRate = 0f;
            for (int i = 0; i < ChestReward.ChestRewards.Count; i++)
            {
                currentRate += Mathf.Max(0f, ChestReward.ChestRewards[i].Rate);
                if (currentRate >= randomRate)
                {
                    return i;
                }
            }

            return ChestReward.ChestRewards.Count - 1;
        }

        private int SelectRewardItemIndex(List<ChestRewardData.RewardItem> rewardItems)
        {
            float totalRate = 0f;
            foreach (ChestRewardData.RewardItem item in rewardItems)
            {
                totalRate += Mathf.Max(0f, item.Rate);
            }

            if (totalRate <= 0f) { return 0; }

            float randomRate = Random.Range(0f, totalRate);
            float currentRate = 0f;
            for (int i = 0; i < rewardItems.Count; i++)
            {
                currentRate += Mathf.Max(0f, rewardItems[i].Rate);
                if (currentRate >= randomRate)
                {
                    return i;
                }
            }

            return rewardItems.Count - 1;
        }

        private GameObject GetSelectedRewardItem()
        {
            if (ChestReward == null || ChestReward.ChestRewards.Count <= _rewardTypeIndex) { return null; }

            List<ChestRewardData.RewardItem> rewardItems = ChestReward.ChestRewards[_rewardTypeIndex].Items;
            if (rewardItems.Count <= _rewardItemIndex) { return null; }

            return rewardItems[_rewardItemIndex].Item;
        }


    }
}
