using System;
using System.Collections.Generic;
using GaeBullBing.Core.Dice;
using GaeBullBing.Presentation.Dice;
using UnityEngine;
using UnityEngine.UI;

namespace GaeBullBing.Presentation.UI
{
    public sealed class DiceTuningView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text title;
        [SerializeField] private Button[] diceButtons;
        [SerializeField] private Button[] faceButtons;
        [SerializeField] private Button decrementButton;
        [SerializeField] private Button incrementButton;
        [SerializeField] private Button backButton;

        private IReadOnlyList<DiceState> dice;
        private Func<int, int, int, bool> selected;
        private Action allTowerBoostSelected;
        private DiceTuning3DDisplay display3D;
        private int selectedDice;
        private Step currentStep;

        private enum Step { Reward, Dice, Face }

        private void Awake()
        {
            var host = new GameObject("Dice Tuning 3D Host").AddComponent<DiceTuning3DDisplay>();
            host.transform.SetParent(transform, false);
            host.Initialize((RectTransform)transform);
            display3D = host;
        }

        public void Show(IReadOnlyList<DiceState> diceStates, Func<int, int, int, bool> onSelected,
            Action onAllTowerBoostSelected)
        {
            dice = diceStates;
            selected = onSelected;
            allTowerBoostSelected = onAllTowerBoostSelected;
            root.SetActive(true);
            ShowRewardStep();
        }

        public void Hide()
        {
            display3D?.HideDisplay();
            root.SetActive(false);
        }

        private void ShowRewardStep()
        {
            currentStep = Step.Reward;
            ResizePanel(new Vector2(680f, 260f));
            title.text = "출발지 통과 보상을 선택하세요";
            ((RectTransform)title.transform).anchoredPosition = new Vector2(0f, 82f);
            display3D.HideDisplay();
            SetButtons(diceButtons, true);
            SetButtons(faceButtons, false);
            SetDeltaButtons(false);
            SetBack(false);

            for (var index = 0; index < diceButtons.Length; index++)
            {
                var button = diceButtons[index];
                button.onClick.RemoveAllListeners();
                if (index == 0)
                {
                    SetButtonText(button, "주사위 편집");
                    button.onClick.AddListener(ShowDiceStep);
                }
                else if (index == 1)
                {
                    SetButtonText(button, "모든 타워 공격력 +5%");
                    button.onClick.AddListener(() => { allTowerBoostSelected?.Invoke(); Hide(); });
                }
                else button.gameObject.SetActive(false);
            }
        }

        private void ShowDiceStep()
        {
            currentStep = Step.Dice;
            ResizePanel(new Vector2(680f, 500f));
            title.text = string.Empty;
            ((RectTransform)title.transform).anchoredPosition = new Vector2(0f, 205f);
            title.transform.SetAsLastSibling();
            SetButtons(diceButtons, false);
            SetButtons(faceButtons, false);
            SetDeltaButtons(false);
            SetBack(true);
            display3D.ShowSelection(BuildPhysicalFaces(), SelectDice);
        }

        private void SelectDice(int index)
        {
            selectedDice = index;
            currentStep = Step.Face;
            SetButtons(diceButtons, false);
            SetButtons(faceButtons, false);
            SetDeltaButtons(true);
            SetBack(true);
            display3D.ShowFaceSelection(index, RefreshSelectedFaceTitle);
            title.transform.SetAsLastSibling();
            decrementButton.transform.SetAsLastSibling();
            incrementButton.transform.SetAsLastSibling();
            if (backButton != null) backButton.transform.SetAsLastSibling();
        }

        private void RefreshSelectedFaceTitle()
        {
            title.text = string.Empty;
        }

        private int[][] BuildPhysicalFaces()
        {
            var result = new int[dice.Count][];
            for (var diceIndex = 0; diceIndex < dice.Count; diceIndex++)
            {
                var values = new List<int>(6);
                for (var faceIndex = 0; faceIndex < dice[diceIndex].Faces.Length; faceIndex++)
                    for (var count = 0; count < dice[diceIndex].Weights[faceIndex]; count++)
                        values.Add(dice[diceIndex].Faces[faceIndex]);
                while (values.Count < 6) values.Add(dice[diceIndex].Faces[0]);
                if (values.Count > 6) values.RemoveRange(6, values.Count - 6);
                result[diceIndex] = values.ToArray();
            }
            return result;
        }

        private void BindDelta(Button button, int delta)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (selected != null && selected(selectedDice, display3D.VisibleFaceValue, delta)) Hide();
            });
        }

        private void SetDeltaButtons(bool active)
        {
            decrementButton.gameObject.SetActive(active);
            incrementButton.gameObject.SetActive(active);
            if (!active) return;
            ((RectTransform)decrementButton.transform).anchoredPosition = new Vector2(-145f, -165f);
            ((RectTransform)incrementButton.transform).anchoredPosition = new Vector2(145f, -165f);
            SetButtonText(decrementButton, "-1");
            SetButtonText(incrementButton, "+1");
            BindDelta(decrementButton, -1);
            BindDelta(incrementButton, 1);
        }

        private void SetBack(bool active)
        {
            if (backButton == null) return;
            backButton.gameObject.SetActive(active);
            if (!active) return;
            ((RectTransform)backButton.transform).anchoredPosition = new Vector2(0f, -220f);
            SetButtonText(backButton, "뒤로가기");
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() =>
            {
                if (currentStep == Step.Face) ShowDiceStep();
                else ShowRewardStep();
            });
        }

        private void ResizePanel(Vector2 size) => ((RectTransform)transform).sizeDelta = size;

        private static void SetButtons(IEnumerable<Button> buttons, bool active)
        {
            foreach (var button in buttons) button.gameObject.SetActive(active);
        }

        private static void SetButtonText(Button button, string value)
        {
            var text = button.GetComponentInChildren<Text>();
            if (text == null) return;
            text.text = value;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 22;
        }
    }
}
