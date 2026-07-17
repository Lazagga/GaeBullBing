using System;
using System.Collections.Generic;
using GaeBullBing.Core.Data;

namespace GaeBullBing.Core.Monsters
{
    [Serializable]
    public sealed class DifficultyPatternData
    {
        public int RequiredKills;
        public float HealthMultiplier = 1f;
        public string[] MonsterIds = Array.Empty<string>();
    }

    public sealed class DifficultyState
    {
        public int KillCount { get; set; }
        public int Level { get; set; } = 1;
        public int PatternIndex { get; set; }
    }

    public sealed class MonsterDatabase
    {
        private readonly Dictionary<string, MonsterDefinition> definitions = new();

        public MonsterDatabase(IEnumerable<MonsterDefinition> source)
        {
            foreach (var definition in source)
                if (definition != null && !string.IsNullOrWhiteSpace(definition.Id))
                    definitions[definition.Id] = definition;
        }

        public MonsterDefinition Get(string id) =>
            definitions.TryGetValue(id, out var definition)
                ? definition
                : throw new KeyNotFoundException($"몬스터 ID를 찾을 수 없습니다: {id}");
    }

    public sealed class DifficultyService
    {
        private readonly DifficultyPatternData[] patterns;

        public DifficultyService(DifficultyPatternData[] patterns)
        {
            this.patterns = patterns ?? throw new ArgumentNullException(nameof(patterns));
            if (patterns.Length == 0)
                throw new ArgumentException("난이도 패턴이 하나 이상 필요합니다.", nameof(patterns));
            Array.Sort(this.patterns, (left, right) => left.RequiredKills.CompareTo(right.RequiredKills));
        }

        public void Reset(DifficultyState state)
        {
            state.KillCount = 0;
            state.Level = 1;
            state.PatternIndex = 0;
        }

        public void AddKills(DifficultyState state, int count)
        {
            state.KillCount += Math.Max(0, count);
            var nextLevel = Math.Min(patterns.Length, GetPatternIndex(state.KillCount) + 1);
            if (state.Level != nextLevel)
                state.PatternIndex = 0;
            state.Level = nextLevel;
        }

        public string GetNextMonsterId(DifficultyState state)
        {
            var patternIndex = GetPatternIndex(state.KillCount);
            var pattern = patterns[patternIndex];
            if (pattern.MonsterIds == null || pattern.MonsterIds.Length == 0)
                throw new InvalidOperationException($"난이도 {patternIndex + 1}의 몬스터 패턴이 비어 있습니다.");
            if (state.Level != patternIndex + 1)
            {
                state.Level = patternIndex + 1;
                state.PatternIndex = 0;
            }
            var id = pattern.MonsterIds[state.PatternIndex % pattern.MonsterIds.Length];
            state.PatternIndex = (state.PatternIndex + 1) % pattern.MonsterIds.Length;
            return id;
        }

        public float GetHealthMultiplier(DifficultyState state) =>
            patterns[GetPatternIndex(state.KillCount)].HealthMultiplier;

        private int GetPatternIndex(int kills)
        {
            var result = 0;
            for (var index = 1; index < patterns.Length; index++)
                if (kills >= patterns[index].RequiredKills) result = index;
            return result;
        }
    }
}
