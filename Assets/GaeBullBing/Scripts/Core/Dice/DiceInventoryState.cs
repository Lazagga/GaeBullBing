using System;
using System.Collections.Generic;

namespace GaeBullBing.Core.Dice
{
    [Serializable]
    public sealed class DiceInventoryState
    {
        public const int EquippedCount = 2;
        public const int Capacity = 4;

        public List<DiceState> Dice { get; } = new();

        public bool QueueEquip(IList<DiceState> equipped, int slotIndex, int inventoryIndex)
        {
            if (equipped == null || slotIndex < 0 || slotIndex >= EquippedCount ||
                inventoryIndex < 0 || inventoryIndex >= Dice.Count)
                return false;

            var selected = Dice[inventoryIndex];
            for (var slot = 0; slot < equipped.Count; slot++)
                if (slot != slotIndex && ReferenceEquals(equipped[slot], selected))
                    return false;

            equipped[slotIndex] = selected;
            return true;
        }

        public bool TryStoreReward(DiceState reward)
        {
            if (reward == null || Dice.Count >= Capacity) return false;
            Dice.Add(reward);
            return true;
        }

        public bool Replace(int index, DiceState reward, IList<DiceState> equipped)
        {
            if (reward == null || index < 0 || index >= Dice.Count) return false;
            var replaced = Dice[index];
            Dice[index] = reward;
            for (var slot = 0; slot < EquippedCount && slot < equipped.Count; slot++)
                if (ReferenceEquals(equipped[slot], replaced)) equipped[slot] = reward;
            return true;
        }

        public void ResetToDefaults()
        {
            Dice.Clear();
            Dice.Add(new DiceState());
            Dice.Add(DiceState.CreateBlack());
        }
    }

    public static class DiceCatalog
    {
        private static readonly DiceState[] Rewards =
        {
            new DiceState("DICE_FIRE", "불꽃 주사위", new[] { 2, 2, 4, 4, 6, 6 },
                new[] { 1, 1, 1, 1, 1, 1 }, .78f, .18f, .15f,
                "불 속성 공격력 +15%", "fire_tower_damage", .15f),
            new DiceState("DICE_ICE", "서리 주사위", new[] { 1, 1, 3, 3, 5, 5 },
                new[] { 1, 1, 1, 1, 1, 1 }, .18f, .38f, .78f,
                "얼음 타일 둔화 강화", "ice_slow", .15f),
            new DiceState("DICE_NATURE", "새싹 주사위", new[] { 2, 2, 3, 3, 5, 5 },
                new[] { 1, 1, 1, 1, 1, 1 }, .35f, .67f, .18f,
                "자원 획득량 +10%", "resource_gain", .10f),
            new DiceState("DICE_VIOLET", "번개 주사위", new[] { 2, 2, 3, 3, 6, 6 },
                new[] { 1, 1, 1, 1, 1, 1 }, .46f, .21f, .66f,
                "전기 타워 연쇄 피해 +10%", "electric_chain", .10f)
        };

        public static DiceState GetReward(int index) => Rewards[Math.Abs(index) % Rewards.Length].Clone();
    }
}
