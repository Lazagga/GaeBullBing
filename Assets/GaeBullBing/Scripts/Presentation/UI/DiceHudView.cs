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

        private GameController controller;

        public void Bind(GameController gameController)
        {
            controller = gameController;
            rollButton.onClick.RemoveListener(OnRollClicked);
            rollButton.onClick.AddListener(OnRollClicked);
            SetResults(0, 0);
            BeginPlayerTurn();
        }

        public void SetRolling(bool rolling)
        {
            rollButton.interactable = !rolling;
            if (rolling)
            {
                rollButton.gameObject.SetActive(false);
                firstDiceText.text = "?";
                secondDiceText.text = "?";
                totalText.text = "Rolling...";
            }
        }

        public void SetBusy()
        {
            rollButton.gameObject.SetActive(false);
            rollButton.interactable = false;
        }

        public void BeginPlayerTurn()
        {
            rollButton.gameObject.SetActive(true);
            rollButton.interactable = true;
        }

        public void ShowGameOver(int escapedCount, int escapeLimit)
        {
            rollButton.gameObject.SetActive(false);
            rollButton.interactable = false;
            firstDiceText.text = "-";
            secondDiceText.text = "-";
            totalText.text = $"GAME OVER  {escapedCount}/{escapeLimit}";
        }

        public void SetResults(int first, int second)
        {
            firstDiceText.text = first > 0 ? first.ToString() : "-";
            secondDiceText.text = second > 0 ? second.ToString() : "-";
            totalText.text = first + second > 0 ? $"Total  {first + second}" : "Roll the dice";
        }

        private void OnRollClicked() => controller.RollDiceAndMovePlayer();
    }
}
