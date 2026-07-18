namespace GaeBullBing.Core.Monsters
{
    public sealed class MonsterState
    {
        public int InstanceId { get; set; }
        public string DefinitionId { get; set; } = string.Empty;
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; set; }
        public int CurrentTileIndex { get; set; }
        public int MoveDistance { get; set; }
        public int DistanceTravelled { get; set; }
        public int BurnStacks { get; set; }
        public bool TouchedFireThisMove { get; set; }
        public bool Shocked { get; set; }
        public int FrozenMovesRemaining { get; set; }
        public int FreezeImmuneLine { get; set; } = -1;
        public int StunnedMovesRemaining { get; set; }
        public bool KnockbackConsumed { get; set; }
        public bool KnockbackImmunityPending { get; set; }
        public bool PhysicsGuardConsumed { get; set; }
        public bool PhysicsGuardTriggeredThisTurn { get; set; }

        public bool IsDead => CurrentHealth <= 0;
    }
}
