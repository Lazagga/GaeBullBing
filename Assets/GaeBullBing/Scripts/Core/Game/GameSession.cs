using System;
using GaeBullBing.Core.Board;
using GaeBullBing.Core.Dice;
using GaeBullBing.Core.Data;
using GaeBullBing.Core.Monsters;
using GaeBullBing.Core.Towers;

namespace GaeBullBing.Core.Game
{
    public sealed class GameSession
    {
        private readonly BoardService boardService;
        private readonly PlayerMovementService movementService;
        private readonly WeightedDiceRoller diceRoller;
        private readonly MonsterService monsterService;
        private readonly TowerService towerService;
        private readonly TowerCombatService towerCombatService;
        private readonly TowerEffectService towerEffectService = new();
        private int[] nextDiceResults;

        public GameSession(
            GameState state,
            BoardService boardService,
            PlayerMovementService movementService,
            WeightedDiceRoller diceRoller,
            MonsterService monsterService,
            TowerService towerService,
            TowerCombatService towerCombatService)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            this.boardService = boardService ?? throw new ArgumentNullException(nameof(boardService));
            this.movementService = movementService ?? throw new ArgumentNullException(nameof(movementService));
            this.diceRoller = diceRoller ?? throw new ArgumentNullException(nameof(diceRoller));
            this.monsterService = monsterService ?? throw new ArgumentNullException(nameof(monsterService));
            this.towerService = towerService ?? throw new ArgumentNullException(nameof(towerService));
            this.towerCombatService = towerCombatService ?? throw new ArgumentNullException(nameof(towerCombatService));
        }

        public GameState State { get; }

        public void StartNewGame(int diceCount = 2, BoardDefinition boardDefinition = null)
        {
            if (diceCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(diceCount));

            if (boardDefinition != null)
                boardService.Initialize(State.Board, boardDefinition);
            else
                boardService.Initialize(State.Board);
            State.Dice.Clear();
            for (var index = 0; index < diceCount; index++)
                State.Dice.Add(new DiceState());

            State.Player.CurrentTileIndex = 0;
            State.Round = 1;
            State.EscapedMonsterCount = 0;
            State.PermanentTowerDamageBonuses.Clear();
            State.EscapeLimit = GameState.DefaultEscapeLimit;
            State.CurrentPhase = TurnPhase.PlayerTurnStart;
            State.LastDiceResults.Clear();
            State.LastMoveDistance = 0;
            nextDiceResults = null;
        }

        public void SetNextDiceResults(int first, int second)
        {
            if (first < 1 || first > 6 || second < 1 || second > 6)
                throw new ArgumentOutOfRangeException("Dice values must be between 1 and 6.");
            nextDiceResults = new[] { first, second };
        }

        public int RollDiceAndMovePlayer()
        {
            if (State.Board.TileCount == 0 || State.Dice.Count == 0)
                throw new InvalidOperationException("Start a game before rolling dice.");

            State.CurrentPhase = TurnPhase.DiceRoll;
            State.LastDiceResults.Clear();

            var distance = 0;
            for (var index = 0; index < State.Dice.Count; index++)
            {
                var result = nextDiceResults != null && index < nextDiceResults.Length
                    ? nextDiceResults[index]
                    : diceRoller.Roll(State.Dice[index]);
                State.LastDiceResults.Add(result);
                distance += result;
            }
            nextDiceResults = null;

            State.LastMoveDistance = distance;
            State.CurrentPhase = TurnPhase.PlayerMove;
            movementService.Move(State.Player, State.Board, distance);
            State.CurrentPhase = TurnPhase.TileResolve;
            return distance;
        }

        public System.Collections.Generic.IReadOnlyList<MonsterMoveResult> MoveMonsters()
        {
            State.CurrentPhase = TurnPhase.MonsterMove;
            var results = monsterService.MoveAll(State);
            foreach (var result in results)
                if (result.ReachedBase)
                    State.EscapedMonsterCount++;

            if (State.EscapedMonsterCount >= State.EscapeLimit)
                State.CurrentPhase = TurnPhase.Defeat;
            return results;
        }

        public MonsterState SpawnMonster(MonsterDefinition definition, float healthMultiplier = 1f)
        {
            State.CurrentPhase = TurnPhase.MonsterSpawn;
            return monsterService.Spawn(State, definition, healthMultiplier);
        }

        public void CompleteRound()
        {
            if (State.IsGameOver)
                return;
            State.CurrentPhase = TurnPhase.RoundEnd;
            State.Round++;
            State.CurrentPhase = TurnPhase.PlayerTurnStart;
        }

