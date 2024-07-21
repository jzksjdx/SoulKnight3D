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
            //playerAtk.TakeNewWeapon(newWeapon);
            Destroy(gameObject);
        }

        private void Start()
        {
            WeaponData weaponData = WeaponPrefab.GetComponent<Weapon>().Data;
            Label.SetLabelText(weaponData.NameCN, weaponData.Rarity);
        }

        private void Update()
        {
            WeaponModel.Rotate(Vector3.up * 0.3f);
        }
    }
}
