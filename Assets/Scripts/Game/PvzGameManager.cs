using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace SoulKnight3D
{
    public class PvzGameManager : MonoBehaviour, IController
    {
        public MoreMountains.Tools.MMSoundManager MMSoundManager;

        public static PvzGameManager Instance;

        public ZombieWavesSO ZombieWavesSO;
        private int currentWaveIndex = 0;
        private float _levelDuration = 0f;
        private float _currentDuration = 0f;

        // timeout
        private float _progressTimeoutDelta = 0f;

        // events
        public EasyEvent<ZombieWavesSO> OnNewLevelStart = new EasyEvent<ZombieWavesSO>();
        public EasyEvent<float> OnProgressUpdate = new EasyEvent<float>();
        /// <summary>
        /// bool parameter: false for big wave, true for final wave
        /// </summary>
        public EasyEvent<bool> OnHugeWave = new EasyEvent<bool>();
        

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        void Start()
        {
            this.GetSystem<AudioSystem>().MusicVolume.RegisterWithInitValue((value) =>
            {
                MMSoundManager.SetVolumeMusic(value);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.GetSystem<AudioSystem>().SoundVolume.RegisterWithInitValue((value) =>
            {
                MMSoundManager.SetVolumeSfx(value);
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            if (this.GetSystem<ControlSystem>().IsMobile)
            {
                UIKit.OpenPanel<UIMobileControlPanel>();
            }

            UIKit.OpenPanel<UIPvzGamePanel>();
            UIKit.OpenPanel<UIInventoryPanel>();
            UIKit.OpenPanel<UIGamePanel>();

            PlayerController.Instance.PlayerAttack.InitPlayerAttackWithUnequipedPlant();
            AudioKit.PlayMusic("bgm_loon");

            for (int i = 0; i < ZombieWavesSO.ZombieWaves.Count - 1; i ++) // ignore final wave duration
            {
                _levelDuration += ZombieWavesSO.ZombieWaves[i].duration;
            }
            OnNewLevelStart.Trigger(ZombieWavesSO);
            StartCoroutine(RunWaves());
        }

        private void Update()
        {
            if (_progressTimeoutDelta >= 0f)
            {
                _progressTimeoutDelta -= Time.deltaTime;
                _currentDuration += Time.deltaTime;
                OnProgressUpdate.Trigger(_currentDuration / _levelDuration);
            }
        }

        private IEnumerator RunWaves()
        {
            // wait for game start tip
            yield return new WaitForSeconds(1.8f);
            foreach (ZombieWave wave in ZombieWavesSO.ZombieWaves)
            {
                yield return StartCoroutine(RunWave(wave));
            }
        }

        private IEnumerator RunWave(ZombieWave wave)
        {
            bool isFinalWave = wave == ZombieWavesSO.ZombieWaves[ZombieWavesSO.ZombieWaves.Count - 1];

            if (wave.isHugeWave)
            {
                OnHugeWave.Trigger(isFinalWave);
                yield return new WaitForSeconds(3f);
            }

            if (!isFinalWave)
            {
                _progressTimeoutDelta += wave.duration;
            }

            float remainingTime = wave.duration;

            while (remainingTime > 0)
            {
                ZombieGroup selectedGroup = wave.ZombieGroups[Random.Range(0, wave.ZombieGroups.Count)];
                StartCoroutine(SpawnGroup(selectedGroup));

                float groupCooldown = selectedGroup.Cooldown;
                if (remainingTime < groupCooldown)
                {
                    yield return new WaitForSeconds(remainingTime);
                    break; // Exit the loop since the remaining time is less than the cooldown
                }
                else
                {
                    yield return new WaitForSeconds(groupCooldown);
                    remainingTime -= groupCooldown;
                }
            }
        }

        private IEnumerator SpawnGroup(ZombieGroup group)
        {
            for (int i = 0; i < group.Count; i++)
            {
                Instantiate(group.ZombiePrefab, GetRandomSpawnPosition(), Quaternion.identity);
                yield return new WaitForSeconds(group.Cooldown / group.Count);
            }
        }

        private Vector3 GetRandomSpawnPosition()
        {
            // Implement your logic to get a random spawn position
            return new Vector3(Random.Range(42, 46), 0.3f, Random.Range(42, 46));
        }

        public IArchitecture GetArchitecture()
        {
           return Global.Interface;
        }
    }

}
