using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GaeBullBing.Presentation.Dice
{
    public sealed class DiceTuning3DDisplay : MonoBehaviour
    {
        private const float StageOffset = 2000f;
        private readonly Vector3 center = new(StageOffset, 0f, StageOffset);
        private readonly Vector3[] normals =
        {
            Vector3.forward, Vector3.right, Vector3.up,
            Vector3.down, Vector3.left, Vector3.back
        };

        private RawImage display;
        private GameObject visualRoot;
        private RenderTexture texture;
        private UnityEngine.Camera renderCamera;
        private DieView[] dice;
        private Button[] selectors;
        private Text[] names;
        private Button[] arrows;
        private Material whiteMaterial;
        private Material blackMaterial;
        private Material blackPipMaterial;
        private Material whitePipMaterial;
        private bool isRotating;
        private int selectedDice = -1;
        private int[][] values;
        private Action<int> selected;
        private Action faceChanged;

        public int VisibleFaceValue => selectedDice < 0 ? 0 :
            values[selectedDice][GetVisibleFaceIndex(dice[selectedDice].Transform.rotation)];

        public void Initialize(RectTransform parent)
        {
            if (display != null) return;
            CreateDisplay(parent);
            CreateStage();
            CreateControls(parent);
            visualRoot.SetActive(false);
        }

        public void ShowSelection(int[][] physicalValues, Action<int> onSelected)
        {
            values = physicalValues;
            selected = onSelected;
            selectedDice = -1;
            visualRoot.SetActive(true);
            display.gameObject.SetActive(true);
            for (var i = 0; i < 2; i++)
            {
                dice[i].Transform.gameObject.SetActive(i < values.Length);
                selectors[i].gameObject.SetActive(i < values.Length);
                selectors[i].transform.SetAsLastSibling();
                names[i].gameObject.SetActive(false);
                dice[i].Transform.position = center + new Vector3(i == 0 ? -1.65f : 1.65f, 0f, 0f);
                dice[i].Transform.rotation = GetLargestFaceForwardRotation(values[i]);
                SetFaceValues(dice[i], values[i]);
            }
            SetArrows(false);
        }

        public void ShowFaceSelection(int diceIndex, Action onFaceChanged)
        {
            selectedDice = diceIndex;
            faceChanged = onFaceChanged;
            for (var i = 0; i < dice.Length; i++)
            {
                dice[i].Transform.gameObject.SetActive(i == diceIndex);
                selectors[i].gameObject.SetActive(false);
                names[i].gameObject.SetActive(false);
            }
            dice[diceIndex].Transform.position = center;
            SetArrows(true);
            faceChanged?.Invoke();
        }

        public void HideDisplay()
        {
            if (visualRoot != null) visualRoot.SetActive(false);
        }

        private void Rotate(Vector3 axis, float angle)
        {
            if (selectedDice < 0 || isRotating) return;
            StartCoroutine(RotateRoutine(axis, angle));
        }

        private IEnumerator RotateRoutine(Vector3 axis, float angle)
        {
            isRotating = true;
            SetArrowInteractable(false);
            var die = dice[selectedDice].Transform;
            var start = die.rotation;
            var target = Quaternion.AngleAxis(angle, axis) * start;
            const float duration = .24f;
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var progress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                die.rotation = Quaternion.Slerp(start, target, progress);
                yield return null;
            }
            die.rotation = target;
            faceChanged?.Invoke();
            SetArrowInteractable(true);
            isRotating = false;
        }

        private int GetVisibleFaceIndex(Quaternion rotation)
        {
            var best = 0;
            var bestScore = float.MaxValue;
            for (var i = 0; i < normals.Length; i++)
            {
                var score = (rotation * normals[i]).z;
                if (score >= bestScore) continue;
                bestScore = score;
                best = i;
            }
            return best;
        }

        private Quaternion GetLargestFaceForwardRotation(IReadOnlyList<int> faceValues)
        {
            var largestIndex = 0;
            for (var i = 1; i < faceValues.Count; i++)
                if (faceValues[i] > faceValues[largestIndex]) largestIndex = i;
            return Quaternion.FromToRotation(normals[largestIndex], Vector3.back);
        }

        private void CreateDisplay(RectTransform parent)
        {
            var objectRoot = new GameObject("Dice Tuning 3D", typeof(RectTransform));
            objectRoot.transform.SetParent(parent, false);
            visualRoot = objectRoot;

            var imageObject = new GameObject("Display", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imageObject.transform.SetParent(objectRoot.transform, false);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(620f, 330f);
            rect.anchoredPosition = new Vector2(0f, -15f);
            display = imageObject.GetComponent<RawImage>();
            display.raycastTarget = false;

            texture = new RenderTexture(900, 480, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            texture.Create();
            display.texture = texture;
        }

        private void CreateStage()
        {
            var stage = new GameObject("Dice Tuning Render Stage");
            stage.transform.SetParent(transform, false);
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            whiteMaterial = new Material(shader) { color = new Color(.96f, .96f, .93f) };
            blackMaterial = new Material(shader) { color = new Color(.035f, .035f, .045f) };
            blackPipMaterial = new Material(shader) { color = Color.black };
            whitePipMaterial = new Material(shader) { color = Color.white };
            dice = new[]
            {
                CreateDie(stage.transform, "Editable Dice 1", whiteMaterial, blackPipMaterial),
                CreateDie(stage.transform, "Editable Dice 2", blackMaterial, whitePipMaterial)
            };

            var cameraObject = new GameObject("Dice Tuning Camera");
            cameraObject.transform.SetParent(stage.transform, false);
            cameraObject.transform.position = center + new Vector3(0f, .35f, -7f);
            cameraObject.transform.LookAt(center);
            renderCamera = cameraObject.AddComponent<UnityEngine.Camera>();
            renderCamera.orthographic = true;
            renderCamera.orthographicSize = 2.35f;
            renderCamera.clearFlags = CameraClearFlags.SolidColor;
            renderCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            renderCamera.targetTexture = texture;
        }

        private DieView CreateDie(Transform parent, string name, Material material, Material pipMaterial)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localScale = Vector3.one * 1.35f;
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            Destroy(cube.GetComponent<Collider>());
            var faces = new DicePipFace[6];
            for (var i = 0; i < faces.Length; i++) faces[i] = DicePipFace.Create(cube.transform, normals[i], pipMaterial, .11f);
            return new DieView(cube.transform, faces);
        }

        private void CreateControls(RectTransform parent)
        {
            parent = (RectTransform)visualRoot.transform;
            selectors = new Button[2];
            names = new Text[2];
            for (var i = 0; i < 2; i++)
            {
                var x = i == 0 ? -120f : 120f;
                names[i] = CreateText(parent, $"Dice {i + 1} Name", $"주사위 {i + 1}", new Vector2(x, 128f), new Vector2(180f, 36f), 22);
                selectors[i] = CreateButton(parent, $"Select Dice {i + 1}", string.Empty, new Vector2(x, -15f), new Vector2(220f, 280f));
                selectors[i].GetComponent<Image>().color = new Color(1f, 1f, 1f, .001f);
                var captured = i;
                selectors[i].onClick.AddListener(() => selected?.Invoke(captured));
            }

            arrows = new[]
            {
                CreateButton(parent, "Rotate Up", "▲", new Vector2(0f, 135f), new Vector2(56f, 44f)),
                CreateButton(parent, "Rotate Down", "▼", new Vector2(0f, -150f), new Vector2(56f, 44f)),
                CreateButton(parent, "Rotate Left", "◀", new Vector2(-175f, -10f), new Vector2(56f, 44f)),
                CreateButton(parent, "Rotate Right", "▶", new Vector2(175f, -10f), new Vector2(56f, 44f))
            };
            arrows[0].onClick.AddListener(() => Rotate(Vector3.right, -90f));
            arrows[1].onClick.AddListener(() => Rotate(Vector3.right, 90f));
            arrows[2].onClick.AddListener(() => Rotate(Vector3.up, -90f));
            arrows[3].onClick.AddListener(() => Rotate(Vector3.up, 90f));
            SetArrows(false);
        }

        private static Button CreateButton(RectTransform parent, string name, string text, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(.24f, .28f, .36f, .96f);
            if (!string.IsNullOrEmpty(text)) CreateText(rect, "Text", text, Vector2.zero, size, 26);
            return go.GetComponent<Button>();
        }

        private static Text CreateText(RectTransform parent, string name, string value, Vector2 position, Vector2 size, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private void SetFaceValues(DieView die, IReadOnlyList<int> faceValues)
        {
            for (var i = 0; i < die.Faces.Length; i++) die.Faces[i].SetValue(faceValues[i]);
        }

        private void SetArrows(bool active)
        {
            if (arrows == null) return;
            foreach (var arrow in arrows) arrow.gameObject.SetActive(active);
        }

        private void SetArrowInteractable(bool interactable)
        {
            foreach (var arrow in arrows) arrow.interactable = interactable;
        }

        private void OnDestroy()
        {
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (whiteMaterial != null) Destroy(whiteMaterial);
            if (blackMaterial != null) Destroy(blackMaterial);
            if (blackPipMaterial != null) Destroy(blackPipMaterial);
            if (whitePipMaterial != null) Destroy(whitePipMaterial);
        }

        private sealed class DieView
        {
            public DieView(Transform transform, DicePipFace[] faces) { Transform = transform; Faces = faces; }
            public Transform Transform { get; }
            public DicePipFace[] Faces { get; }
        }
    }
}
