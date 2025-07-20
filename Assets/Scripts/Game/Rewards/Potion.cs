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

        private GameObject _particle;

        protected override void Start()
        {
            base.Start();
            Label.SetLabelText("药水", WeaponData.WeaponRarity.White);
            //Outline.OutlineColor = GetRarityColor();
        }

        public override void Interact()
        {
            PlayerStats stats = PlayerController.Instance.PlayerStats;
            stats.RecoverEnergy(_recoverEnergyValue);
            stats.RecoverHealth(_recoverHealValue);
            AudioKit.PlaySound("fx_healthpot");
            _particle = Instantiate(_particlePrefab, PlayerController.Instance.transform);
            Destroy(gameObject);
        }
    }
}
