using System;

namespace GaeBullBing.Core.Dice
{
    [Serializable]
    public sealed class DiceState
    {
        public DiceState()
            : this(new[] { 1, 2, 3, 4, 5, 6 }, new[] { 1, 1, 1, 1, 1, 1 })
        {
        }

        public DiceState(int[] faces, int[] weights)
        {
            if (faces == null || weights == null)
                throw new ArgumentNullException(faces == null ? nameof(faces) : nameof(weights));
            if (faces.Length == 0 || faces.Length != weights.Length)
                throw new ArgumentException("Faces and weights must have the same non-zero length.");

            Faces = (int[])faces.Clone();
            Weights = (int[])weights.Clone();
        }

        public int[] Faces { get; }
        public int[] Weights { get; }

        public void SetWeight(int faceIndex, int weight)
        {
            if (weight < 0)
                throw new ArgumentOutOfRangeException(nameof(weight));

            Weights[faceIndex] = weight;
        }
    }
}
