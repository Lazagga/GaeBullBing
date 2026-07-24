using System;
using UnityEngine;

namespace GaeBullBing.Core.Data
{
    public sealed class TowerUpgradeDatabaseDefinition : ScriptableObject
    {
        [SerializeField] private TowerUpgradeDefinition[] upgrades =
            Array.Empty<TowerUpgradeDefinition>();

        public TowerUpgradeDefinition[] Upgrades => upgrades;
    }
}
