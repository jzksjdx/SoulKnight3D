using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
	public class PickupWeapon : InteractiveItem
	{
		public GameObject WeaponPrefab;
		public Transform WeaponModel;

		public override void Interact()
		{
			PlayerAttack playerAtk = PlayerController.Instance.PlayerAttack;
			if (!playerAtk) { return; }

            GameObject newWeapon = Instantiate(WeaponPrefab, playerAtk.WeaponPoint.position, Quaternion.identity, playerAtk.WeaponPoint);
            newWeapon.transform.rotation = new Quaternion(0, 0, 0, 0);
            playerAtk.TakeNewWeapon(newWeapon);
            Destroy(gameObject);
        }

        protected override void Start()
        {
            base.Start();
            WeaponData weaponData = WeaponPrefab.GetComponent<Weapon>().Data;

            string WeaponLabelText = _languageSystem.CurrentLanguage == LanguageSystem.Languages.Chinese ? weaponData.NameCN : weaponData.Name;
            Label.SetLabelText(WeaponLabelText, weaponData.Rarity);
        }

        private void Update()
        {
            WeaponModel.Rotate(Vector3.up * 0.3f);
        }
    }
}
