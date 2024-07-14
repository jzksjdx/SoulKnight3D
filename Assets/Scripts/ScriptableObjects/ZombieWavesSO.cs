using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    [CreateAssetMenu(fileName = "ZombieWavesSO", menuName = "ScriptableObject/ZombieWavesSO")]
    public class ZombieWavesSO : ScriptableObject
    {
        public List<ZombieWave> ZombieWaves = new List<ZombieWave>();
    }

    [Serializable]
    public class ZombieWave
    {
        public List<ZombieGroup> ZombieGroups = new List<ZombieGroup>();
        public float duration;
        public bool isHugeWave;
    }

    [Serializable]
    public class ZombieGroup
    {
        public GameObject ZombiePrefab;
        public int Count;
        public float Cooldown;
    }
}