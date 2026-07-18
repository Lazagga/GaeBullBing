using System;
using System.Collections.Generic;
using GaeBullBing.Core.Board;
using GaeBullBing.Core.Data;
using GaeBullBing.Core.Game;

namespace GaeBullBing.Core.Monsters
{
    public readonly struct MonsterMoveResult
    {
        public MonsterMoveResult(int instanceId, int startTileIndex, int distance, bool reachedBase,
            bool isBoss = false, IReadOnlyList<BossFeatherEvent> featherEvents = null)
        {
            InstanceId = instanceId;
            StartTileIndex = startTileIndex;
            Distance = distance;
            ReachedBase = reachedBase;
            IsBoss = isBoss;
            FeatherEvents = featherEvents ?? Array.Empty<BossFeatherEvent>();
        }

        public int InstanceId { get; }
        public int StartTileIndex { get; }
        public int Distance { get; }
        public bool ReachedBase { get; }
        public bool IsBoss { get; }
        public IReadOnlyList<BossFeatherEvent> FeatherEvents { get; }

        public MonsterMoveResult WithFeatherEvents(IReadOnlyList<BossFeatherEvent> events) =>
            new(InstanceId, StartTileIndex, Distance, ReachedBase, IsBoss, events);
    }

    public enum BossFeatherEventType { Drop, Recover }

    public readonly struct BossFeatherEvent
    {
        public BossFeatherEvent(BossFeatherEventType type, int tileIndex, int stepOffset)
        {
            Type = type;
            TileIndex = tileIndex;
            StepOffset = stepOffset;
        }

        public BossFeatherEventType Type { get; }
        public int TileIndex { get; }
        public int StepOffset { get; }
    }

    public sealed class MonsterService
    {
        private int nextInstanceId = 1;

        public MonsterState Spawn(GameState state, MonsterDefinition definition, float healthMultiplier = 1f)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var maxHealth = Math.Max(1f, definition.MaxHp *
                (definition.Tier == MonsterTier.Boss ? 1f : healthMultiplier));
            var monster = new MonsterState
            {
                InstanceId = nextInstanceId++,
                DefinitionId = definition.Id,
                Tier = definition.Tier,
                CurrentHealth = maxHealth,
                MaxHealth = maxHealth,
                BaseDefense = definition.BaseDefense,
                CurrentTileIndex = 0,
                MoveDistance = definition.MoveDistance,
                DistanceTravelled = 0
            };
            state.Monsters.Add(monster);
            return monster;
        }

        public IReadOnlyList<MonsterMoveResult> MoveAll(GameState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            state.Monsters.Sort((left, right) => right.DistanceTravelled.CompareTo(left.DistanceTravelled));
            var results = new List<MonsterMoveResult>(state.Monsters.Count);
            var reachedBase = new List<MonsterState>();

            foreach (var monster in state.Monsters)
            {
                var currentLine = GetLine(monster.CurrentTileIndex);
                if (monster.FreezeImmuneLine >= 0 && monster.FreezeImmuneLine != currentLine)
                    monster.FreezeImmuneLine = -1;
                var remainingToBase = BoardState.DefaultTileCount - monster.DistanceTravelled;
                var cannotMove = monster.FrozenMovesRemaining > 0 || monster.StunnedMovesRemaining > 0;
                if (monster.FrozenMovesRemaining > 0) { monster.FrozenMovesRemaining--; monster.FreezeImmuneLine = currentLine; }
                if (monster.StunnedMovesRemaining > 0) monster.StunnedMovesRemaining--;
                var startTileIndex = monster.CurrentTileIndex;
                monster.TouchedFireThisMove = false;
                if (monster.IsBoss)
                {
                    var bossDistance = cannotMove ? 0 : Math.Min(monster.MoveDistance, remainingToBase);
                    monster.DistanceTravelled += bossDistance;
                    monster.CurrentTileIndex = (monster.CurrentTileIndex + bossDistance) %
                        BoardState.DefaultTileCount;
                    var bossReachedBase = monster.DistanceTravelled >= BoardState.DefaultTileCount;
                    results.Add(new MonsterMoveResult(monster.InstanceId, startTileIndex, bossDistance,
                        bossReachedBase, true));
                    if (bossReachedBase) reachedBase.Add(monster);
                    continue;
                }
                ApplyFireTile(state, monster, monster.CurrentTileIndex);

                var onIce = HasIce(state, monster.CurrentTileIndex);
                var plannedDistance = cannotMove ? 0 : Math.Min(onIce ? 1 : monster.MoveDistance, remainingToBase);
                var distance = 0;
                while (distance < plannedDistance)
                {
                    distance++;
                    var enteredTileIndex = (startTileIndex + distance) % BoardState.DefaultTileCount;
                    ApplyFireTile(state, monster, enteredTileIndex);
                    if (TriggersPhysicsGuard(state, monster, enteredTileIndex))
                    {
                        plannedDistance = distance;
                        break;
                    }
                    if (HasIce(state, enteredTileIndex) && distance < plannedDistance)
                        plannedDistance = Math.Min(plannedDistance, distance + 1);
                }

                monster.DistanceTravelled += distance;
                monster.CurrentTileIndex = (monster.CurrentTileIndex + distance) % BoardState.DefaultTileCount;
                var hasReachedBase = monster.DistanceTravelled >= BoardState.DefaultTileCount;
                results.Add(new MonsterMoveResult(monster.InstanceId, startTileIndex, distance, hasReachedBase));

                if (hasReachedBase)
                    reachedBase.Add(monster);
            }

            foreach (var monster in reachedBase)
                state.Monsters.Remove(monster);

            return results;
        }

        private static bool HasIce(GameState state, int tileIndex) =>
            tileIndex >= 0 && tileIndex < state.Board.Tiles.Count &&
            state.Board.Tiles[tileIndex].IceTurnsRemaining > 0;

        private static bool TriggersPhysicsGuard(GameState state, MonsterState monster, int tileIndex)
        {
            if (monster.IsBoss || monster.PhysicsGuardConsumed || tileIndex < 0 ||
                tileIndex >= state.Board.Tiles.Count)
                return false;

            var tower = state.Board.Tiles[tileIndex].Tower;
            if (tower == null || !tower.AppliedUpgradeIds.Contains("UPG_PHYSICS_T3_00"))
                return false;

            monster.PhysicsGuardConsumed = true;
            monster.PhysicsGuardTriggeredThisTurn = true;
            return true;
        }

        private static void ApplyFireTile(GameState state, MonsterState monster, int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= state.Board.Tiles.Count ||
                state.Board.Tiles[tileIndex].FireTurnsRemaining <= 0)
                return;
            monster.BurnStacks++;
            monster.TouchedFireThisMove = true;
        }

        public static int GetLine(int tileIndex)
        {
            if (tileIndex <= 9) return 0;
            if (tileIndex <= 18) return 1;
            if (tileIndex <= 27) return 2;
            return 3;
        }
    }
}
