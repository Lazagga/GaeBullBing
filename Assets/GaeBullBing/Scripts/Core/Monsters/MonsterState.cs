namespace GaeBullBing.Core.Monsters
{
    public sealed class MonsterState
    {
        public int InstanceId { get; set; }
        public string DefinitionId { get; set; } = string.Empty;
        public int CurrentHealth { get; set; }
        public int CurrentTileIndex { get; set; }
        public int RemainingMove { get; set; }
    }
}
