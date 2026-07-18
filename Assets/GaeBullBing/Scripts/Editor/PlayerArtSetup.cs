#if UNITY_EDITOR
using GaeBullBing.Presentation.Board;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GaeBullBing.Editor
{
    public static class PlayerArtSetup
    {
        private const string FrontPath = "Assets/GaeBullBing/Art/Characters/PlayerChar_front.png";
        private const string BackPath = "Assets/GaeBullBing/Art/Characters/PlayerChar_back.png";

        [MenuItem("GaeBullBing/Art/Configure Player Direction Sprites")]
        public static void Configure()
        {
            ConfigureTexture(FrontPath);
            ConfigureTexture(BackPath);
            var front = AssetDatabase.LoadAssetAtPath<Sprite>(FrontPath);
            var back = AssetDatabase.LoadAssetAtPath<Sprite>(BackPath);
            var player = Object.FindFirstObjectByType<PlayerBoardView>(FindObjectsInactive.Include);
            if (player == null || front == null || back == null)
            {
                Debug.LogError("Player direction sprites or PlayerBoardView were not found.");
                return;
            }

            var data = new SerializedObject(player);
            data.FindProperty("frontSprite").objectReferenceValue = front;
            data.FindProperty("backSprite").objectReferenceValue = back;
            data.ApplyModifiedPropertiesWithoutUndo();
            var renderer = player.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.sprite = front;
            EditorUtility.SetDirty(player);
            if (renderer != null) EditorUtility.SetDirty(renderer);
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            Debug.Log("Configured front/back player sprites with pivot (0.5, 0.06) and PPU 600.");
        }

        private static void ConfigureTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(.5f, .06f);
            settings.spritePixelsPerUnit = 600f;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            importer.SetTextureSettings(settings);
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}
#endif