        public TowerState BuildTower(int tileIndex, string definitionId)
        {
            State.CurrentPhase = TurnPhase.TowerResolve;
            return towerService.Build(State.Board.Tiles[tileIndex], definitionId);
        }

        public TowerState UpgradeTower(int tileIndex, TowerUpgradeDefinition upgrade)
        {
            State.CurrentPhase = TurnPhase.TowerResolve;
            return towerService.Upgrade(State.Board.Tiles[tileIndex], upgrade.Id, upgrade.Tier);
        }

        public System.Collections.Generic.IReadOnlyList<TowerAttackResult> ResolveTowerCombat(
            System.Collections.Generic.IReadOnlyList<TowerDefinition> definitions,
            System.Collections.Generic.IReadOnlyList<TowerUpgradeDefinition> upgrades)
        {
            State.CurrentPhase = TurnPhase.TowerCombat;
            var stats = new System.Collections.Generic.Dictionary<int, TowerCombatStats>();
            foreach (var tile in State.Board.Tiles)
            {
                if (!tile.HasTower) continue;
                TowerDefinition definition = null;
                foreach (var candidate in definitions)
                    if (candidate != null && candidate.Id == tile.Tower.DefinitionId) { definition = candidate; break; }
                if (definition != null)
                    stats[tile.Tower.InstanceId] = BuildCombatStats(definition, tile.Tower, upgrades);
            }
            var attacks = towerCombatService.ResolveByTower(State, stats);
            var effects = towerEffectService.ResolveAfterAttacks(State, attacks);
            var combined = new System.Collections.Generic.List<TowerAttackResult>(attacks);
            combined.AddRange(effects);
            return combined;
        }

        public System.Collections.Generic.IReadOnlyList<TowerAttackResult> ResolveMonsterTurnEndEffects() =>
            towerEffectService.ResolveMonsterTurnEnd(State);

        private TowerCombatStats BuildCombatStats(
            TowerDefinition definition,
            TowerState tower,
            System.Collections.Generic.IReadOnlyList<TowerUpgradeDefinition> upgrades)
        {
            var damageAdd = 0f; var damageMultiply = 1f;
            var rangeAdd = 0f; var rangeMultiply = 1f;
            var targetAdd = 0f; var targetMultiply = 1f;
            var attackAdd = 0f; var attackMultiply = 1f;
                foreach (var id in tower.AppliedUpgradeIds)
                    foreach (var upgrade in upgrades)
                        if (upgrade != null && upgrade.Id == id)
                            foreach (var modifier in upgrade.StatModifiers)
                            {
                                if (upgrade.Id == "UPG_ICE_T3_02" && modifier.Stat.Equals("damage", StringComparison.OrdinalIgnoreCase) && modifier.Operation.Equals("Multiply", StringComparison.OrdinalIgnoreCase)) continue;
                                var multiply = string.Equals(modifier.Operation, "Multiply", StringComparison.OrdinalIgnoreCase);
                                switch (modifier.Stat.ToLowerInvariant())
                                {
                                    case "damage": if (multiply) damageMultiply *= modifier.Value; else damageAdd += modifier.Value; break;
                                    case "range": if (multiply) rangeMultiply *= modifier.Value; else rangeAdd += modifier.Value; break;
                                    case "target_count": if (multiply) targetMultiply *= modifier.Value; else targetAdd += modifier.Value; break;
                                    case "attack_count": if (multiply) attackMultiply *= modifier.Value; else attackAdd += modifier.Value; break;
                                }
                            }
            return new TowerCombatStats(
                Math.Max(0, (int)Math.Round((definition.Damage + State.GetPermanentTowerDamageBonus(definition.Element) + damageAdd) * damageMultiply)),
                Math.Max(0, (int)Math.Round((definition.Range + rangeAdd) * rangeMultiply)),
                Math.Max(1, (int)Math.Round((definition.TargetCount + targetAdd) * targetMultiply)),
                Math.Max(1, (int)Math.Round((definition.AttackCount + attackAdd) * attackMultiply)));
        }

        public void AddPermanentTowerDamageBonus(TowerElement element, int amount) =>
            State.AddPermanentTowerDamageBonus(element, amount);

        public void TeleportPlayer(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= State.Board.TileCount) throw new ArgumentOutOfRangeException(nameof(tileIndex));
            State.Player.CurrentTileIndex = tileIndex;
        }
    }
}
