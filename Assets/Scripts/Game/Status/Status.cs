using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D
{
    public class Status : MonoBehaviour, IPoolable
    {
        public enum StatusType
        {
            SpeedUp, SpeedDown, Poison
        }

        public StatusType Type;
        [SerializeField] protected float _duration;
        protected TargetableObject _target;

        public virtual void ActivateStatus(TargetableObject target)
        {
            // called in gameobject manager when spawning new status
            if (target.Statuses.Contains(Type))
            {
                GameObjectsManager.Instance.DespawnStatus(this);
                return;
            }

            _target = target;
            _target.Statuses.Add(Type);
            transform.parent = target.transform;
            transform.localPosition = Vector3.zero;
            gameObject.Show();

            ActionKit.Delay(_duration, () =>
            {
                HandleDespawn();
            }).Start(this);
        }

        protected virtual void HandleDespawn()
        {
            _target.Statuses.Remove(Type);
            GameObjectsManager.Instance.DespawnStatus(this);
        }

        public void Reset()
        {
            gameObject.Hide();
            _target = null;
            transform.parent = GameObjectsManager.Instance.transform;
        }
    }

}
