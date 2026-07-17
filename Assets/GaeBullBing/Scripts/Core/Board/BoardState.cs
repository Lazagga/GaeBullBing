using System.Collections.Generic;
using GaeBullBing.Core.Towers;

namespace GaeBullBing.Core.Board
{
    public sealed class BoardState
    {
        public const int DefaultTileCount = 36;
        public List<TileState> Tiles { get; } = new();

        public int TileCount => Tiles.Count;
    }

    public sealed class TileState
    {
        public int Index { get; set; }
        public string DefinitionId { get; set; } = string.Empty;
        public TowerState Tower { get; set; }
    }
}
