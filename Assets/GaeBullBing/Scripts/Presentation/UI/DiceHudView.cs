using GaeBullBing.Presentation.Game;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace GaeBullBing.Presentation.UI
{
    public sealed class DiceHudView : MonoBehaviour
    {
        [SerializeField] private Button rollButton;
        [SerializeField] private DeveloperConsoleView developerConsole;
        [SerializeField] private Text remainingKillsText;
        [SerializeField] private DiceFaceListView diceFaceListView;
        [SerializeField] private Image[] playerHealthHearts;

        private GameController controller;

        public void Bind(GameController gameController)
        {
            controller = gameController;
            if (diceFaceListView == null)
                diceFaceListView = FindFirstObjectByType<DiceFaceListView>(FindObjectsInactive.Include);
            diceFaceListView?.Bind(gameController.State.Dice);
            rollButton.onClick.RemoveListener(OnRollClicked);
            rollButton.onClick.AddListener(OnRollClicked);
            BeginPlayerTurn();
            RefreshDifficulty();
            RefreshPlayerHealth();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || controller == null || !controller.AcceptsGameplayInput ||
                developerConsole != null && developerConsole.IsOpen)
                return;
            if ((keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) &&
                rollButton.gameObject.activeInHierarchy && rollButton.interactable)
                OnRollClicked();
        }

        public void SetRolling(bool rolling)
        {
            rollButton.interactable = !rolling;
            if (rolling)
                rollButton.gameObject.SetActive(false);
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
            if (remainingKillsText != null) remainingKillsText.text = "게임 오버";
        }

        public void ShowGameClear()
        {
            rollButton.gameObject.SetActive(false);
            rollButton.interactable = false;
            if (remainingKillsText != null) remainingKillsText.text = "게임 클리어!";
        }

        public void SetResults(int first, int second) { }

        public void RefreshDifficulty()
        {
            if (remainingKillsText == null || controller == null) return;
            remainingKillsText.text = $"총 포획 {controller.TotalKills}마리";
        }

        public void RefreshDiceFaces() => diceFaceListView?.Refresh();

        public void RefreshPlayerHealth()
        {
            if (controller == null || playerHealthHearts == null) return;
            var remainingHealth = Mathf.Clamp(
                controller.State.EscapeLimit - controller.State.EscapedMonsterCount,
                0,
                playerHealthHearts.Length);
            for (var index = 0; index < playerHealthHearts.Length; index++)
                if (playerHealthHearts[index] != null)
                    playerHealthHearts[index].gameObject.SetActive(index < remainingHealth);
        }

        private void OnRollClicked()
        {
            if (controller != null && controller.AcceptsGameplayInput)
                controller.RollDiceAndMovePlayer();
        }
    }
}
