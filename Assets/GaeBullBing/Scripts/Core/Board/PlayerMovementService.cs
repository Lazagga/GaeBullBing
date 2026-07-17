using System;
using GaeBullBing.Core.Game;

namespace GaeBullBing.Core.Board
{
    public sealed class PlayerMovementService
    {
        public int Move(PlayerState player, BoardState board, int distance)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));
            if (board == null)
                throw new ArgumentNullException(nameof(board));
            if (board.TileCount == 0)
                throw new InvalidOperationException("The board must contain at least one tile.");
            if (distance < 0)
                throw new ArgumentOutOfRangeException(nameof(distance));

            player.CurrentTileIndex = (player.CurrentTileIndex + distance) % board.TileCount;
            return player.CurrentTileIndex;
        }
    }
}
