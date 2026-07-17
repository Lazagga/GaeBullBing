using System.Collections;
using GaeBullBing.Presentation.Board;
using UnityEngine;
using UnityEngine.UI;

namespace GaeBullBing.Presentation.Dice
{
    public sealed class Dice3DPresenter : MonoBehaviour
    {
        [SerializeField, Min(0.2f)] private float rollDuration = 1.45f;
        [SerializeField, Min(0f)] private float resultHoldDuration = 0.18f;
        [SerializeField] private float diceSize = 1f;
        [SerializeField] private RawImage display;

        private const float StageOffset = 1000f;
        private readonly Vector3 stageCenter = new(StageOffset, 0f, StageOffset);
        private DieView first;
        private DieView second;
        private RenderTexture renderTexture;
        private Material diceMaterial;
        private int lastPreset = -1;

        public void Initialize(BoardTilemapView unusedBoardView)
        {
            if (first != null)
                return;
            ConfigureDisplay();
            CreateStage();
            display.enabled = false;
        }

        public IEnumerator Roll(int firstResult, int secondResult)
        {
            var firstTarget = stageCenter + new Vector3(-0.65f, diceSize * 0.5f + 0.01f, 0f);
            var secondTarget = stageCenter + new Vector3(0.65f, diceSize * 0.5f + 0.01f, 0f);
            first.Label.text = firstResult.ToString();
            second.Label.text = secondResult.ToString();
            display.enabled = true;

            var preset = SelectPreset();
            for (var elapsed = 0f; elapsed < rollDuration; elapsed += Time.deltaTime)
            {
                var progress = Mathf.Clamp01(elapsed / rollDuration);
                AnimatePreset(preset, progress, firstTarget, secondTarget);
                yield return null;
            }

            SetFinalPose(first, firstTarget);
            SetFinalPose(second, secondTarget);
            if (resultHoldDuration > 0f)
                yield return new WaitForSeconds(resultHoldDuration);

            display.enabled = false;
            yield return null;
        }

        private int SelectPreset()
        {
            if (lastPreset < 0)
                return lastPreset = Random.Range(0, 3);

            var selection = Random.Range(0, 2);
            if (selection >= lastPreset)
                selection++;
            lastPreset = selection;
            return selection;
        }

        private void AnimatePreset(int preset, float t, Vector3 firstTarget, Vector3 secondTarget)
        {
            AnimateCollisionPreset(preset, t, firstTarget, secondTarget);
        }

        private void AnimateCollisionPreset(int preset, float t, Vector3 firstTarget, Vector3 secondTarget)
        {
            Vector3 firstStartOffset;
            Vector3 secondStartOffset;
            Vector3 firstReboundOffset;
            Vector3 secondReboundOffset;
            Vector3 firstTurns;
            Vector3 secondTurns;

            switch (preset)
            {
                case 0: // 정면 충돌
                    firstStartOffset = new Vector3(-2.9f, 3.4f, -1.25f);
                    secondStartOffset = new Vector3(2.9f, 3.6f, -1.25f);
                    firstReboundOffset = new Vector3(-2.25f, 2.15f, 1.15f);
                    secondReboundOffset = new Vector3(2.25f, 2.2f, -1.15f);
                    firstTurns = new Vector3(-6f, 5f, 4f);
                    secondTurns = new Vector3(6f, -5f, -4f);
                    break;
                case 1: // 대각선 충돌
                    firstStartOffset = new Vector3(-2.8f, 3.8f, 1.7f);
                    secondStartOffset = new Vector3(2.75f, 3.3f, -1.8f);
                    firstReboundOffset = new Vector3(-2.05f, 2.35f, -1.4f);
                    secondReboundOffset = new Vector3(2.1f, 2.25f, 1.45f);
                    firstTurns = new Vector3(7f, -5f, 4f);
                    secondTurns = new Vector3(-6f, 7f, -5f);
                    break;
                default: // 교차 후 크게 튕김
                    firstStartOffset = new Vector3(-2.5f, 4.1f, -1.85f);
                    secondStartOffset = new Vector3(2.55f, 4f, 1.85f);
                    firstReboundOffset = new Vector3(-2.45f, 2.65f, 1.55f);
                    secondReboundOffset = new Vector3(2.45f, 2.7f, -1.55f);
                    firstTurns = new Vector3(-8f, 7f, 6f);
                    secondTurns = new Vector3(8f, -6f, -7f);
                    break;
            }

            var impactHalfDistance = diceSize * 0.53f;
            var firstStart = stageCenter + firstStartOffset;
            var secondStart = stageCenter + secondStartOffset;
            var firstImpact = stageCenter + new Vector3(-impactHalfDistance, diceSize * 0.62f, -0.04f);
            var secondImpact = stageCenter + new Vector3(impactHalfDistance, diceSize * 0.62f, 0.04f);
            var firstRebound = stageCenter + firstReboundOffset;
            var secondRebound = stageCenter + secondReboundOffset;

            first.Transform.position = GetCollisionPathPosition(
                t, firstStart, firstImpact, firstRebound, firstTarget);
            second.Transform.position = GetCollisionPathPosition(
                t, secondStart, secondImpact, secondRebound, secondTarget);
            first.Transform.rotation = Quaternion.Euler(firstTurns * (360f * t));
            second.Transform.rotation = Quaternion.Euler(secondTurns * (360f * t));
        }

