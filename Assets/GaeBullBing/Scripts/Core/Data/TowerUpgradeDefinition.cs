using System;
using UnityEngine;

namespace GaeBullBing.Core.Data
{
    [CreateAssetMenu(menuName = "GaeBullBing/Data/Tower Upgrade", fileName = "TowerUpgradeDefinition")]
    public sealed class TowerUpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string description = string.Empty;
        [SerializeField] private TowerElement element;
        [SerializeField, Min(2)] private int tier = 2;
        [SerializeField, Min(0)] private int weight = 1;
        [SerializeField] private TowerStatModifier[] statModifiers = Array.Empty<TowerStatModifier>();
        [SerializeField] private TowerUpgradeEffect[] effects = Array.Empty<TowerUpgradeEffect>();

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public TowerElement Element => element;
        public int Tier => tier;
        public int Weight => weight;
        public TowerStatModifier[] StatModifiers => statModifiers;
        public TowerUpgradeEffect[] Effects => effects;
    }

    [Serializable] public struct TowerStatModifier { public string Stat; public string Operation; public float Value; }
    [Serializable] public struct TowerUpgradeEffect { public string Id; public float Value; }
}
