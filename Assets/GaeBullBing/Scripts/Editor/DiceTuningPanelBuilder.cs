#if UNITY_EDITOR
using GaeBullBing.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GaeBullBing.Editor
{
    public static class DiceTuningPanelBuilder
    {
        private const string UiFontPath = "Assets/Fonts/NanumDongHwaDdoBag.ttf";

        [MenuItem("GaeBullBing/UI/Build Fixed Dice Tuning UI")]
        public static void BuildFixedDiceTuningUI()
        {
            var view = Object.FindFirstObjectByType<DiceTuningView>(FindObjectsInactive.Include);
            if (view == null) { Debug.LogError("DiceTuningView was not found."); return; }

            var old = view.transform.Find("Dice Tuning 3D");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            var displayController = view.GetComponent<GaeBullBing.Presentation.Dice.DiceTuning3DDisplay>();
            if (displayController == null)
                displayController = Undo.AddComponent<GaeBullBing.Presentation.Dice.DiceTuning3DDisplay>(view.gameObject);

            var root = CreateRect("Dice Tuning 3D", view.transform, Vector2.zero, new Vector2(620f, 330f));
            var displayObject = CreateRect("Display", root.transform, new Vector2(0f, -15f), new Vector2(620f, 330f),
                typeof(CanvasRenderer), typeof(RawImage));
            var display = displayObject.GetComponent<RawImage>();
            display.raycastTarget = false;

            var selectors = new Button[2];
            for (var i = 0; i < 2; i++)
            {
                var x = i == 0 ? -120f : 120f;
                selectors[i] = CreateButton($"Select Dice {i + 1}", root.transform,
                    new Vector2(x, -15f), new Vector2(220f, 280f), string.Empty, true);
            }

            var arrows = new[]
            {
                CreateButton("Rotate Up", root.transform, new Vector2(0f,135f), new Vector2(56f,44f), "▲"),
                CreateButton("Rotate Down", root.transform, new Vector2(0f,-150f), new Vector2(56f,44f), "▼"),
                CreateButton("Rotate Left", root.transform, new Vector2(-175f,-10f), new Vector2(56f,44f), "◀"),
                CreateButton("Rotate Right", root.transform, new Vector2(175f,-10f), new Vector2(56f,44f), "▶")
            };

            var controllerData = new SerializedObject(displayController);
            controllerData.FindProperty("visualRoot").objectReferenceValue = root;
            controllerData.FindProperty("display").objectReferenceValue = display;
            AssignArray(controllerData.FindProperty("selectors"), selectors);
            AssignArray(controllerData.FindProperty("arrows"), arrows);
            controllerData.ApplyModifiedPropertiesWithoutUndo();

            var viewData = new SerializedObject(view);
            viewData.FindProperty("display3D").objectReferenceValue = displayController;
            viewData.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(displayController);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            Debug.Log("Built and connected fixed Dice Tuning UI objects.");
        }

        [MenuItem("GaeBullBing/UI/Add Dice Tuning Back Button")]
        public static void Build()
        {
            var view = Object.FindFirstObjectByType<DiceTuningView>(FindObjectsInactive.Include);
            if (view == null)
            {
                Debug.LogError("DiceTuningView was not found in the active scene.");
                return;
            }

            var old = view.transform.Find("Back");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            var buttonObject = new GameObject("Back", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(buttonObject, "Add Dice Tuning Back Button");
            buttonObject.transform.SetParent(view.transform, false);
            buttonObject.layer = view.gameObject.layer;
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(130f, 42f);
            rect.anchoredPosition = new Vector2(0f, -88f);
            buttonObject.GetComponent<Image>().color = new Color(.28f, .31f, .38f, 1f);

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(buttonObject.transform, false);
            textObject.layer = buttonObject.layer;
            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            var text = textObject.GetComponent<Text>();
            text.font = LoadUiFont();
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "뒤로가기";
            text.raycastTarget = false;

            var serialized = new SerializedObject(view);
            serialized.FindProperty("backButton").objectReferenceValue = buttonObject.GetComponent<Button>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            buttonObject.SetActive(false);
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            Debug.Log("Added and connected the Dice Tuning back button.");
        }

        private static GameObject CreateRect(string name, Transform parent, Vector2 position, Vector2 size,
            params System.Type[] extraComponents)
        {
            var components = new System.Collections.Generic.List<System.Type> { typeof(RectTransform) };
            components.AddRange(extraComponents);
            var value = new GameObject(name, components.ToArray());
            Undo.RegisterCreatedObjectUndo(value, $"Create {name}");
            value.transform.SetParent(parent, false);
            value.layer = parent.gameObject.layer;
            var rect = value.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return value;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 position, Vector2 size,
            string label, bool transparent = false)
        {
            var value = CreateRect(name, parent, position, size, typeof(CanvasRenderer), typeof(Image), typeof(Button));
            value.GetComponent<Image>().color = transparent
                ? new Color(1f, 1f, 1f, .001f)
                : new Color(.24f, .28f, .36f, .96f);
            if (!string.IsNullOrEmpty(label))
            {
                var textObject = CreateRect("Text", value.transform, Vector2.zero, size, typeof(CanvasRenderer), typeof(Text));
                var text = textObject.GetComponent<Text>();
                text.font = LoadUiFont();
                text.fontSize = 26;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.raycastTarget = false;
                text.text = label;
            }
            return value.GetComponent<Button>();
        }

        private static Font LoadUiFont() =>
            AssetDatabase.LoadAssetAtPath<Font>(UiFontPath) ??
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        private static void AssignArray<T>(SerializedProperty property, T[] values) where T : Object
        {
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
#endif
