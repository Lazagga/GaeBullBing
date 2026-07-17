using System.Collections.Generic;
using GaeBullBing.Core.Board;
using GaeBullBing.Core.Dice;
using GaeBullBing.Core.Monsters;

namespace GaeBullBing.Core.Game
{
    public sealed class GameState
    {
        public int Round { get; set; }
        public TurnPhase CurrentPhase { get; set; }
        public PlayerState Player { get; } = new();
        public BoardState Board { get; } = new();
        public List<MonsterState> Monsters { get; } = new();
        public List<DiceState> Dice { get; } = new();
    }

    public sealed class PlayerState
    {
        public int CurrentTileIndex { get; set; }
        public int GrowthPoints { get; set; }
        public int Gold { get; set; }
    }
}
