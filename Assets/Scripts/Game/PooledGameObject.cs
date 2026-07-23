using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public sealed class PooledGameObject : MonoBehaviour, IPoolable
    {
        public GameObject PrefabRef { get; private set; }
        public bool IsReleased { get; private set; } = true;

        internal void Configure(GameObject prefabRef)
        {
            PrefabRef = prefabRef;
        }

        internal void MarkAllocated()
        {
            IsReleased = false;
        }

        public void ShowFromPool()
        {
            IsReleased = false;
            gameObject.Show();
        }

        public void ReleaseToPool()
        {
            if (IsReleased) { return; }

            if (GameObjectsManager.Instance != null)
            {
                GameObjectsManager.Instance.DespawnPooledObject(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Reset()
        {
            IsReleased = true;
            gameObject.Hide();
        }
    }
}
