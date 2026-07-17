using System;

namespace GaeBullBing.Core.Dice
{
    [Serializable]
    public sealed class DiceState
    {
        public int[] Faces { get; set; } = { 1, 2, 3, 4, 5, 6 };
        public int[] Weights { get; set; } = { 1, 1, 1, 1, 1, 1 };
    }
}
