using System;
using GaeBullBing.Core.Board;
using GaeBullBing.Core.Dice;
using GaeBullBing.Core.Data;
using GaeBullBing.Core.Monsters;

namespace GaeBullBing.Core.Game
{
    public sealed class GameSession
    {
        private readonly BoardService boardService;
        private readonly PlayerMovementService movementService;
        private readonly WeightedDiceRoller diceRoller;
        private readonly MonsterService monsterService;

        public GameSession(
            GameState state,
            BoardService boardService,
            PlayerMovementService movementService,
            WeightedDiceRoller diceRoller,
            MonsterService monsterService)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            this.boardService = boardService ?? throw new ArgumentNullException(nameof(boardService));
            this.movementService = movementService ?? throw new ArgumentNullException(nameof(movementService));
            this.diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
            this.monsterService = monsterService ?? throw new ArgumentNullException(nameof(monsterService));
        }

        public GameState State { get; }

        public void StartNewGame(int diceCount = 2)
        {
            if (diceCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(diceCount));

            boardService.Initialize(State.Board);
            State.Dice.Clear();
            for (var index = 0; index < diceCount; index++)
                State.Dice.Add(new DiceState());

            State.Player.CurrentTileIndex = 0;
            State.Round = 1;
            State.CurrentPhase = TurnPhase.PlayerTurnStart;
            State.LastDiceResults.Clear();
            State.LastMoveDistance = 0;
        }

        public int RollDiceAndMovePlayer()
        {
            if (State.Board.TileCount == 0 || State.Dice.Count == 0)
                throw new InvalidOperationException("Start a game before rolling dice.");

            State.CurrentPhase = TurnPhase.DiceRoll;
            State.LastDiceResults.Clear();

            var distance = 0;
            foreach (var dice in State.Dice)
            {
                var result = diceRoller.Roll(dice);
                State.LastDiceResults.Add(result);
                distance += result;
            }

            State.LastMoveDistance = distance;
            State.CurrentPhase = TurnPhase.PlayerMove;
            movementService.Move(State.Player, State.Board, distance);
            State.CurrentPhase = TurnPhase.TileResolve;
            return distance;
        }

        public System.Collections.Generic.IReadOnlyList<MonsterMoveResult> MoveMonsters()
        {
            State.CurrentPhase = TurnPhase.MonsterMove;
            return monsterService.MoveAll(State);
        }

        public MonsterState SpawnMonster(MonsterDefinition definition)
        {
            State.CurrentPhase = TurnPhase.MonsterSpawn;
            return monsterService.Spawn(State, definition);
        }

        public void CompleteRound()
        {
            State.CurrentPhase = TurnPhase.RoundEnd;
            State.Round++;
            State.CurrentPhase = TurnPhase.PlayerTurnStart;
        }
    }
}
