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
        public Dictionary<TowerElement, int> PermanentTowerDamageBonuses { get; } = new();

        public int GetPermanentTowerDamageBonus(TowerElement element) =>
            PermanentTowerDamageBonuses.TryGetValue(element, out var value) ? value : 0;

        public void AddPermanentTowerDamageBonus(TowerElement element, int amount)
        {
            if (element == TowerElement.None || amount <= 0) return;
            PermanentTowerDamageBonuses[element] = GetPermanentTowerDamageBonus(element) + amount;
        }
    }

    public sealed class PlayerState
    {
        public int CurrentTileIndex { get; set; }
        public int GrowthPoints { get; set; }
        public int Gold { get; set; }
    }
}
