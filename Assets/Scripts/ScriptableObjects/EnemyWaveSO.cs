using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace SoulKnight3D
{
    [CreateAssetMenu(fileName = "EnemyWaveSO", menuName = "ScriptableObject/EnemyWaveSO")]
    public class EnemyWaveSO : ScriptableObject
    {
        public List<EnemyWaveGroup> EnemyWaveGroups = new List<EnemyWaveGroup>();
    }

    [Serializable]
    public class EnemyWaveGroup
    {
        //public string Name;
        public List<EnemyWave> Waves = new List<EnemyWave>();
    }

    [Serializable]
    public class EnemyWave
    {
        //public string Name;
        public GameObject EnemyPrefab;
        public int Count;
    }
}
