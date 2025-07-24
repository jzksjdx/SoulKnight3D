using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class StatusZone : MonoBehaviour, IPoolable
    {
        public Status.StatusType Type;
        [SerializeField] protected GameObject _statusPrefab;
        [SerializeField] protected Collider _collider;
        [SerializeField] protected float _duration;
        private float _durationTimer = 0f;

        [HideInInspector] public GameObject PrefabRef; // for pool only, used as a dictionary key

        protected virtual void Start()
        {
            _collider.OnTriggerStayEvent((other) =>
            {
                if (!other.TryGetComponent(out TargetableObject target)) { return; }
                if (target.Statuses.Contains(Type)) { return; }
                GameObjectsManager.Instance.SpawnStatus(_statusPrefab, target);
            }).UnRegisterWhenGameObjectDestroyed(this);
        }

        // Update is called once per frame
        void Update()
        {
            if (_durationTimer >= 0f)
            {
                _durationTimer -= Time.deltaTime;
                if (_durationTimer <= 0f)
                {
                    // despawn self
                    GameObjectsManager.Instance.DespawnStatusZone(this);
                }
            }
        }

        public virtual void Reset()
        {
            gameObject.Hide();
        }

        public virtual void ActivateStatusZone(Vector3 position)
        {
            gameObject.Show();
            _durationTimer = _duration;
            transform.position = position;
        }

    }

}
