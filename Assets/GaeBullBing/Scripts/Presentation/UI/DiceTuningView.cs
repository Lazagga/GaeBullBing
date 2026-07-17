using System;
using System.Collections.Generic;
using GaeBullBing.Core.Dice;
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

        private IReadOnlyList<DiceState> dice;
        private Func<int, int, int, bool> selected;
        private int selectedDice;
        private int selectedFace;

        public void Show(IReadOnlyList<DiceState> diceStates, Func<int, int, int, bool> onSelected)
        {
            dice = diceStates;
            selected = onSelected;
            root.SetActive(true);
            ShowDiceStep();
        }

        public void Hide() => root.SetActive(false);

        private void ShowDiceStep()
        {
            title.text = "조정할 주사위를 선택하세요";
            SetButtons(diceButtons, true);
            SetButtons(faceButtons, false);
            decrementButton.gameObject.SetActive(false);
            incrementButton.gameObject.SetActive(false);
            for (var index = 0; index < diceButtons.Length; index++)
            {
                var captured = index;
                diceButtons[index].onClick.RemoveAllListeners();
                diceButtons[index].onClick.AddListener(() => SelectDice(captured));
            }
        }

        private void SelectDice(int index)
        {
            selectedDice = index;
            title.text = $"주사위 {index + 1}: 이동할 눈금을 선택하세요";
            SetButtons(diceButtons, false);
            SetButtons(faceButtons, true);
            for (var faceIndex = 0; faceIndex < faceButtons.Length; faceIndex++)
            {
                var capturedFace = faceIndex + 1;
                var weight = dice[index].Weights[faceIndex];
                faceButtons[faceIndex].GetComponentInChildren<Text>().text = $"{capturedFace}\n({weight})";
                faceButtons[faceIndex].interactable = weight > 0;
                faceButtons[faceIndex].onClick.RemoveAllListeners();
                faceButtons[faceIndex].onClick.AddListener(() => SelectFace(capturedFace));
            }
        }

        private void SelectFace(int face)
        {
            selectedFace = face;
            title.text = $"눈금 {face}: 이동 방향을 선택하세요";
            SetButtons(faceButtons, false);
            decrementButton.gameObject.SetActive(true);
            incrementButton.gameObject.SetActive(true);
            BindDelta(decrementButton, -1);
            BindDelta(incrementButton, 1);
        }

        private void BindDelta(Button button, int delta)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (selected != null && selected(selectedDice, selectedFace, delta))
                    Hide();
            });
        }

        private static void SetButtons(Button[] buttons, bool active)
        {
            foreach (var button in buttons)
                button.gameObject.SetActive(active);
        }
    }
}
