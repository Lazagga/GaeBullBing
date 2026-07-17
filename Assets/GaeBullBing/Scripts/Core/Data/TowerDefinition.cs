using System;
using UnityEngine;

namespace GaeBullBing.Core.Data
{
    [CreateAssetMenu(menuName = "GaeBullBing/Data/Tower", fileName = "TowerDefinition")]
    public sealed class TowerDefinition : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private TowerElement element;
        [SerializeField] private TowerAttackType attackType;
        [SerializeField] private TowerLevelData[] levels = Array.Empty<TowerLevelData>();

        public string Id => id;
        public string DisplayName => displayName;
        public TowerElement Element => element;
        public TowerAttackType AttackType => attackType;
        public TowerLevelData[] Levels => levels;
    }

    [Serializable]
    public struct TowerLevelData
    {
        [Min(0)] public int Damage;
        [Min(0)] public int Range;
        [Min(0.01f)] public float AttackInterval;
        [Min(0)] public int UpgradeCost;
    }
}
