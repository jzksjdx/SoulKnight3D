using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public class BoostTile : MonoBehaviour
    {
        public GameObject SpeedBuffPrefab;
        private BoxCollider _collider;
        // Start is called before the first frame update
        void Start()
        {
            _collider = GetComponent<BoxCollider>();

            _collider.OnTriggerEnterEvent((other) =>
            {
                if (other.TryGetComponent(out TargetableObject targetable))
                {
                    if (targetable.GetComponentInChildren<SpeedBuff>()) { return; }
                    GameObject newBuff = Instantiate(SpeedBuffPrefab, targetable.transform)
                        .Position(targetable.transform.position);
                    newBuff.GetComponent<SpeedBuff>().ActivateBuff(targetable);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
    }
}

