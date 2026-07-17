using System;
using System.Collections.Generic;

namespace GaeBullBing.Core.Board
{
    public readonly struct BoardCell : IEquatable<BoardCell>
    {
        public BoardCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public bool Equals(BoardCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is BoardCell other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public override string ToString() => $"({X}, {Y})";
    }

    public static class BoardLayout
    {
        public const int SideLength = 10;

        private static readonly IReadOnlyList<BoardCell> cells = CreateSquareBorder();

        public static IReadOnlyList<BoardCell> Cells => cells;

        public static BoardCell GetCell(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= cells.Count)
                throw new ArgumentOutOfRangeException(nameof(tileIndex));

            return cells[tileIndex];
        }

        private static IReadOnlyList<BoardCell> CreateSquareBorder()
        {
            var result = new List<BoardCell>(BoardState.DefaultTileCount);
            var last = SideLength - 1;

            for (var y = 0; y <= last; y++)
                result.Add(new BoardCell(0, y));
            for (var x = 1; x <= last; x++)
                result.Add(new BoardCell(x, last));
            for (var y = last - 1; y >= 0; y--)
                result.Add(new BoardCell(last, y));
            for (var x = last - 1; x >= 1; x--)
                result.Add(new BoardCell(x, 0));

            return result;
        }
    }
}
