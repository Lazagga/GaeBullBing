using System.Collections.Generic;
using GaeBullBing.Core.Board;
using GaeBullBing.Core.Dice;
using GaeBullBing.Core.Monsters;
using GaeBullBing.Core;

namespace GaeBullBing.Core.Game
{
    public sealed class GameState
    {
        public const int DefaultEscapeLimit = 5;

        public int Round { get; set; }
        public TurnPhase CurrentPhase { get; set; }
        public int EscapedMonsterCount { get; set; }
        public int EscapeLimit { get; set; } = DefaultEscapeLimit;
        public bool IsGameOver => CurrentPhase == TurnPhase.Defeat;
        public PlayerState Player { get; } = new();
        public BoardState Board { get; } = new();
        public List<MonsterState> Monsters { get; } = new();
        public DifficultyState Difficulty { get; } = new();
        public List<DiceState> Dice { get; } = new();
        public List<int> LastDiceResults { get; } = new();
        public int LastMoveDistance { get; set; }
        public Dictionary<TowerElement, float> PermanentTowerDamageRateBonuses { get; } = new();
        public float PermanentAllTowerDamageRateBonus { get; private set; }

        public float GetPermanentTowerDamageRateBonus(TowerElement element) =>
            PermanentTowerDamageRateBonuses.TryGetValue(element, out var value) ? value : 0f;

        public void AddPermanentTowerDamageRateBonus(TowerElement element, float amount)
        {
            if (element == TowerElement.None || amount <= 0f) return;
            PermanentTowerDamageRateBonuses[element] = GetPermanentTowerDamageRateBonus(element) + amount;
        }

        public void AddPermanentAllTowerDamageRateBonus(float amount)
        {
            if (amount > 0f) PermanentAllTowerDamageRateBonus += amount;
        }

        public void ResetPermanentTowerBonuses()
        {
            PermanentTowerDamageRateBonuses.Clear();
            PermanentAllTowerDamageRateBonus = 0f;
        }
    }

    public sealed class PlayerState
    {
        public int CurrentTileIndex { get; set; }
        public int GrowthPoints { get; set; }
        public int Gold { get; set; }
    }
}
