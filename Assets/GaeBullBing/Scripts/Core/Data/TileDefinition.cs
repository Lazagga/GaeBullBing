using UnityEngine;

namespace GaeBullBing.Core.Data
{
    [CreateAssetMenu(menuName = "GaeBullBing/Data/Tile", fileName = "TileDefinition")]
    public sealed class TileDefinition : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private TileType type;
        [SerializeField] private TowerElement element;
        [SerializeField] private string buildTowerDefinitionId = string.Empty;

        public string Id => id;
        public string DisplayName => displayName;
        public TileType Type => type;
        public TowerElement Element => element;
        public string BuildTowerDefinitionId => buildTowerDefinitionId;
    }
}
