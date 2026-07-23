using System;
using UnityEngine;

namespace SoulKnight3D
{
    [CreateAssetMenu(
        fileName = "Boss Encounter",
        menuName = "ScriptableObject/Boss Encounter")]
    public sealed class BossEncounterDataSO : ScriptableObject
    {
        [SerializeField] private GameObject _bossPrefab;
        [SerializeField] private Sprite _bossSprite;
        [SerializeField] private string _displayName;
        [SerializeField] private Color _introBackgroundColor = Color.white;

        public GameObject BossPrefab => _bossPrefab;
        public Sprite BossSprite => _bossSprite;
        public string DisplayName => _displayName;
        public Color IntroBackgroundColor => _introBackgroundColor;

        public bool IsValid =>
            _bossPrefab != null &&
            _bossPrefab.GetComponent<BossEnemy>() != null;
    }

    [Serializable]
    public sealed class WeightedBossEncounter
    {
        public BossEncounterDataSO Boss;
        [Min(0f)] public float Weight = 1f;

        public bool IsAvailable => Boss != null && Boss.IsValid && Weight > 0f;
    }
}
