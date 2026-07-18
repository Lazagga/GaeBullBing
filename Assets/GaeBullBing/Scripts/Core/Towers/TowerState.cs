using System.Collections.Generic;

namespace GaeBullBing.Core.Towers
{
    public enum StoneExitAnimation
    {
        None,
        FallOffBoard,
        ShrinkOnZeroDamage
    }

    public sealed class TowerState
    {
        public int InstanceId { get; set; }
        public string DefinitionId { get; set; } = string.Empty;
        public int UpgradeTier { get; set; } = 1;
        public List<string> AppliedUpgradeIds { get; } = new();
        public List<string> AppliedEffectIds { get; } = new();
        public List<int> TargetInstanceIds { get; } = new();
        public int AttackCooldownRounds { get; set; }
        public int BonusAttackCount { get; set; }
        public int BonusAttackTurnsRemaining { get; set; }
        public bool StoneActive { get; set; }
        public int StoneTileIndex { get; set; }
        public float StoneDamageMultiplier { get; set; } = 1f;
        public int StoneBaseDamage { get; set; }
        public List<int> StoneTraversalTiles { get; } = new();
        public StoneExitAnimation StoneExitAnimation { get; set; }
        public int StoneExitTileIndex { get; set; } = -1;
    }
}
