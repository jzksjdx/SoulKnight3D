using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D {
    public class GameObjectsManager : MonoBehaviour
    {
        public static GameObjectsManager Instance;

        public GameObject EnergyOrbPrefab;
        public List<GameObject> CoinPrefabs = new List<GameObject>();

        private SimpleObjectPool<EnergyOrb> _energyOrbPool;
        private readonly Dictionary<CoinPickup.CoinType, SimpleObjectPool<CoinPickup>> _coinPools =
            new Dictionary<CoinPickup.CoinType, SimpleObjectPool<CoinPickup>>();
        private readonly HashSet<CoinPickup.CoinType> _missingCoinPoolWarnings =
            new HashSet<CoinPickup.CoinType>();

        private readonly Dictionary<GameObject, SimpleObjectPool<Bullet>> _bulletPools = new Dictionary<GameObject, SimpleObjectPool<Bullet>>();
        private readonly Dictionary<GameObject, SimpleObjectPool<PooledGameObject>>
            _reusableObjectPools =
                new Dictionary<GameObject, SimpleObjectPool<PooledGameObject>>();

        private readonly Dictionary<GameObject, SimpleObjectPool<StatusZone>> _statusZonePools = new Dictionary<GameObject, SimpleObjectPool<StatusZone>>();

        private readonly Dictionary<Status.StatusType, SimpleObjectPool<Status>> _statusPools = new Dictionary<Status.StatusType, SimpleObjectPool<Status>>();
        private readonly Dictionary<GameObject, Status.StatusType> _statusTypeCache = new Dictionary<GameObject, Status.StatusType>();

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
            _energyOrbPool = new SimpleObjectPool<EnergyOrb>(factoryMethod: () =>
            {
                GameObject newEnergyOrb = Instantiate(EnergyOrbPrefab, transform);
                newEnergyOrb.Hide();
                return newEnergyOrb.GetComponent<EnergyOrb>();
            },
            initCount: 5,
            resetMethod: (energyOrb) =>
            {
                energyOrb.Reset();
            });

            InitializeCoinPools();

        }

        private void InitializeCoinPools()
        {
            foreach (GameObject coinPrefab in CoinPrefabs)
            {
                if (coinPrefab == null || !coinPrefab.TryGetComponent(out CoinPickup coinTemplate))
                {
                    continue;
                }

                CoinPickup.CoinType coinType = coinTemplate.Type;
                if (_coinPools.ContainsKey(coinType))
                {
                    Debug.LogWarning($"Multiple coin prefabs are configured for {coinType}. Using the first one.");
                    continue;
                }

                SimpleObjectPool<CoinPickup> pool = new SimpleObjectPool<CoinPickup>(factoryMethod: () =>
                {
                    GameObject coinObject = Instantiate(coinPrefab, transform).Hide();
                    return coinObject.GetComponent<CoinPickup>();
                }, initCount: 3,
                resetMethod: coin =>
                {
                    coin.Reset();
                });

                _coinPools.Add(coinType, pool);
            }
        }

        public GameObject SpawnEnergyOrb(Vector3 position)
        {
            EnergyOrb newOrb = _energyOrbPool.Allocate();
            newOrb.transform.position = position;
            newOrb.gameObject.Show();
            return newOrb.gameObject;
        }

        public void DespawnEnergyOrb(GameObject gameObject)
        {
            if (gameObject.TryGetComponent(out EnergyOrb energyOrb))
            {
                _energyOrbPool.Recycle(energyOrb);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public GameObject SpawnCoin(Vector3 position, CoinPickup.CoinType coinType)
        {
            if (!_coinPools.TryGetValue(coinType, out SimpleObjectPool<CoinPickup> coinPool))
            {
                if (_missingCoinPoolWarnings.Add(coinType))
                {
                    Debug.LogWarning($"No pooled coin prefab is configured for {coinType}.");
                }
                return null;
            }

            CoinPickup coin = coinPool.Allocate();
            coin.transform.SetPositionAndRotation(position, Quaternion.identity);
            coin.gameObject.Show();
            return coin.gameObject;
        }

        public void DespawnCoin(CoinPickup coin)
        {
            if (coin != null && _coinPools.TryGetValue(coin.Type,
                out SimpleObjectPool<CoinPickup> coinPool))
            {
                coinPool.Recycle(coin);
                return;
            }

            if (coin != null)
            {
                Destroy(coin.gameObject);
            }
        }

        // bullets
        public GameObject SpawnBullet(GameObject bulletPrefab)
        {
            if (_bulletPools.TryGetValue(bulletPrefab, out SimpleObjectPool<Bullet> bulletPool))
            {
                return bulletPool.Allocate().gameObject;
            }

            SimpleObjectPool<Bullet> newBulletPool = new SimpleObjectPool<Bullet>(factoryMethod: () =>
            {
                GameObject bulletObject = Instantiate(bulletPrefab, transform).Hide();
                return bulletObject.GetComponent<Bullet>();
            }, initCount: 5,
            resetMethod: (bullet) =>
            {
                bullet.Reset();
            });

            _bulletPools.Add(bulletPrefab, newBulletPool);
            return newBulletPool.Allocate().gameObject;
        }

        public void DespawnBullet(Bullet bullet)
        {
            if (_bulletPools.TryGetValue(bullet.PrefabRef, out SimpleObjectPool<Bullet> bulletPool))
            {
                bulletPool.Recycle(bullet);
            }
            else
            {
                Destroy(bullet.gameObject);
            }
        }

        // Reusable hazards and effects that do not use the Bullet component.
        public PooledGameObject SpawnPooledObject(GameObject prefab, Vector3 position,
            Quaternion rotation)
        {
            if (prefab == null) { return null; }

            if (!_reusableObjectPools.TryGetValue(
                prefab, out SimpleObjectPool<PooledGameObject> pool))
            {
                pool = new SimpleObjectPool<PooledGameObject>(factoryMethod: () =>
                {
                    GameObject instance = Instantiate(prefab, transform).Hide();
                    PooledGameObject pooledObject =
                        instance.GetComponent<PooledGameObject>() ??
                        instance.AddComponent<PooledGameObject>();
                    pooledObject.Configure(prefab);
                    return pooledObject;
                }, initCount: 3,
                resetMethod: pooledObject =>
                {
                    pooledObject.Reset();
                });
                _reusableObjectPools.Add(prefab, pool);
            }

            PooledGameObject result = pool.Allocate();
            result.Configure(prefab);
            result.MarkAllocated();
            result.transform.SetPositionAndRotation(position, rotation);
            return result;
        }

        public void DespawnPooledObject(PooledGameObject pooledObject)
        {
            if (pooledObject != null && pooledObject.PrefabRef != null &&
                _reusableObjectPools.TryGetValue(
                    pooledObject.PrefabRef, out SimpleObjectPool<PooledGameObject> pool))
            {
                pool.Recycle(pooledObject);
                return;
            }

            if (pooledObject != null)
            {
                Destroy(pooledObject.gameObject);
            }
        }

        // status
        public GameObject SpawnStatus(GameObject statusPrefab, TargetableObject target)
        {
            if (target == null) { return null; }

            Status.StatusType statusType = GetStatusType(statusPrefab);
            if (target.Statuses.Contains(statusType))
            {
                return null;
            }

            if (!_statusPools.TryGetValue(statusType, out SimpleObjectPool<Status> statusPool))
            {
                statusPool = new SimpleObjectPool<Status>(factoryMethod: () =>
                {
                    GameObject statusObject = Instantiate(statusPrefab, transform).Hide();
                    return statusObject.GetComponent<Status>();
                }, initCount: 5,
                resetMethod: (status) =>
                {
                    status.Reset();
                });

                _statusPools.Add(statusType, statusPool);
            }

            Status newStatus = statusPool.Allocate();
            if (!newStatus.ActivateStatus(target))
            {
                statusPool.Recycle(newStatus);
                return null;
            }
            return newStatus.gameObject;
        }

        public void DespawnStatus(Status status)
        {
            if (_statusPools.TryGetValue(status.Type, out SimpleObjectPool<Status> statusPool))
            {
                statusPool.Recycle(status);
            }
            else
            {
                Destroy(status.gameObject);
            }
        }

        // status zones
        public GameObject SpawnStatusZone(GameObject statusZonePrefab, Vector3 position)
        {
            if (_statusZonePools.TryGetValue(statusZonePrefab, out SimpleObjectPool<StatusZone> statusZonePool))
            {
                StatusZone statusZone = statusZonePool.Allocate();
                statusZone.ActivateStatusZone(position);
                return statusZone.gameObject;
            }

            SimpleObjectPool<StatusZone> newStatusZonePool = new SimpleObjectPool<StatusZone>(factoryMethod: () =>
            {
                GameObject newStatusZone = Instantiate(statusZonePrefab, transform).Hide();
                StatusZone statusZone = newStatusZone.GetComponent<StatusZone>();
                statusZone.PrefabRef = statusZonePrefab;
                return statusZone;
            }, initCount: 5,
            resetMethod: (statusZone) =>
            {
                statusZone.Reset();
            });

            _statusZonePools.Add(statusZonePrefab, newStatusZonePool);

            StatusZone newStatusZone = newStatusZonePool.Allocate();
            newStatusZone.ActivateStatusZone(position);
            return newStatusZone.gameObject;
        }

        public void DespawnStatusZone(StatusZone statusZone)
        {
            if (_statusZonePools.TryGetValue(statusZone.PrefabRef, out SimpleObjectPool<StatusZone> statusZonePool))
            {
                statusZonePool.Recycle(statusZone);
            }
            else
            {
                Destroy(statusZone.gameObject);
            }
        }

        private Status.StatusType GetStatusType(GameObject statusPrefab)
        {
            if (_statusTypeCache.TryGetValue(statusPrefab, out Status.StatusType statusType))
            {
                return statusType;
            }

            statusType = statusPrefab.GetComponent<Status>().Type;
            _statusTypeCache.Add(statusPrefab, statusType);
            return statusType;
        }
    }
}

