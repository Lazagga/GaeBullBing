using System;
using UnityEngine;

namespace GaeBullBing.Core.Data
{
    public sealed class TowerDatabaseDefinition : ScriptableObject
    {
        [SerializeField] private TowerDefinition[] towers = Array.Empty<TowerDefinition>();
        public TowerDefinition[] Towers => towers;
    }
}
