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
                    if (targetable.Statuses.Contains(Status.StatusType.SpeedUp) || targetable.Statuses.Contains(Status.StatusType.SpeedDown)) { return; }
                    GameObjectsManager.Instance.SpawnStatus(SpeedBuffPrefab, targetable);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }
    }
}

