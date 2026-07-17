using GaeBullBing.Presentation.Game;
using UnityEngine;
using UnityEngine.UI;

namespace GaeBullBing.Presentation.UI
{
    public sealed class DiceHudView : MonoBehaviour
    {
        [SerializeField] private Text firstDiceText;
        [SerializeField] private Text secondDiceText;
        [SerializeField] private Text totalText;
        [SerializeField] private Button rollButton;
        [SerializeField] private Button endTurnButton;

        private GameController controller;

        public void Bind(GameController gameController)
        {
            controller = gameController;
            rollButton.onClick.RemoveListener(OnRollClicked);
            rollButton.onClick.AddListener(OnRollClicked);
            endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
            SetResults(0, 0);
            BeginPlayerTurn();
        }

        public void SetRolling(bool rolling)
        {
            rollButton.interactable = !rolling;
            endTurnButton.interactable = false;
            if (rolling)
            {
                firstDiceText.text = "?";
                secondDiceText.text = "?";
                totalText.text = "Rolling...";
            }
        }

        public void SetAwaitingEndTurn()
        {
            rollButton.interactable = false;
            endTurnButton.interactable = true;
        }

        public void SetBusy()
        {
            rollButton.interactable = false;
            endTurnButton.interactable = false;
        }

        public void BeginPlayerTurn()
        {
            rollButton.interactable = true;
            endTurnButton.interactable = false;
        }

        public void SetResults(int first, int second)
        {
            firstDiceText.text = first > 0 ? first.ToString() : "-";
            secondDiceText.text = second > 0 ? second.ToString() : "-";
            totalText.text = first + second > 0 ? $"Total  {first + second}" : "Roll the dice";
        }

        private void OnRollClicked() => controller.RollDiceAndMovePlayer();
        private void OnEndTurnClicked() => controller.EndTurn();
    }
}
