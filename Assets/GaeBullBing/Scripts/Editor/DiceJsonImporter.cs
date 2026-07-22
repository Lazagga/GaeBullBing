using System;
using System.Collections.Generic;
using System.IO;
using GaeBullBing.Core.Data;
using UnityEditor;
using UnityEngine;

namespace GaeBullBing.Editor
{
    public static class DiceJsonImporter
    {
        public const string JsonPath = "Assets/GaeBullBing/Data/Json/Dice.json";
        public const string OutputFolder = "Assets/GaeBullBing/Data/Dice";

        [MenuItem("GaeBullBing/Data/Import Dice JSON")]
        public static void Import()
        {
            var jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(JsonPath);
            if (jsonAsset == null)
                throw new FileNotFoundException(JsonPath);

            var database = JsonUtility.FromJson<DiceDatabaseJson>(jsonAsset.text);
            if (database?.dice == null)
                throw new InvalidOperationException("dice 배열이 없습니다.");

            EnsureFolder();
            var importedIds = new HashSet<string>();
            foreach (var source in database.dice)
            {
                Validate(source, importedIds);
                if (!Enum.TryParse(source.grade, true, out DiceGrade grade))
                    throw new InvalidOperationException($"{source.id}의 grade 값이 올바르지 않습니다: {source.grade}");
                if (!ColorUtility.TryParseHtmlString(source.color, out var color))
                    throw new InvalidOperationException($"{source.id}의 color 값이 올바르지 않습니다: {source.color}");

                var definition = FindOrCreate(source.id);
                var serialized = new SerializedObject(definition);
                serialized.FindProperty("id").stringValue = source.id;
                serialized.FindProperty("displayName").stringValue = source.name;
                serialized.FindProperty("grade").enumValueIndex = (int)grade;
                SetFaces(serialized.FindProperty("faces"), source.faces);
                serialized.FindProperty("color").colorValue = color;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                definition.name = source.id;
                EditorUtility.SetDirty(definition);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"주사위 JSON 임포트 완료: {database.dice.Length}개 갱신");
        }

        private static void Validate(DiceJson source, ISet<string> ids)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.id) || !ids.Add(source.id))
                throw new InvalidOperationException($"비어 있거나 중복된 주사위 ID입니다: {source?.id}");
            if (string.IsNullOrWhiteSpace(source.name))
                throw new InvalidOperationException($"{source.id}의 name이 비어 있습니다.");
            if (source.faces == null || source.faces.Length != 6)
                throw new InvalidOperationException($"{source.id}의 faces는 정확히 6개여야 합니다.");
            foreach (var face in source.faces)
                if (face < 1 || face > 6)
                    throw new InvalidOperationException($"{source.id}의 주사위 눈은 1~6이어야 합니다: {face}");
        }

        private static DiceDefinition FindOrCreate(string id)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:DiceDefinition", new[] { OutputFolder }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<DiceDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && asset.Id == id)
                    return asset;
            }

            var created = ScriptableObject.CreateInstance<DiceDefinition>();
            AssetDatabase.CreateAsset(created, $"{OutputFolder}/{id}.asset");
            return created;
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder(OutputFolder)) return;
            Directory.CreateDirectory(OutputFolder);
            AssetDatabase.Refresh();
        }

        private static void SetFaces(SerializedProperty property, int[] faces)
        {
            property.arraySize = faces.Length;
            for (var index = 0; index < faces.Length; index++)
                property.GetArrayElementAtIndex(index).intValue = faces[index];
        }

        [Serializable]
        private sealed class DiceDatabaseJson
        {
            public DiceJson[] dice;
        }

        [Serializable]
        private sealed class DiceJson
        {
            public string id;
            public string name;
            public string grade;
            public int[] faces;
            public string color;
            // passive는 형식에만 존재하며 현재 단계에서는 의도적으로 파싱하지 않는다.
        }
    }

    public sealed class DiceJsonAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
                if (path == DiceJsonImporter.JsonPath)
                {
                    DiceJsonImporter.Import();
                    return;
                }
        }
    }
}