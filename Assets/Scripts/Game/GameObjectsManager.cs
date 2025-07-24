using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D {
    public class GameObjectsManager : MonoBehaviour
    {
        public static GameObjectsManager Instance;

        public GameObject EnergyOrbPrefab;

        private SimpleObjectPool<GameObject> _energyOrbPool;

        private Dictionary<GameObject, SimpleObjectPool<GameObject>> _bulletPools = new Dictionary<GameObject, SimpleObjectPool<GameObject>>();

        private Dictionary<GameObject, SimpleObjectPool<GameObject>> _statusZonePools = new Dictionary<GameObject, SimpleObjectPool<GameObject>>();

        private Dictionary<Status.StatusType, SimpleObjectPool<GameObject>> _statusPools = new Dictionary<Status.StatusType, SimpleObjectPool<GameObject>>();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        // Start is called before the first frame update
        void Start()
        {
            _energyOrbPool = new SimpleObjectPool<GameObject>(factoryMethod: () =>
            {
                GameObject newEnergyOrb = Instantiate(EnergyOrbPrefab, transform);
                newEnergyOrb.Hide();
                return newEnergyOrb;
            },
            initCount: 5,
            resetMethod: (gameObject) =>
            {
                gameObject.GetComponent<EnergyOrb>().Reset();
            });

        }

        public GameObject SpawnEnergyOrb(Vector3 position)
        {
            GameObject newOrb = _energyOrbPool
                .Allocate()
                .Position(position)
                .Show();
            return newOrb;
        }

        public void DespawnEnergyOrb(GameObject gameObject)
        {
            _energyOrbPool.Recycle(gameObject);
        }

        // bullets
        public GameObject SpawnBullet(GameObject bulletPrefab)
        {
            if (_bulletPools.ContainsKey(bulletPrefab))
            {
                //Debug.Log("Allocate bullet");
                return _bulletPools[bulletPrefab].Allocate();
            } else
            {
                SimpleObjectPool<GameObject> newBulletPool = new SimpleObjectPool<GameObject>(factoryMethod: () =>
                {
                    return Instantiate(bulletPrefab, transform).Hide();
                }, initCount: 5,
                resetMethod: (gameObject) =>
                {
                    gameObject.GetComponent<Bullet>().Reset();
                });

                _bulletPools.Add(bulletPrefab, newBulletPool);
                return newBulletPool.Allocate();

            }
        }

        public void DespawnBullet(Bullet bullet)
        {
            if (_bulletPools.ContainsKey(bullet.PrefabRef))
            {
                _bulletPools[bullet.PrefabRef].Recycle(bullet.gameObject);
            } else
            {
                Destroy(bullet.gameObject);
            }
        }

        // status
        public GameObject SpawnStatus(GameObject statusPrefab, TargetableObject target)
        {
            Status.StatusType statusType = statusPrefab.GetComponent<Status>().Type;
            if (_statusPools.ContainsKey(statusType))
            {
                GameObject newStatus = _statusPools[statusType].Allocate();
                newStatus.GetComponent<Status>().ActivateStatus(target);
                return newStatus;
            } else
            {
                SimpleObjectPool<GameObject> newStatusPool = new SimpleObjectPool<GameObject>(factoryMethod: () =>
                {
                    return Instantiate(statusPrefab, transform).Hide();
                }, initCount: 5,
                resetMethod: (gameObject) =>
                {
                    gameObject.GetComponent<Status>().Reset();
                });

                _statusPools.Add(statusType, newStatusPool);

                GameObject newStatus = newStatusPool.Allocate();
                newStatus.GetComponent<Status>().ActivateStatus(target);
                return newStatus;
            }
        }

        public void DespawnStatus(Status status)
        {
            if (_statusPools.ContainsKey(status.Type))
            {
                _statusPools[status.Type].Recycle(status.gameObject);
            } else
            {
                Destroy(status.gameObject);
            }
        }

        // status zones
        public GameObject SpawnStatusZone(GameObject statusZonePrefab, Vector3 position)
        {
            if (_statusZonePools.ContainsKey(statusZonePrefab))
            {
                GameObject statusZone = _statusZonePools[statusZonePrefab].Allocate();
                statusZone.GetComponent<StatusZone>().ActivateStatusZone(position);
                return statusZone;
            }
            else
            {
                SimpleObjectPool<GameObject> newStatusZonePool = new SimpleObjectPool<GameObject>(factoryMethod: () =>
                {
                    GameObject newStatusZone = Instantiate(statusZonePrefab, transform).Hide();
                    newStatusZone.GetComponent<StatusZone>().PrefabRef = statusZonePrefab;
                    return newStatusZone;
                }, initCount: 5,
                resetMethod: (gameObject) =>
                {
                    gameObject.GetComponent<StatusZone>().Reset();
                });

                _statusZonePools.Add(statusZonePrefab, newStatusZonePool);

                GameObject statusZone = newStatusZonePool.Allocate();
                statusZone.GetComponent<StatusZone>().ActivateStatusZone(position);
                return statusZone;

            }
        }

        public void DespawnStatusZone(StatusZone statusZone)
        {
            if (_statusZonePools.ContainsKey(statusZone.PrefabRef))
            {
                _statusZonePools[statusZone.PrefabRef].Recycle(statusZone.gameObject);
            }
            else
            {
                Destroy(statusZone.gameObject);
            }
        }
    }
}

