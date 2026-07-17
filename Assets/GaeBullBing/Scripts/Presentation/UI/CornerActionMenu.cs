using System;
using GaeBullBing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GaeBullBing.Presentation.UI
{
    public sealed class CornerActionMenu : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text title;
        [SerializeField] private Button fireButton;
        [SerializeField] private Button iceButton;
        [SerializeField] private Button physicsButton;
        [SerializeField] private Button electricButton;
        [SerializeField] private GameObject teleportRoot;
        [SerializeField] private Dropdown tileDropdown;
        [SerializeField] private Button teleportButton;

        public void ShowElementSelection(Action<TowerElement> selected)
        {
            root.SetActive(true); teleportRoot.SetActive(false); title.text = "강화할 속성 선택";
            Bind(fireButton, TowerElement.Fire, selected); Bind(iceButton, TowerElement.Ice, selected);
            Bind(physicsButton, TowerElement.Physics, selected); Bind(electricButton, TowerElement.Electric, selected);
            SetElementButtons(true);
        }

        public void ShowTeleportSelection(int tileCount, int currentTile, Action<int> selected)
        {
            root.SetActive(true); SetElementButtons(false); teleportRoot.SetActive(true); title.text = "이동할 타일 선택";
            tileDropdown.ClearOptions(); var options = new System.Collections.Generic.List<string>();
            for (var i = 0; i < tileCount; i++) options.Add(i == currentTile ? $"{i} (현재 위치)" : i.ToString());
            tileDropdown.AddOptions(options); tileDropdown.value = currentTile;
            teleportButton.onClick.RemoveAllListeners(); teleportButton.onClick.AddListener(() => selected(tileDropdown.value));
        }

        public void Hide() { if (root != null) root.SetActive(false); }
        private void Bind(Button button, TowerElement element, Action<TowerElement> selected)
        { button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => selected(element)); }
        private void SetElementButtons(bool active)
        { fireButton.gameObject.SetActive(active); iceButton.gameObject.SetActive(active); physicsButton.gameObject.SetActive(active); electricButton.gameObject.SetActive(active); }
    }
}