        private static Vector3 GetCollisionPathPosition(
            float t, Vector3 start, Vector3 impact, Vector3 rebound, Vector3 target)
        {
            const float impactTime = 0.34f;
            const float reboundEndTime = 0.69f;
            if (t <= impactTime)
            {
                var approach = Mathf.SmoothStep(0f, 1f, t / impactTime);
                var position = Vector3.Lerp(start, impact, approach);
                position.y += Mathf.Sin(approach * Mathf.PI) * 1.05f;
                return position;
            }

            if (t <= reboundEndTime)
            {
                var bounce = Mathf.SmoothStep(0f, 1f,
                    (t - impactTime) / (reboundEndTime - impactTime));
                var position = Vector3.Lerp(impact, rebound, bounce);
                position.y += Mathf.Sin(bounce * Mathf.PI) * 1.35f;
                return position;
            }

            var landing = Mathf.SmoothStep(0f, 1f,
                (t - reboundEndTime) / (1f - reboundEndTime));
            var landingPosition = Vector3.Lerp(rebound, target, landing);
            landingPosition.y += Mathf.Abs(Mathf.Sin(landing * Mathf.PI * 2f)) *
                                 (1f - landing) * 0.48f;
            return landingPosition;
        }

        private static void SetFinalPose(DieView die, Vector3 target)
        {
            die.Transform.position = target;
            die.Transform.rotation = Quaternion.identity;
        }

        private void ConfigureDisplay()
        {
            if (display == null)
                throw new MissingReferenceException("Dice3DPresenter에 3D Dice Display UI가 연결되지 않았습니다.");
            display.enabled = false;
            display.raycastTarget = false;
            display.color = Color.white;

            renderTexture = new RenderTexture(768, 480, 24, RenderTextureFormat.ARGB32)
            {
                name = "Dice 3D Render Texture",
                antiAliasing = 4
            };
            renderTexture.Create();
            display.texture = renderTexture;
        }

        private void CreateStage()
        {
            var stage = new GameObject("3D Dice Animation Stage");
            stage.transform.SetParent(transform, false);

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            diceMaterial = new Material(shader) { color = new Color(0.96f, 0.93f, 0.82f) };
            first = CreateDie(stage.transform, "3D Dice 1");
            second = CreateDie(stage.transform, "3D Dice 2");

            var cameraObject = new GameObject("Dice Isometric Camera");
            cameraObject.transform.SetParent(stage.transform, false);
            cameraObject.transform.position = stageCenter + new Vector3(6f, 7.5f, -6f);
            cameraObject.transform.LookAt(stageCenter);
            var camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.targetTexture = renderTexture;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 30f;
        }

        private DieView CreateDie(Transform parent, string objectName)
        {
            var die = GameObject.CreatePrimitive(PrimitiveType.Cube);
            die.name = objectName;
            die.transform.SetParent(parent, false);
            die.transform.localScale = Vector3.one * diceSize;
            die.GetComponent<MeshRenderer>().sharedMaterial = diceMaterial;
            Destroy(die.GetComponent<BoxCollider>());
            die.transform.position = stageCenter + new Vector3(0f, diceSize * 0.5f, 0f);

            var labelObject = new GameObject("Top Result");
            labelObject.transform.SetParent(die.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.505f, 0f);
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.16f;
            var label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 64;
            label.color = Color.black;
            return new DieView(die.transform, label);
        }

        private void OnDestroy()
        {
            if (renderTexture != null)
            {
                if (display != null)
                    display.texture = null;
                renderTexture.Release();
                Destroy(renderTexture);
            }
            if (diceMaterial != null)
                Destroy(diceMaterial);
        }

        private sealed class DieView
        {
            public DieView(Transform transform, TextMesh label) { Transform = transform; Label = label; }
            public Transform Transform { get; }
            public TextMesh Label { get; }
        }
    }
}
