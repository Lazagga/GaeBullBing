#if UNITY_EDITOR
using GaeBullBing.Presentation.Monsters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GaeBullBing.Editor
{
    public static class MonsterArtSetup
    {
        private const string FoxPath = "Assets/GaeBullBing/Art/Monsters/Fox.png";

        [MenuItem("GaeBullBing/Art/Configure Fox Monster")]
        public static void ConfigureFox()
        {
            var importer = AssetImporter.GetAtPath(FoxPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Fox texture was not found: {FoxPath}");
                return;
            }

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

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FoxPath);
            var presenter = Object.FindFirstObjectByType<MonsterPresenter>(FindObjectsInactive.Include);
            if (sprite == null || presenter == null)
            {
                Debug.LogError("Fox sprite or MonsterPresenter could not be loaded.");
                return;
            }

            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("foxSprite").objectReferenceValue = sprite;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
            EditorSceneManager.MarkSceneDirty(presenter.gameObject.scene);
            Debug.Log("Configured MON_002 fox art with the player pivot, scale, and ground offset.");
        }
    }
}
#endif
