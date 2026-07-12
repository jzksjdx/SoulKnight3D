using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public class PoisonZone : StatusZone
    {
        protected override void Start()
        {
            _collider.OnTriggerStayEvent((other) =>
            {
                if (!other.TryGetComponent(out TargetableObject target)) { return; }
                if (other.CompareTag("Enemy") == false) { return; } // only apply to enemy
                if (target.Statuses.Contains(Type)) { return; }
                GameObjectsManager.Instance.SpawnStatus(_statusPrefab, target);
            }).UnRegisterWhenGameObjectDestroyed(this);
        }

    }

}
