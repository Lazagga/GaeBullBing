using System.Collections.Generic;
using System.Collections;
using GaeBullBing.Core.Dice;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GaeBullBing.Presentation.UI
{
    public sealed class DiceFaceListView : MonoBehaviour
    {
        [SerializeField] private Button whiteDiceButton;
        [SerializeField] private Button blackDiceButton;
        [SerializeField] private DiceFacePipGraphic[] whiteFaces;
        [SerializeField] private DiceFacePipGraphic[] blackFaces;
        [SerializeField, Min(1f)] private float faceStep = 56f;
        [SerializeField, Min(.05f)] private float animationDuration = .24f;
        [SerializeField, Min(0f)] private float animationStagger = .025f;

        private IReadOnlyList<DiceState> dice;
        private Coroutine whiteAnimation;
        private Coroutine blackAnimation;
        private bool whiteOpen;
        private bool blackOpen;

        private void Awake()
        {
            DisableKeyboardSelection(whiteDiceButton);
            DisableKeyboardSelection(blackDiceButton);
            whiteDiceButton.onClick.AddListener(ToggleWhiteList);
            blackDiceButton.onClick.AddListener(ToggleBlackList);
            SetCollapsed(whiteFaces);
            SetCollapsed(blackFaces);
        }

        public void Bind(IReadOnlyList<DiceState> diceStates)
        {
            dice = diceStates;
            Refresh();
        }

        public void Refresh()
        {
            if (dice == null || dice.Count < 2) return;
            ApplyFaces(whiteFaces, BuildPhysicalFaces(dice[0]), false);
            ApplyFaces(blackFaces, BuildPhysicalFaces(dice[1]), true);
        }

        private void ToggleWhiteList()
        {
            Refresh();
            SetWhiteOpen(!whiteOpen);
            ClearButtonSelection();
        }

        private void ToggleBlackList()
        {
            Refresh();
            SetBlackOpen(!blackOpen);
            ClearButtonSelection();
        }

        private static void DisableKeyboardSelection(Button button)
        {
            if (button == null) return;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
        }

        private static void ClearButtonSelection()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        private void SetWhiteOpen(bool open)
        {
            whiteOpen = open;
            if (whiteAnimation != null) StopCoroutine(whiteAnimation);
            whiteAnimation = StartCoroutine(AnimateFaces(whiteFaces, open, () => whiteAnimation = null));
        }

        private void SetBlackOpen(bool open)
        {
            blackOpen = open;
            if (blackAnimation != null) StopCoroutine(blackAnimation);
            blackAnimation = StartCoroutine(AnimateFaces(blackFaces, open, () => blackAnimation = null));
        }

        private IEnumerator AnimateFaces(DiceFacePipGraphic[] faces, bool open, System.Action completed)
        {
            var count = faces.Length;
            var startPositions = new Vector2[count];
            var startScales = new Vector3[count];
            var startAlphas = new float[count];
            for (var index = 0; index < count; index++)
            {
                faces[index].gameObject.SetActive(true);
                startPositions[index] = faces[index].rectTransform.anchoredPosition;
                startScales[index] = faces[index].rectTransform.localScale;
                startAlphas[index] = faces[index].GetComponent<CanvasGroup>().alpha;
            }

            var elapsed = 0f;
            var totalDuration = animationDuration + animationStagger * Mathf.Max(0, count - 1);
            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                for (var index = 0; index < count; index++)
                {
                    var delayIndex = open ? index : count - 1 - index;
                    var progress = Mathf.Clamp01((elapsed - delayIndex * animationStagger) / animationDuration);
                    var eased = Mathf.SmoothStep(0f, 1f, progress);
                    var rect = faces[index].rectTransform;
                    var target = new Vector2(startPositions[index].x, open ? -(index + 1) * faceStep : 0f);
                    rect.anchoredPosition = Vector2.Lerp(startPositions[index], target, eased);
                    rect.localScale = Vector3.Lerp(startScales[index], open ? Vector3.one : Vector3.one * .72f, eased);
                    faces[index].GetComponent<CanvasGroup>().alpha = Mathf.Lerp(startAlphas[index], open ? 1f : 0f, eased);
                }
                yield return null;
            }

            if (!open)
                foreach (var face in faces) face.gameObject.SetActive(false);
            completed?.Invoke();
        }

        private static int[] BuildPhysicalFaces(DiceState state)
        {
            var result = new List<int>(6);
            for (var faceIndex = 0; faceIndex < state.Faces.Length; faceIndex++)
                for (var count = 0; count < state.Weights[faceIndex]; count++)
                    result.Add(state.Faces[faceIndex]);
            while (result.Count < 6) result.Add(state.Faces[0]);
            if (result.Count > 6) result.RemoveRange(6, result.Count - 6);
            return result.ToArray();
        }

        private static void ApplyFaces(DiceFacePipGraphic[] graphics, IReadOnlyList<int> values, bool inverted)
        {
            for (var index = 0; index < graphics.Length && index < values.Count; index++)
            {
                graphics[index].SetInverted(inverted);
                graphics[index].SetValue(values[index]);
            }
        }

        private static void SetCollapsed(IEnumerable<DiceFacePipGraphic> faces)
        {
            foreach (var face in faces)
            {
                face.rectTransform.anchoredPosition = new Vector2(face.rectTransform.anchoredPosition.x, 0f);
                face.rectTransform.localScale = Vector3.one * .72f;
                face.GetComponent<CanvasGroup>().alpha = 0f;
                face.gameObject.SetActive(false);
            }
        }
    }
}
