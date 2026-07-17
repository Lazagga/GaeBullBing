using GaeBullBing.Core;
using GaeBullBing.Core.Board;
using GaeBullBing.Core.Dice;
using GaeBullBing.Core.Game;
using NUnit.Framework;

namespace GaeBullBing.Tests.EditMode
{
    public sealed class BasicGameLoopTests
    {
        [Test]
        public void BoardService_CreatesThirtySixLoopTiles()
        {
            var board = new BoardState();

            new BoardService().Initialize(board);

            Assert.That(board.TileCount, Is.EqualTo(36));
            Assert.That(board.Tiles[0].DefinitionId, Is.EqualTo("start"));
            Assert.That(board.Tiles[35].Index, Is.EqualTo(35));
        }

        [Test]
        public void BoardLayout_CreatesTenByTenSquareBorderWithThirtySixUniqueCells()
        {
            Assert.That(BoardLayout.Cells, Has.Count.EqualTo(36));
            Assert.That(BoardLayout.GetCell(0), Is.EqualTo(new BoardCell(0, 0)));
            Assert.That(BoardLayout.GetCell(9), Is.EqualTo(new BoardCell(0, 9)));
            Assert.That(BoardLayout.GetCell(18), Is.EqualTo(new BoardCell(9, 9)));
            Assert.That(BoardLayout.GetCell(27), Is.EqualTo(new BoardCell(9, 0)));
            Assert.That(new System.Collections.Generic.HashSet<BoardCell>(BoardLayout.Cells), Has.Count.EqualTo(36));
        }

        [Test]
        public void BoardLayout_EveryConsecutiveCellIsAdjacentIncludingLoopEnd()
        {
            for (var index = 0; index < BoardLayout.Cells.Count; index++)
            {
                var current = BoardLayout.Cells[index];
                var next = BoardLayout.Cells[(index + 1) % BoardLayout.Cells.Count];
                var distance = System.Math.Abs(current.X - next.X) + System.Math.Abs(current.Y - next.Y);
                Assert.That(distance, Is.EqualTo(1), $"Cells {index} and {(index + 1) % 36} must be adjacent.");
            }
        }

        [Test]
        public void PlayerMovement_WrapsAroundBoard()
        {
            var board = new BoardState();
            new BoardService().Initialize(board);
            var player = new PlayerState { CurrentTileIndex = 32 };

            var destination = new PlayerMovementService().Move(player, board, 7);

            Assert.That(destination, Is.EqualTo(3));
        }

        [Test]
        public void WeightedDiceRoller_UsesConfiguredWeights()
        {
            var dice = new DiceState(
                new[] { 1, 2, 3 },
                new[] { 0, 0, 5 });

            var result = new WeightedDiceRoller(new FixedRandom(0)).Roll(dice);

            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public void GameSession_RollsTwoDiceAndMovesPlayer()
        {
            var state = new GameState();
            var session = new GameSession(
                state,
                new BoardService(),
                new PlayerMovementService(),
                new WeightedDiceRoller(new FixedRandom(0)),
                new GaeBullBing.Core.Monsters.MonsterService());
            session.StartNewGame();

            var distance = session.RollDiceAndMovePlayer();

            Assert.That(distance, Is.EqualTo(2));
            Assert.That(state.LastDiceResults, Is.EqualTo(new[] { 1, 1 }));
            Assert.That(state.Player.CurrentTileIndex, Is.EqualTo(2));
            Assert.That(state.CurrentPhase, Is.EqualTo(TurnPhase.TileResolve));
        }

        [Test]
        public void MonsterService_MovesMonsterByTurnDistance()
        {
            var state = new GameState();
            state.Monsters.Add(new GaeBullBing.Core.Monsters.MonsterState
            {
                InstanceId = 1,
                CurrentHealth = 30,
                CurrentTileIndex = 0,
                MoveDistance = 2
            });

            var results = new GaeBullBing.Core.Monsters.MonsterService().MoveAll(state);

            Assert.That(results[0].Distance, Is.EqualTo(2));
            Assert.That(state.Monsters[0].CurrentTileIndex, Is.EqualTo(2));
            Assert.That(state.Monsters[0].DistanceTravelled, Is.EqualTo(2));
        }

        [Test]
        public void MonsterService_RemovesMonsterAfterFullLap()
        {
            var state = new GameState();
            state.Monsters.Add(new GaeBullBing.Core.Monsters.MonsterState
            {
                InstanceId = 1,
                CurrentHealth = 30,
                CurrentTileIndex = 35,
                MoveDistance = 2,
                DistanceTravelled = 35
            });

            var results = new GaeBullBing.Core.Monsters.MonsterService().MoveAll(state);

            Assert.That(results[0].Distance, Is.EqualTo(1));
            Assert.That(results[0].ReachedBase, Is.True);
            Assert.That(state.Monsters, Is.Empty);
        }

        private sealed class FixedRandom : IDiceRandom
        {
            private readonly int value;
            public FixedRandom(int value) => this.value = value;
            public int Next(int maxExclusive) => value % maxExclusive;
        }
    }
}
