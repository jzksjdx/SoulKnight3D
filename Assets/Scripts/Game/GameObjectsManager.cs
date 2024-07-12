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

        public GameObject SpawnBullet(GameObject bulletPrefab)
        {
            if (_bulletPools.ContainsKey(bulletPrefab))
            {
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
    }
}

