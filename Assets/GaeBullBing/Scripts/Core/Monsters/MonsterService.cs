using System;
using System.Collections.Generic;
using GaeBullBing.Core.Board;
using GaeBullBing.Core.Data;
using GaeBullBing.Core.Game;

namespace GaeBullBing.Core.Monsters
{
    public readonly struct MonsterMoveResult
    {
        public MonsterMoveResult(int instanceId, int startTileIndex, int distance, bool reachedBase)
        {
            InstanceId = instanceId;
            StartTileIndex = startTileIndex;
            Distance = distance;
            ReachedBase = reachedBase;
        }

        public int InstanceId { get; }
        public int StartTileIndex { get; }
        public int Distance { get; }
        public bool ReachedBase { get; }
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

            var maxHealth = Math.Max(1, (int)Math.Ceiling(definition.MaxHp * healthMultiplier));
            var monster = new MonsterState
            {
                InstanceId = nextInstanceId++,
                DefinitionId = definition.Id,
                CurrentHealth = maxHealth,
                MaxHealth = maxHealth,
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
                var onIce = state.Board.Tiles.Count > monster.CurrentTileIndex && state.Board.Tiles[monster.CurrentTileIndex].IceTurnsRemaining > 0;
                var moveDistance = onIce ? 1 : monster.MoveDistance;
                var distance = cannotMove ? 0 : Math.Min(moveDistance, remainingToBase);
                var startTileIndex = monster.CurrentTileIndex;
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

        public static int GetLine(int tileIndex)
        {
            if (tileIndex <= 9) return 0;
            if (tileIndex <= 18) return 1;
            if (tileIndex <= 27) return 2;
            return 3;
        }
    }
}
