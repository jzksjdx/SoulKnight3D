using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using System;
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

            // calculate rewards, determine reward category
            //Random.InitState((int)DateTime.Now.Ticks);
            float totalRewardTypeRate = 0f;
            foreach (ChestRewardData.RewardCategory category in ChestReward.ChestRewards)
            {
                totalRewardTypeRate += category.Rate;
            }
            float randomRewardTypeRate = Random.Range(0f, totalRewardTypeRate);
            float currRewardTypeRate = 0f;
            for (int i = 0; i < ChestReward.ChestRewards.Count; i++)
            {
                currRewardTypeRate += ChestReward.ChestRewards[i].Rate;
                if (currRewardTypeRate >= randomRewardTypeRate)
                {
                    _rewardTypeIndex = i;
                    _selectedType = ChestReward.ChestRewards[i].Type;
                    break;
                }
            }
            // determine reward item
            float totalRewardItemRate = 0f;
            foreach(ChestRewardData.RewardItem item in ChestReward.ChestRewards[_rewardTypeIndex].Items)
            {
                totalRewardItemRate += item.Rate;
            }
            float randomRewardItemRate = Random.Range(0f, totalRewardItemRate);
            float currRewardItemRate = 0f;
            for (int j = 0; j < ChestReward.ChestRewards[_rewardTypeIndex].Items.Count; j++)
            {
                currRewardItemRate += ChestReward.ChestRewards[_rewardTypeIndex].Items[j].Rate;
                if (currRewardItemRate >= randomRewardItemRate)
                {
                    _rewardItemIndex = j;
                    break;
                }
            }
            // for weapon reward
            _startPos = transform.position;
            _finalPos = _startPos + new Vector3(0, 0.4f, 0);
        }

        public override void Interact()
        {
            InteractCollider.enabled = false;
            AudioKit.PlaySound("fx_chest_open");
            _animator.SetTrigger(_animIdOpen);

            switch(_selectedType)
            {
                case ChestRewardData.ChestRewardType.EnergyAndCoin:
                    for (int i = 0; i <= 3; i++)
                    {
                        GameObject newOrb = GameObjectsManager.Instance.SpawnEnergyOrb(transform.position);
                        Rigidbody rb = newOrb.GetComponent<Rigidbody>();
                        float randomScale = 0.3f;
                        Vector3 randomDirection = Vector3.up + new Vector3(Random.Range(-randomScale, randomScale), 0f, Random.Range(-randomScale, randomScale));
                        rb.AddForce(randomDirection * 5, ForceMode.Impulse);
                    }
                    break;
                case ChestRewardData.ChestRewardType.Potion:
                    Instantiate(ChestReward.ChestRewards[_rewardTypeIndex].Items[_rewardItemIndex].Item, transform);
                    break;
                case ChestRewardData.ChestRewardType.Weapon:
                    Random.InitState((int)DateTime.Now.Ticks);
                    _chestItem = Instantiate(ChestReward.ChestRewards[_rewardTypeIndex].Items[_rewardItemIndex].Item, transform);
                    _chestItem.GetComponent<PickupWeapon>().SelfRigidBody.isKinematic = true;
                    _lerpTimeoutDelta = _lerpTimeout;
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


    }
}
