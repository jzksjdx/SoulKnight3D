using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;
using QFramework;

namespace SoulKnight3D
{
    public class DungeonChest : Chest
    {
        public List<GameObject> WeaponPool;

        private GameObject _chestItem;
        private Vector3 _startPos;
        private Vector3 _finalPos;
        private Vector3 _startScale = new Vector3(0, 0, 0);
        private float _lerpTimeout = 1f;
        private float _lerpTimeoutDelta;

        public override void Interact()
        {
            base.Interact();
            Random.InitState((int)DateTime.Now.Ticks);
            _chestItem = Instantiate(WeaponPool[Random.Range(0, WeaponPool.Count)], transform);
            _chestItem.GetComponent<PickupWeapon>().SelfRigidBody.isKinematic = true;
            _lerpTimeoutDelta = _lerpTimeout;
        }

        protected override void Start()
        {
            base.Start();

            _startPos = transform.position;
            _finalPos = _startPos + new Vector3(0, 0.4f, 0);
        }

        private void Update()
        {
            if (_chestItem && _lerpTimeoutDelta >= 0f)
            {
                _lerpTimeoutDelta -= Time.deltaTime;
                float percent = (_lerpTimeout - _lerpTimeoutDelta) / _lerpTimeout;
                _chestItem.transform.position = Vector3.Lerp(_startPos, _finalPos, percent);
                _chestItem.transform.localScale = Vector3.Lerp(_startScale, Vector3.one, percent);
            }
        }
    }
}

