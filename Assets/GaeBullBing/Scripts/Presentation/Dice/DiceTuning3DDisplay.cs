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

        [SerializeField] private GameObject visualRoot;
        [SerializeField] private RawImage display;
        [SerializeField] private Button[] selectors;
        [SerializeField] private Button[] arrows;
        private RenderTexture texture;
        private UnityEngine.Camera renderCamera;
        private DieView[] dice;
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
            if (dice != null) return;
            if (visualRoot == null || display == null || selectors == null || selectors.Length < 2 ||
                arrows == null || arrows.Length < 4)
                throw new MissingReferenceException("Dice tuning fixed UI is not connected in the scene.");
            CreateStage();
            BindControls();
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

        public void RotateUp() => Rotate(Vector3.right, -90f);
        public void RotateDown() => Rotate(Vector3.right, 90f);
        public void RotateLeft() => Rotate(Vector3.up, -90f);
        public void RotateRight() => Rotate(Vector3.up, 90f);

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

        private void CreateRenderTexture()
        {
            texture = new RenderTexture(900, 480, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            texture.Create();
            display.texture = texture;
        }

        private void CreateStage()
        {
            CreateRenderTexture();
            var stage = new GameObject("Dice Tuning Render Stage");
            stage.transform.SetParent(transform, false);
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("GaeBullBing/DiceOverlay") ??
                         Shader.Find("Unlit/Color");
            if (shader == null)
                throw new MissingReferenceException("A dice tuning shader was not included in the build.");
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

        private void BindControls()
        {
            for (var i = 0; i < 2; i++)
            {
                var captured = i;
                selectors[i].onClick.RemoveAllListeners();
                selectors[i].onClick.AddListener(() => selected?.Invoke(captured));
            }
            foreach (var arrow in arrows) arrow.onClick.RemoveAllListeners();
            arrows[0].onClick.AddListener(RotateUp);
            arrows[1].onClick.AddListener(RotateDown);
            arrows[2].onClick.AddListener(RotateLeft);
            arrows[3].onClick.AddListener(RotateRight);
            SetArrows(false);
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
