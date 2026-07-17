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

        public MonsterState Spawn(GameState state, MonsterDefinition definition)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var monster = new MonsterState
            {
                InstanceId = nextInstanceId++,
                DefinitionId = definition.Id,
                CurrentHealth = definition.MaxHp,
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
                var remainingToBase = BoardState.DefaultTileCount - monster.DistanceTravelled;
                var distance = Math.Min(monster.MoveDistance, remainingToBase);
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
    }
}
