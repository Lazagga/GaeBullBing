using UnityEngine;

namespace GaeBullBing.Core.Data
{
    [CreateAssetMenu(menuName = "GaeBullBing/Data/Tile", fileName = "TileDefinition")]
    public sealed class TileDefinition : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private TileType type;
        [SerializeField] private TowerElement element;

        public string Id => id;
        public TileType Type => type;
        public TowerElement Element => element;
    }
}
