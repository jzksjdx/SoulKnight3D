using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;

namespace SoulKnight3D {
    public class GameObjectsManager : MonoBehaviour
    {
        public static GameObjectsManager Instance;

        public GameObject SunPrefab;

        public List<GameObject> DropPlants;

        public float DropChance = 0.05f;

        private SimpleObjectPool<GameObject> _sunPool;

        private Dictionary<GameObject, SimpleObjectPool<GameObject>> _bulletPools = new Dictionary<GameObject, SimpleObjectPool<GameObject>>();

        private Dictionary<GameObject, SimpleObjectPool<GameObject>> _zombiePools = new Dictionary<GameObject, SimpleObjectPool<GameObject>>();

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
            _sunPool = new SimpleObjectPool<GameObject>(factoryMethod: () =>
            {
                GameObject newEnergyOrb = Instantiate(SunPrefab, transform);
                newEnergyOrb.Hide();
                return newEnergyOrb;
            },
            initCount: 5,
            resetMethod: (gameObject) =>
            {
                gameObject.GetComponent<EnergyOrb>().Reset();
            });

        }

        public GameObject SpawnSun(Vector3 position)
        {
            GameObject newOrb = _sunPool
                .Allocate()
                .Position(position)
                .Show();
            return newOrb;
        }

        public void DespawnSun(GameObject gameObject)
        {
            _sunPool.Recycle(gameObject);
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

        public GameObject SpawnZombie(GameObject zombiePrefab)
        {
            if (_zombiePools.ContainsKey(zombiePrefab))
            {
                return _zombiePools[zombiePrefab].Allocate();
            }
            else
            {
                SimpleObjectPool<GameObject> newBulletPool = new SimpleObjectPool<GameObject>(factoryMethod: () =>
                {
                    return Instantiate(zombiePrefab, transform)
                        .Self((self) =>
                        {
                            self.GetComponent<Zombie>().SetPrefabRef(zombiePrefab);
                        })
                        .Hide();
                }, initCount: 5,
                resetMethod: (gameObject) =>
                {
                    gameObject.Hide();
                    gameObject.GetComponent<Zombie>().Reset();
                });

                _zombiePools.Add(zombiePrefab, newBulletPool);
                return newBulletPool.Allocate();

            }
        }

        public void DespawnZombie(Zombie zombie)
        {
            if (_zombiePools.ContainsKey(zombie.PrefabRef))
            {
                _zombiePools[zombie.PrefabRef].Recycle(zombie.gameObject);
            }
            else
            {
                Destroy(zombie.gameObject);
            }

           
        }

        public void DrawDropPlant(Vector3 dropPosition)
        {
            if (DropChance >= Random.Range(0f, 1f))
            {
                AudioKit.PlaySound("prize");
                GameObject newDropPlant = Instantiate(DropPlants[Random.Range(0, DropPlants.Count)], dropPosition, Quaternion.identity);
                Rigidbody plantRb = newDropPlant.GetComponent<ItemPlant>().GetRigidbody();
                Vector3 randomForce = new Vector3(Random.Range(-1f, 1f), 1.5f, Random.Range(-1f, 1f));
                plantRb.AddForce(randomForce, ForceMode.Impulse);
            }
        }
    }
}

