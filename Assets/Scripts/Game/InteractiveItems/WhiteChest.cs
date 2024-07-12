using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    public class WhiteChest : Chest
    {
        public override void Interact()
        {
            base.Interact();

            for (int i = 0; i <= 3; i++)
            {
                GameObject newOrb = GameObjectsManager.Instance.SpawnEnergyOrb(transform.position);
                Rigidbody rb = newOrb.GetComponent<Rigidbody>();
                float randomScale = 0.3f;
                Vector3 randomDirection = Vector3.up + new Vector3(Random.Range(-randomScale, randomScale), 0f, Random.Range(-randomScale, randomScale));
                rb.AddForce(randomDirection * 5, ForceMode.Impulse);
            }
        }
    }
}

