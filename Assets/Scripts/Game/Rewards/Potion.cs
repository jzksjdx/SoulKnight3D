using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class Potion : InteractiveItem
    {
        [SerializeField] private GameObject _particlePrefab;
        [SerializeField] private int _recoverHealValue;
        [SerializeField] private int _recoverEnergyValue;
        [SerializeField] private GameObject _mesh;
        [SerializeField, Min(1)] private int _basePrice = 25;
        [SerializeField] private string _displayName = "Health Potion";
        [SerializeField] private string _displayNameCN = "生命药水";

        private GameObject _particle;

        public int BasePrice => _basePrice;
        public string DisplayName => _displayName;
        public string DisplayNameCN => _displayNameCN;

        protected override void Start()
        {
            base.Start();
            string labelText = _languageSystem.CurrentLanguage == LanguageSystem.Languages.Chinese
                ? _displayNameCN
                : _displayName;
            Label.SetLabelText(labelText, WeaponData.WeaponRarity.White);
        }

        public override void Interact()
        {
            if (!IsInteractable) { return; }

            SetInteractable(false);
            PlayerStats stats = PlayerController.Instance.PlayerStats;
            stats.RecoverEnergy(_recoverEnergyValue);
            stats.RecoverHealth(_recoverHealValue);
            AudioKit.PlaySound("fx_healthpot");
            _particle = Instantiate(_particlePrefab, PlayerController.Instance.transform);
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        public void ConfigureMerchantData(string displayName, string displayNameCN, int basePrice)
        {
            _displayName = displayName;
            _displayNameCN = displayNameCN;
            _basePrice = Mathf.Max(1, basePrice);
        }
#endif
    }
}
