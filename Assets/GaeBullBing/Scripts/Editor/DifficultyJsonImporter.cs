#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using GaeBullBing.Core.Data;
using GaeBullBing.Core.Monsters;
using UnityEditor;
using UnityEngine;

namespace GaeBullBing.Editor
{
    public static class DifficultyJsonImporter
    {
        public const string JsonPath = "Assets/GaeBullBing/Data/Json/Pattern.json";
        public const string RuntimeDatabasePath =
            "Assets/Resources/GaeBullBing/DifficultyDatabase.asset";

        [MenuItem("GaeBullBing/Data/Import Difficulty JSON")]
        public static void Import()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonPath);
            if (json == null) throw new FileNotFoundException(JsonPath);
            var source = JsonUtility.FromJson<DifficultyDatabaseJson>(json.text);
            if (source?.wavedata == null || source.wavedata.Length == 0)
                throw new InvalidOperationException("Pattern.json의 wavedata가 비어 있습니다.");
            if (source.wave_patterns == null || source.wave_patterns.Length == 0)
                throw new InvalidOperationException("Pattern.json의 wave_patterns가 비어 있습니다.");

            var monsterDatabase = AssetDatabase.LoadAssetAtPath<MonsterDatabaseDefinition>(
                MonsterJsonImporter.RuntimeDatabasePath);
            if (monsterDatabase == null || monsterDatabase.Monsters == null ||
                monsterDatabase.Monsters.Length == 0)
                throw new InvalidOperationException(
                    "MonsterDatabase가 없습니다. Monster.json을 먼저 임포트하세요.");
            var monsterIds = new HashSet<string>();
            foreach (var monster in monsterDatabase.Monsters)
                if (monster != null) monsterIds.Add(monster.Id);

            Array.Sort(source.wave_patterns, (left, right) => left.level.CompareTo(right.level));
            var common = source.wavedata[0];
            var killsPerLevel = Mathf.Max(1, common.required_kills);
            var healthMultiplier = common.multiplier > 0f ? common.multiplier : 1f;
            var patterns = new List<DifficultyPatternData>(source.wave_patterns.Length);
            var requiredKills = 0;
            var patternHealth = 1f;
            foreach (var pattern in source.wave_patterns)
            {
                if (pattern?.spawn_pattern == null || pattern.spawn_pattern.Length == 0)
                    throw new InvalidOperationException(
                        $"Pattern.json level {pattern?.level ?? 0}의 spawn_pattern이 비어 있습니다.");
                foreach (var monsterId in pattern.spawn_pattern)
                    if (!monsterIds.Contains(monsterId))
                        throw new InvalidOperationException(
                            $"Pattern.json이 존재하지 않는 몬스터를 참조합니다: {monsterId}");
                patterns.Add(new DifficultyPatternData
                {
                    RequiredKills = requiredKills,
                    HealthMultiplier = patternHealth,
                    MonsterIds = pattern.spawn_pattern
                });
                requiredKills += killsPerLevel;
                patternHealth *= healthMultiplier;
            }

            var directory = Path.GetDirectoryName(RuntimeDatabasePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }
            var database = AssetDatabase.LoadAssetAtPath<DifficultyDatabaseDefinition>(
                RuntimeDatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<DifficultyDatabaseDefinition>();
                AssetDatabase.CreateAsset(database, RuntimeDatabasePath);
            }
            var serialized = new SerializedObject(database);
            var patternProperty = serialized.FindProperty("patterns");
            patternProperty.arraySize = patterns.Count;
            for (var index = 0; index < patterns.Count; index++)
            {
                var element = patternProperty.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("RequiredKills").intValue =
                    patterns[index].RequiredKills;
                element.FindPropertyRelative("HealthMultiplier").floatValue =
                    patterns[index].HealthMultiplier;
                var ids = element.FindPropertyRelative("MonsterIds");
                ids.arraySize = patterns[index].MonsterIds.Length;
                for (var idIndex = 0; idIndex < patterns[index].MonsterIds.Length; idIndex++)
                    ids.GetArrayElementAtIndex(idIndex).stringValue =
                        patterns[index].MonsterIds[idIndex];
            }
            serialized.FindProperty("killsPerLevel").intValue = killsPerLevel;
            serialized.FindProperty("healthMultiplierPerLevel").floatValue = healthMultiplier;
            serialized.FindProperty("defensePerLevel").floatValue =
                Mathf.Max(0f, common.defense_per_wave);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"Difficulty JSON 임포트 완료: {patterns.Count}개 패턴");
        }

        [Serializable] private sealed class DifficultyDatabaseJson
        {
            public DifficultyCommonJson[] wavedata;
            public DifficultyPatternJson[] wave_patterns;
        }
        [Serializable] private sealed class DifficultyCommonJson
        {
            public int required_kills;
            public float multiplier;
            public float defense_per_wave;
        }
        [Serializable] private sealed class DifficultyPatternJson
        {
            public int level;
            public string[] spawn_pattern;
        }
    }

    public sealed class DifficultyJsonAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] imported,
            string[] deleted,
            string[] moved,
            string[] movedFrom)
        {
            foreach (var path in imported)
                if (path == DifficultyJsonImporter.JsonPath)
                {
                    DifficultyJsonImporter.Import();
                    return;
                }
        }
    }
}
#endif
