#if UNITY_EDITOR
using GaeBullBing.Presentation.Monsters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GaeBullBing.Editor
{
    public static class MonsterArtSetup
    {
        private const string FoxFrontPath = "Assets/GaeBullBing/Art/Monsters/Fox_front.png";
        private const string FoxBackPath = "Assets/GaeBullBing/Art/Monsters/Fox_back.png";

        [MenuItem("GaeBullBing/Art/Configure Fox Monster")]
        public static void ConfigureFox()
        {
            ConfigureTexture(FoxFrontPath);
            ConfigureTexture(FoxBackPath);

            var frontSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FoxFrontPath);
            var backSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FoxBackPath);
            var presenter = Object.FindFirstObjectByType<MonsterPresenter>(FindObjectsInactive.Include);
            if (frontSprite == null || backSprite == null || presenter == null)
            {
                Debug.LogError("Fox front/back sprites or MonsterPresenter could not be loaded.");
                return;
            }

            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("foxFrontSprite").objectReferenceValue = frontSprite;
            serialized.FindProperty("foxBackSprite").objectReferenceValue = backSprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
            EditorSceneManager.MarkSceneDirty(presenter.gameObject.scene);
            Debug.Log("Configured MON_002 fox front/back art with the player pivot, scale, and ground offset.");
        }

        private static void ConfigureTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException($"Fox texture was not found: {path}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 600f;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(.5f, .06f);
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
#endif
