#if UNITY_EDITOR
using GaeBullBing.Presentation.Game;
using GaeBullBing.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GaeBullBing.Editor
{
    public static class TileInfoPanelBuilder
    {
        [MenuItem("GaeBullBing/UI/Build Tile Information Panel")]
        public static void Build()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("Tile information panel requires a Canvas in the active scene.");
                return;
            }

            var old = canvas.transform.Find("Tile Information Panel");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            var root = CreateUiObject("Tile Information Panel", canvas.transform);
            Undo.RegisterCreatedObjectUndo(root, "Build Tile Information Panel");
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, .5f);
            rect.anchorMax = new Vector2(1f, .5f);
            rect.pivot = new Vector2(1f, .5f);
            rect.anchoredPosition = new Vector2(-24f, 0f);
            rect.sizeDelta = new Vector2(430f, 620f);

            var image = root.AddComponent<Image>();
            image.color = new Color(.055f, .065f, .09f, .96f);
            image.raycastTarget = false;
            var view = root.AddComponent<TileInfoPanelView>();

            var title = CreateText("Title", root.transform, 26, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -18f), new Vector2(-36f, 52f), new Vector2(0f, 1f));
            title.color = new Color(1f, .84f, .28f);

            var divider = CreateUiObject("Divider", root.transform);
            var dividerImage = divider.AddComponent<Image>();
            dividerImage.color = new Color(1f, 1f, 1f, .14f);
            dividerImage.raycastTarget = false;
            SetRect(divider.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -72f), new Vector2(-36f, 2f), new Vector2(0f, 1f));

            var tower = CreateText("Tower Information", root.transform, 18, FontStyle.Normal, TextAnchor.UpperLeft);
            tower.resizeTextForBestFit = true;
            tower.resizeTextMinSize = 12;
            tower.resizeTextMaxSize = 18;
            SetRect(tower.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -88f), new Vector2(-36f, 330f), new Vector2(0f, 1f));

            var monsterBox = CreateUiObject("Monster Background", root.transform);
            var monsterImage = monsterBox.AddComponent<Image>();
            monsterImage.color = new Color(0f, 0f, 0f, .22f);
            monsterImage.raycastTarget = false;
            SetRect(monsterBox.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(14f, 40f), new Vector2(-28f, 178f), new Vector2(0f, 0f));

            var monsters = CreateText("Monster Information", monsterBox.transform, 18, FontStyle.Normal, TextAnchor.UpperLeft);
            monsters.resizeTextForBestFit = true;
            monsters.resizeTextMinSize = 12;
            monsters.resizeTextMaxSize = 18;
            SetRect(monsters.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-24f, -24f), Vector2.zero);

            var hint = CreateText("Hint", root.transform, 14, FontStyle.Italic, TextAnchor.MiddleRight);
            hint.text = "다른 타일 선택 · 바깥 클릭으로 닫기";
            hint.color = new Color(1f, 1f, 1f, .55f);
            SetRect(hint.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(18f, 8f), new Vector2(-36f, 26f), new Vector2(0f, 0f));

            var serializedView = new SerializedObject(view);
            serializedView.FindProperty("panelRoot").objectReferenceValue = root;
            serializedView.FindProperty("titleText").objectReferenceValue = title;
            serializedView.FindProperty("towerText").objectReferenceValue = tower;
            serializedView.FindProperty("monsterText").objectReferenceValue = monsters;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            var controller = Object.FindFirstObjectByType<GameController>();
            if (controller != null)
            {
                var serializedController = new SerializedObject(controller);
                serializedController.FindProperty("tileInfoPanel").objectReferenceValue = view;
                serializedController.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(controller);
            }

            root.SetActive(false);
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log("Built the persistent Tile Information Panel UI.");
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var value = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            value.transform.SetParent(parent, false);
            value.layer = parent.gameObject.layer;
            return value;
        }

        private static Text CreateText(string name, Transform parent, int size, FontStyle style, TextAnchor anchor)
        {
            var value = CreateUiObject(name, parent);
            var text = value.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
#endif
