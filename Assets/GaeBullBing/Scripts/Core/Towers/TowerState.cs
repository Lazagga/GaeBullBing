namespace GaeBullBing.Core.Towers
{
    public sealed class TowerState
    {
        public string DefinitionId { get; set; } = string.Empty;
        public int Level { get; set; } = 1;
        public int OwnerId { get; set; }
    }
}
