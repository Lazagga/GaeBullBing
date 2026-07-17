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
        public void BoardService_DisablesTowerBuildingOnFourCorners()
        {
            var board = new BoardState();
            new BoardService().Initialize(board);

            Assert.That(board.Tiles[0].CanBuildTower, Is.False);
            Assert.That(board.Tiles[9].CanBuildTower, Is.False);
            Assert.That(board.Tiles[18].CanBuildTower, Is.False);
            Assert.That(board.Tiles[27].CanBuildTower, Is.False);
            Assert.That(board.Tiles[1].CanBuildTower, Is.True);
            Assert.That(board.Tiles[10].CanBuildTower, Is.True);
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
                new GaeBullBing.Core.Monsters.MonsterService(),
                new GaeBullBing.Core.Towers.TowerService(),
                new GaeBullBing.Core.Towers.TowerCombatService());
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

        [Test]
        public void GameSession_EndsGameWhenFiveMonstersReachStartTile()
        {
            var state = new GameState();
            var session = CreateSession(state);
            session.StartNewGame();
            for (var id = 1; id <= 5; id++)
                state.Monsters.Add(new GaeBullBing.Core.Monsters.MonsterState
                {
                    InstanceId = id,
                    CurrentHealth = 1,
                    CurrentTileIndex = 35,
                    MoveDistance = 1,
                    DistanceTravelled = 35
                });

            session.MoveMonsters();

            Assert.That(state.EscapedMonsterCount, Is.EqualTo(5));
            Assert.That(state.CurrentPhase, Is.EqualTo(TurnPhase.Defeat));
            Assert.That(state.Monsters, Is.Empty);
        }

        [Test]
        public void TowerService_BuildsOnlyTheTowerAssignedToTile()
        {
            var tile = new TileState { Index = 3, BuildTowerDefinitionId = "TOW_01" };
            var service = new GaeBullBing.Core.Towers.TowerService();

            var tower = service.Build(tile, "TOW_01");

            Assert.That(tile.HasTower, Is.True);
            Assert.That(tower.DefinitionId, Is.EqualTo("TOW_01"));
            Assert.That(tower.InstanceId, Is.GreaterThan(0));
        }

        [Test]
        public void TowerService_UpgradeAddsModifierWithoutReplacingBaseTower()
        {
            var tile = new TileState { Index = 3, BuildTowerDefinitionId = "TOW_01" };
            var service = new GaeBullBing.Core.Towers.TowerService();
            var tower = service.Build(tile, "TOW_01");

            service.Upgrade(tile, "UPG_FIRE_02", 2);

            Assert.That(tile.Tower, Is.SameAs(tower));
            Assert.That(tile.Tower.DefinitionId, Is.EqualTo("TOW_01"));
            Assert.That(tile.Tower.UpgradeTier, Is.EqualTo(2));
            Assert.That(tile.Tower.AppliedUpgradeIds, Contains.Item("UPG_FIRE_02"));
        }

        [Test]
        public void TowerCombat_PrioritizesExistingTargetBeforeCloserMonster()
        {
            var state = CreateCombatState();
            var tower = state.Board.Tiles[5].Tower;
            tower.TargetInstanceIds.Add(2);
            state.Monsters.Add(CreateMonster(1, 4, 10, 4));
            state.Monsters.Add(CreateMonster(2, 8, 10, 8));

            var results = ResolveCombat(state, 10, 3, 1);

            Assert.That(results[0].TargetInstanceId, Is.EqualTo(2));
        }

        [Test]
        public void TowerCombat_PrioritizesDistanceBeforeHealth()
        {
            var state = CreateCombatState();
            state.Monsters.Add(CreateMonster(1, 4, 10, 4));
            state.Monsters.Add(CreateMonster(2, 7, 30, 7));

            var results = ResolveCombat(state, 5, 3, 1);

            Assert.That(results[0].TargetInstanceId, Is.EqualTo(1));
        }

        [Test]
        public void TowerCombat_PrioritizesHigherHealthAtEqualDistance()
        {
            var state = CreateCombatState();
            state.Monsters.Add(CreateMonster(1, 4, 10, 4));
            state.Monsters.Add(CreateMonster(2, 6, 30, 6));

            var results = ResolveCombat(state, 5, 3, 1);

            Assert.That(results[0].TargetInstanceId, Is.EqualTo(2));
        }

        [Test]
        public void TowerCombat_ResolvesRearTowerBeforeFrontTower()
        {
            var state = CreateCombatState();
            state.Board.Tiles[5].Tower = null;
            state.Board.Tiles[3].Tower = new GaeBullBing.Core.Towers.TowerState
            {
                InstanceId = 1,
                DefinitionId = "TOW_01"
            };
            state.Board.Tiles[7].Tower = new GaeBullBing.Core.Towers.TowerState
            {
                InstanceId = 2,
                DefinitionId = "TOW_01"
            };
            state.Monsters.Add(CreateMonster(1, 5, 10, 5));

            var results = ResolveCombat(state, 10, 2, 1);

            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].TowerInstanceId, Is.EqualTo(2));
        }

        private static GameState CreateCombatState()
        {
            var state = new GameState();
            new BoardService().Initialize(state.Board);
            state.Board.Tiles[5].Tower = new GaeBullBing.Core.Towers.TowerState
            {
                InstanceId = 1,
                DefinitionId = "TOW_01"
            };
            return state;
        }

        private static GameSession CreateSession(GameState state) => new(
            state,
            new BoardService(),
            new PlayerMovementService(),
            new WeightedDiceRoller(new FixedRandom(0)),
            new GaeBullBing.Core.Monsters.MonsterService(),
            new GaeBullBing.Core.Towers.TowerService(),
            new GaeBullBing.Core.Towers.TowerCombatService());

        private static GaeBullBing.Core.Monsters.MonsterState CreateMonster(
            int id, int tileIndex, int health, int distanceTravelled)
        {
            return new GaeBullBing.Core.Monsters.MonsterState
            {
                InstanceId = id,
                CurrentTileIndex = tileIndex,
                CurrentHealth = health,
                DistanceTravelled = distanceTravelled,
                MoveDistance = 1
            };
        }

        private static System.Collections.Generic.IReadOnlyList<GaeBullBing.Core.Towers.TowerAttackResult> ResolveCombat(
            GameState state, int damage, int range, int targetCount)
        {
            var stats = new System.Collections.Generic.Dictionary<string, GaeBullBing.Core.Towers.TowerCombatStats>
            {
                ["TOW_01"] = new GaeBullBing.Core.Towers.TowerCombatStats(damage, range, targetCount)
            };
            return new GaeBullBing.Core.Towers.TowerCombatService().Resolve(state, stats);
        }

        private sealed class FixedRandom : IDiceRandom
        {
            private readonly int value;
            public FixedRandom(int value) => this.value = value;
            public int Next(int maxExclusive) => value % maxExclusive;
        }
    }
}
