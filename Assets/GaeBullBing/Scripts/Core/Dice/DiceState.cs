using System;

namespace GaeBullBing.Core.Dice
{
    [Serializable]
    public sealed class DiceState
    {
        public DiceState()
            : this("DICE_WHITE", "흰 주사위", new[] { 1, 2, 3, 4, 5, 6 },
                new[] { 1, 1, 1, 1, 1, 1 }, 0.96f, 0.96f, 0.93f, "기본 주사위", "", 0f)
        {
        }

        public DiceState(int[] faces, int[] weights)
            : this("DICE_CUSTOM", "주사위", faces, weights, 0.96f, 0.96f, 0.93f, "", "", 0f)
        {
        }

        public DiceState(string id, string displayName, int[] faces, int[] weights,
            float red, float green, float blue, string passiveDescription,
            string passiveId, float passiveValue)
        {
            if (faces == null || weights == null)
                throw new ArgumentNullException(faces == null ? nameof(faces) : nameof(weights));
            if (faces.Length == 0 || faces.Length != weights.Length)
                throw new ArgumentException("Faces and weights must have the same non-zero length.");

            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Faces = (int[])faces.Clone();
            Weights = (int[])weights.Clone();
            Red = red;
            Green = green;
            Blue = blue;
            PassiveDescription = passiveDescription ?? string.Empty;
            PassiveId = passiveId ?? string.Empty;
            PassiveValue = passiveValue;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int[] Faces { get; }
        public int[] Weights { get; }
        public float Red { get; }
        public float Green { get; }
        public float Blue { get; }
        public string PassiveDescription { get; }
        public string PassiveId { get; }
        public float PassiveValue { get; }
        public bool UsesBlackPips => Id == "DICE_WHITE";

        public DiceState Clone() => new DiceState(Id, DisplayName, Faces, Weights,
            Red, Green, Blue, PassiveDescription, PassiveId, PassiveValue);

        public void SetWeight(int faceIndex, int weight)
        {
            if (weight < 0)
                throw new ArgumentOutOfRangeException(nameof(weight));
            Weights[faceIndex] = weight;
        }

        public static DiceState CreateBlack() => new DiceState(
            "DICE_BLACK", "검은 주사위", new[] { 1, 2, 3, 4, 5, 6 },
            new[] { 1, 1, 1, 1, 1, 1 }, 0.035f, 0.035f, 0.045f,
            "기본 주사위", "", 0f);
    }
}
