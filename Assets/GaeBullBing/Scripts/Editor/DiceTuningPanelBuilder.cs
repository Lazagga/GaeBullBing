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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
    }
}
#endif
