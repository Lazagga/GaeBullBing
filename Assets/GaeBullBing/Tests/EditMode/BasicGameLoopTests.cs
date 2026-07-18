using GaeBullBing.Core;
using GaeBullBing.Core.Board;
using GaeBullBing.Core.Dice;
using GaeBullBing.Core.Game;
using NUnit.Framework;
using UnityEngine;

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
        public void MonsterService_IceEnteredMidMoveLeavesOnlyOneRemainingStep()
        {
            var state = new GameState();
            new BoardService().Initialize(state.Board);
            state.Board.Tiles[2].IceTurnsRemaining = 1;
            state.Monsters.Add(new GaeBullBing.Core.Monsters.MonsterState
            {
                InstanceId = 1, CurrentHealth = 30, CurrentTileIndex = 0, MoveDistance = 5
            });

            var result = new GaeBullBing.Core.Monsters.MonsterService().MoveAll(state)[0];

            Assert.That(result.Distance, Is.EqualTo(3));
            Assert.That(state.Monsters[0].CurrentTileIndex, Is.EqualTo(3));
        }

        [Test]
        public void MonsterService_RemovingIceRestoresBaseMovement()
        {
            var state = new GameState();
            new BoardService().Initialize(state.Board);
            state.Board.Tiles[0].IceTurnsRemaining = 1;
            state.Monsters.Add(new GaeBullBing.Core.Monsters.MonsterState
            {
                InstanceId = 1, CurrentHealth = 30, CurrentTileIndex = 0, MoveDistance = 4
            });
            var service = new GaeBullBing.Core.Monsters.MonsterService();
            Assert.That(service.MoveAll(state)[0].Distance, Is.EqualTo(1));

            state.Board.Tiles[1].IceTurnsRemaining = 0;
            Assert.That(service.MoveAll(state)[0].Distance, Is.EqualTo(4));
        }

        [Test]
        public void MonsterService_FireCrossedMidMoveAddsBurnStack()
        {
            var state = new GameState();
            new BoardService().Initialize(state.Board);
            state.Board.Tiles[2].FireTurnsRemaining = 1;
            state.Monsters.Add(new GaeBullBing.Core.Monsters.MonsterState
            {
                InstanceId = 1, CurrentHealth = 30, CurrentTileIndex = 0, MoveDistance = 5
            });

            new GaeBullBing.Core.Monsters.MonsterService().MoveAll(state);

            Assert.That(state.Monsters[0].CurrentTileIndex, Is.EqualTo(5));
            Assert.That(state.Monsters[0].BurnStacks, Is.EqualTo(1));
        }

        [Test]
        public void MonsterState_BurnStacksAreClampedBetweenZeroAndTen()
        {
            var monster = new GaeBullBing.Core.Monsters.MonsterState { BurnStacks = 25 };
            Assert.That(monster.BurnStacks, Is.EqualTo(10));

            monster.BurnStacks = -3;
            Assert.That(monster.BurnStacks, Is.EqualTo(0));
        }

        [Test]
        public void MonsterState_ReceiveDamageUsesPatternDefensePerLevel()
        {
            var difficulty = new GaeBullBing.Core.Monsters.DifficultyState
            {
                Level = 6,
                DefensePerLevel = 20f
            };
            var monster = new GaeBullBing.Core.Monsters.MonsterState
            {
                CurrentHealth = 100f,
                MaxHealth = 100f,
                BaseDefense = 0f
            };

            var actualDamage = monster.ReceiveDamage(100f, difficulty);

            Assert.That(monster.GetDefense(difficulty), Is.EqualTo(100f));
            Assert.That(actualDamage, Is.EqualTo(50f).Within(0.0001f));
            Assert.That(monster.CurrentHealth, Is.EqualTo(50f).Within(0.0001f));
        }

        [Test]
        public void MonsterState_ReceiveDamageCombinesBaseDefenseAndShock()
        {
            var difficulty = new GaeBullBing.Core.Monsters.DifficultyState
            {
                Level = 1,
                DefensePerLevel = 20f
            };
            var monster = new GaeBullBing.Core.Monsters.MonsterState
            {
                CurrentHealth = 200f,
                MaxHealth = 200f,
                BaseDefense = 20f,
                Shocked = true
            };

            var actualDamage = monster.ReceiveDamage(120f, difficulty);

            Assert.That(actualDamage, Is.EqualTo(130f).Within(0.0001f));
            Assert.That(monster.CurrentHealth, Is.EqualTo(70f).Within(0.0001f));
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
        public void DifficultyService_FinalPatternRepeatsWhileLevelAndHealthKeepIncreasing()
        {
            var patterns = new[]
            {
                new GaeBullBing.Core.Monsters.DifficultyPatternData
                {
                    RequiredKills = 0,
                    HealthMultiplier = 1f,
                    MonsterIds = new[] { "MON_001" }
                },
                new GaeBullBing.Core.Monsters.DifficultyPatternData
                {
                    RequiredKills = 10,
                    HealthMultiplier = 1.5f,
                    MonsterIds = new[] { "MON_002", "MON_003" }
                }
            };
            var service = new GaeBullBing.Core.Monsters.DifficultyService(patterns, 10, 1.5f);
            var state = new GaeBullBing.Core.Monsters.DifficultyState();
            service.Reset(state);

            service.AddKills(state, 10);
            Assert.That(service.GetNextMonsterId(state), Is.EqualTo("MON_002"));

            service.AddKills(state, 10);

            Assert.That(state.Level, Is.EqualTo(3));
            Assert.That(service.GetHealthMultiplier(state), Is.EqualTo(2.25f).Within(0.0001f));
            Assert.That(service.GetNextMonsterId(state), Is.EqualTo("MON_003"));
            Assert.That(service.GetRemainingKills(state), Is.EqualTo(10));
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
        public void GameSession_ElementFlatDamageBonusAffectsMatchingTowerCombat()
        {
            var state = new GameState();
            var session = CreateSession(state);
            session.StartNewGame();
            state.Board.Tiles[5].Tower = new GaeBullBing.Core.Towers.TowerState
            {
                InstanceId = 1,
                DefinitionId = "TOW_01"
            };
            state.Monsters.Add(CreateMonster(1, 5, 100, 5));

            var definition = ScriptableObject.CreateInstance<GaeBullBing.Core.Data.TowerDefinition>();
            SetPrivateField(definition, "id", "TOW_01");
            SetPrivateField(definition, "element", TowerElement.Fire);
            SetPrivateField(definition, "damage", 10);
            SetPrivateField(definition, "range", 3);
            SetPrivateField(definition, "targetCount", 1);
            SetPrivateField(definition, "attackCount", 1);
            session.AddPermanentTowerDamageFlatBonus(TowerElement.Fire, 30);

            var results = session.ResolveTowerCombat(
                new[] { definition },
                System.Array.Empty<GaeBullBing.Core.Data.TowerUpgradeDefinition>());

            Assert.That(results[0].Damage, Is.EqualTo(40));
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void TowerDamage_AppliesMultipliersBeforeFlatBonuses()
        {
            var state = new GameState();
            var session = CreateSession(state);
            session.StartNewGame();
            state.Board.Tiles[5].Tower = new GaeBullBing.Core.Towers.TowerState
            {
                InstanceId = 1,
                DefinitionId = "TOW_01"
            };
            state.Board.Tiles[5].Tower.AppliedUpgradeIds.Add("UPG_TEST_DAMAGE_ORDER");
            state.Monsters.Add(CreateMonster(1, 5, 100, 5));

            var definition = ScriptableObject.CreateInstance<GaeBullBing.Core.Data.TowerDefinition>();
            SetPrivateField(definition, "id", "TOW_01");
            SetPrivateField(definition, "element", TowerElement.Fire);
            SetPrivateField(definition, "damage", 10);
            SetPrivateField(definition, "range", 3);
            SetPrivateField(definition, "targetCount", 1);
            SetPrivateField(definition, "attackCount", 1);

            var upgrade = ScriptableObject.CreateInstance<GaeBullBing.Core.Data.TowerUpgradeDefinition>();
            SetPrivateField(upgrade, "id", "UPG_TEST_DAMAGE_ORDER");
            SetPrivateField(upgrade, "statModifiers", new[]
            {
                new GaeBullBing.Core.Data.TowerStatModifier { Stat = "damage", Operation = "Multiply", Value = 2f },
                new GaeBullBing.Core.Data.TowerStatModifier { Stat = "damage", Operation = "Add", Value = 5f }
            });
            session.AddPermanentTowerDamageRateBonus(TowerElement.Fire, .5f);
            session.AddPermanentTowerDamageFlatBonus(TowerElement.Fire, 3);

            var results = session.ResolveTowerCombat(new[] { definition }, new[] { upgrade });

            Assert.That(results[0].Damage, Is.EqualTo(38));
            Object.DestroyImmediate(upgrade);
            Object.DestroyImmediate(definition);
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

        [Test]
        public void ElectricTransferRangeUpgradeOnlyExpandsBuiltInTileFieldTransfer()
        {
            var state = new GameState();
            new BoardService().Initialize(state.Board);
            var tower = new GaeBullBing.Core.Towers.TowerState
            {
                InstanceId = 1,
                DefinitionId = "TOW_04"
            };
            tower.AppliedUpgradeIds.Add("UPG_ELECTRIC_T2_00");
            tower.AppliedUpgradeIds.Add("UPG_ELECTRIC_T2_02");
            tower.AppliedUpgradeIds.Add("UPG_ELECTRIC_T2_03");
            state.Board.Tiles[5].Tower = tower;
            state.Board.Tiles[5].FireTurnsRemaining = TileState.OneTurnEffectDuration;

            var target = CreateMonster(1, 5, 100, 5);
            target.Shocked = true;
            var adjacent = CreateMonster(2, 6, 100, 6);
            var twoTilesAway = CreateMonster(3, 7, 100, 7);
            var fourTilesAway = CreateMonster(4, 9, 100, 9);
            state.Monsters.Add(target);
            state.Monsters.Add(adjacent);
            state.Monsters.Add(twoTilesAway);
            state.Monsters.Add(fourTilesAway);

            new GaeBullBing.Core.Towers.TowerEffectService().ResolveAfterAttacks(
                state,
                new[]
                {
                    new GaeBullBing.Core.Towers.TowerAttackResult(1, 1, 10f, false, targetTileIndex: 5)
                });

            Assert.That(state.Board.Tiles[3].FireTurnsRemaining, Is.GreaterThan(0));
            Assert.That(state.Board.Tiles[7].FireTurnsRemaining, Is.GreaterThan(0));
            Assert.That(adjacent.Shocked, Is.True);
            Assert.That(twoTilesAway.Shocked, Is.False);
            Assert.That(fourTilesAway.CurrentHealth, Is.EqualTo(100f));
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

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(target, value);
        }

        private sealed class FixedRandom : IDiceRandom
        {
            private readonly int value;
            public FixedRandom(int value) => this.value = value;
            public int Next(int maxExclusive) => value % maxExclusive;
        }
    }
}
