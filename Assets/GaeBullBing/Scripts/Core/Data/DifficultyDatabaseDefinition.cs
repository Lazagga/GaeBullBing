using System;
using GaeBullBing.Core.Monsters;
using UnityEngine;

namespace GaeBullBing.Core.Data
{
    public sealed class DifficultyDatabaseDefinition : ScriptableObject
    {
        [SerializeField] private DifficultyPatternData[] patterns =
            Array.Empty<DifficultyPatternData>();
        [SerializeField, Min(1)] private int killsPerLevel = 1;
        [SerializeField, Min(0.0001f)] private float healthMultiplierPerLevel = 1f;
        [SerializeField, Min(0f)] private float defensePerLevel;

        public DifficultyPatternData[] Patterns => patterns;
        public int KillsPerLevel => killsPerLevel;
        public float HealthMultiplierPerLevel => healthMultiplierPerLevel;
        public float DefensePerLevel => defensePerLevel;
    }
}
