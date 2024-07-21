using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public class BackpackSlot : UISlot
    {
        public override void UpdateView()
        {
            base.UpdateView();

            // Handle Manual drag to backpack
            if (Data.Item == null) { return; }
            PlayRandomSound();
            if (Data.Item.State != ItemPlant.StorageState.Backpack)
            {
                Data.Item.State = ItemPlant.StorageState.Backpack;
                Data.Item.PickUpItem();
                Data.Item.MoveToBackpack();
            }
        }

        private void PlayRandomSound()
        {
            List<string> soundNames = new List<string>() { "plant", "plant2" };
            AudioKit.PlaySound(soundNames[Random.Range(0, soundNames.Count)]);
        }
    }

}
