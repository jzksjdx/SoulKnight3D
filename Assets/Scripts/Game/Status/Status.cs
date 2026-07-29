using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class Status : MonoBehaviour, IPoolable
    {
        public enum StatusType
        {
            SpeedUp, SpeedDown, Poison, Restrained
        }

        public StatusType Type;
        [SerializeField] protected float _duration;
        protected TargetableObject _target;
        [HideInInspector] public GameObject PrefabRef;

        private bool _isActive;
        private float _durationTimer;

        protected virtual bool Expires => true;

        private void Update()
        {
            if (!_isActive) { return; }

            if (Expires)
            {
                _durationTimer -= Time.deltaTime;
            }
            OnStatusTick(Time.deltaTime);

            if (Expires && _durationTimer <= 0f)
            {
                HandleDespawn();
            }
        }

        public virtual bool ActivateStatus(TargetableObject target)
        {
            // called in gameobject manager when spawning new status
            if (target == null) { return false; }
            if (target.Statuses.Contains(Type))
            {
                return false;
            }

            _target = target;
            _target.Statuses.Add(Type);
            transform.parent = target.transform;
            transform.localPosition = Vector3.zero;
            _durationTimer = _duration;
            _isActive = true;
            gameObject.Show();

            OnStatusApplied();
            return true;
        }

        protected virtual void HandleDespawn()
        {
            DeactivateStatus();
            if (GameObjectsManager.Instance != null)
            {
                GameObjectsManager.Instance.DespawnStatus(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void RemoveStatus()
        {
            if (!_isActive) { return; }
            HandleDespawn();
        }

        public void Reset()
        {
            DeactivateStatus();
            gameObject.Hide();
            if (GameObjectsManager.Instance != null)
            {
                transform.parent = GameObjectsManager.Instance.transform;
            }
        }

        protected virtual void OnStatusApplied() { }

        protected virtual void OnStatusRemoved() { }

        protected virtual void OnStatusTick(float deltaTime) { }

        private void DeactivateStatus()
        {
            if (!_isActive) { return; }

            OnStatusRemoved();

            if (_target != null)
            {
                _target.Statuses.Remove(Type);
            }

            _target = null;
            _isActive = false;
            _durationTimer = 0f;
        }
    }

}
