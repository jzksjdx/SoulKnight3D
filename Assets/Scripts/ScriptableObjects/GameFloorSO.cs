using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SoulKnight3D
{
    [CreateAssetMenu(fileName = "GameFloorSO", menuName = "ScriptableObject/GameFloorSO")]
    public class GameFloorSO: ScriptableObject
    {
        public List<GameLevel> GameLevels = new List<GameLevel>();
    }

    [Serializable]
    public class GameLevel
    {
        public List<EnemyWaveSO> LevelWaves = new List<EnemyWaveSO>();
    }
}
