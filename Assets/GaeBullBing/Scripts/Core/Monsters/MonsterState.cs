namespace GaeBullBing.Core.Monsters
{
    public sealed class MonsterState
    {
        public int InstanceId { get; set; }
        public string DefinitionId { get; set; } = string.Empty;
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public int CurrentTileIndex { get; set; }
        public int MoveDistance { get; set; }
        public int DistanceTravelled { get; set; }
        public int BurnStacks { get; set; }
        public bool Shocked { get; set; }
        public int FrozenMovesRemaining { get; set; }
        public int FreezeImmuneLine { get; set; } = -1;
        public int StunnedMovesRemaining { get; set; }
        public bool KnockbackConsumed { get; set; }
        public bool PhysicsGuardConsumed { get; set; }

        public bool IsDead => CurrentHealth <= 0;
    }
}
