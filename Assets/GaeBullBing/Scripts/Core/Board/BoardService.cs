using System;

namespace GaeBullBing.Core.Board
{
    public sealed class BoardService
    {
        public void Initialize(BoardState board, int tileCount = BoardState.DefaultTileCount)
        {
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (tileCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(tileCount));

            board.Tiles.Clear();
            for (var index = 0; index < tileCount; index++)
            {
                board.Tiles.Add(new TileState
                {
                    Index = index,
                    DefinitionId = index == 0 ? "start" : "normal"
                });
            }
        }
    }
}
