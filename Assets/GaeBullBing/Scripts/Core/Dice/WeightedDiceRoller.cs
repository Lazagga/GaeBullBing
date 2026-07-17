using System;

namespace GaeBullBing.Core.Dice
{
    public interface IDiceRandom
    {
        int Next(int maxExclusive);
    }

    public sealed class SystemDiceRandom : IDiceRandom
    {
        private readonly Random random;

        public SystemDiceRandom() : this(new Random()) { }
        public SystemDiceRandom(Random random) => this.random = random;
        public int Next(int maxExclusive) => random.Next(maxExclusive);
    }

    public sealed class WeightedDiceRoller
    {
        private readonly IDiceRandom random;

        public WeightedDiceRoller(IDiceRandom random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public int Roll(DiceState dice)
        {
            if (dice == null)
                throw new ArgumentNullException(nameof(dice));

            var totalWeight = 0;
            for (var index = 0; index < dice.Weights.Length; index++)
            {
                if (dice.Weights[index] < 0)
                    throw new InvalidOperationException("A dice weight cannot be negative.");
                totalWeight += dice.Weights[index];
            }

            if (totalWeight <= 0)
                throw new InvalidOperationException("A dice must have at least one selectable face.");

            var selection = random.Next(totalWeight);
            var accumulatedWeight = 0;
            for (var index = 0; index < dice.Faces.Length; index++)
            {
                accumulatedWeight += dice.Weights[index];
                if (selection < accumulatedWeight)
                    return dice.Faces[index];
            }

            throw new InvalidOperationException("Failed to select a dice face.");
        }
    }
}
