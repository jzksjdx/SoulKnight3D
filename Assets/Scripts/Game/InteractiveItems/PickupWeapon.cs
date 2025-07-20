using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
	public class PickupWeapon : InteractiveItem
	{
        public Rigidbody SelfRigidBody;
        public WeaponData WeaponData;
		public Transform WeaponModel;
        [SerializeField] private Outline Outline;

		public override void Interact()
		{
			PlayerAttack playerAtk = PlayerController.Instance.PlayerAttack;
			if (!playerAtk) { return; }

            Transform weaponTransform;
            WeaponData.WeaponAnimation weaponAnimation = WeaponData.Animation;
            if (weaponAnimation == WeaponData.WeaponAnimation.Bow)
            {
                weaponTransform = playerAtk.LeftWeaponPoint;
            } else
            {
                weaponTransform = playerAtk.WeaponPoint;
            }
            GameObject newWeapon = Instantiate(WeaponData.WeaponPrefab, weaponTransform.position, Quaternion.identity, weaponTransform);
            newWeapon.transform.rotation = new Quaternion(0, 0, 0, 0);
            playerAtk.TakeNewWeapon(newWeapon);
            Destroy(gameObject);
        }

        protected override void Start()
        {
            base.Start();
            
            string WeaponLabelText = _languageSystem.CurrentLanguage == LanguageSystem.Languages.Chinese ? WeaponData.NameCN : WeaponData.Name;
            Label.SetLabelText(WeaponLabelText, WeaponData.Rarity);
            Outline.OutlineColor = GetRarityColor();
        }

        private void Update()
        {
            WeaponModel.Rotate(Vector3.up * 0.3f);
        }

        private Color GetRarityColor()
        {
            switch(WeaponData.Rarity)
            {
                case WeaponData.WeaponRarity.White:
                    return Color.white;
                case WeaponData.WeaponRarity.Green:
                    return new Color(61f / 255f, 226f / 255f, 90f / 255f);
                case WeaponData.WeaponRarity.Blue:
                    return new Color(14f / 255f, 165f / 255f, 255f / 255f);
                case WeaponData.WeaponRarity.Magenta:
                    return new Color(14f / 255f, 165f / 255f, 255f / 255f);
                case WeaponData.WeaponRarity.Purple:
                    return new Color(190f / 255f, 7f / 255f, 201f / 255f);
                case WeaponData.WeaponRarity.Orange:
                    return new Color(255f / 255f, 143f / 255f, 1f / 255f);
                case WeaponData.WeaponRarity.Red:
                    return new Color(255f / 255f, 26f / 255f, 26f / 255f);
                default:
                    return Color.white;
            }
        }
    }
}
