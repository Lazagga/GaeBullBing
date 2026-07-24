using System;
using UnityEngine;

namespace GaeBullBing.Core.Data
{
    public sealed class MonsterDatabaseDefinition : ScriptableObject
    {
        [SerializeField] private MonsterDefinition[] monsters = Array.Empty<MonsterDefinition>();
        public MonsterDefinition[] Monsters => monsters;
    }
}
