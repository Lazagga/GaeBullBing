using System;
using System.Collections.Generic;
using GaeBullBing.Core.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GaeBullBing.Presentation.UI
{
    public sealed class RadialActionMenu : MonoBehaviour
    {
        [SerializeField] private RectTransform menuRoot;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Text primaryText;
        [SerializeField] private RectTransform choicesRoot;
        [SerializeField] private Button[] choiceButtons = Array.Empty<Button>();
        [SerializeField] private Vector2 screenOffset = new(0f, 80f);
        [SerializeField, Min(20f)] private float choiceRadius = 90f;

        private RectTransform canvasRect;
        private Transform worldTarget;
        private UnityEngine.Camera worldCamera;

        private void Awake()
        {
            canvasRect = GetComponentInParent<Canvas>().transform as RectTransform;
            Hide();
        }

        private void LateUpdate()
        {
            if (worldTarget == null || !menuRoot.gameObject.activeSelf)
                return;

            var screenPoint = worldCamera.WorldToScreenPoint(worldTarget.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out var localPoint);
            menuRoot.anchoredPosition = localPoint + screenOffset;
        }

        public void ShowPrimary(
            Transform target,
            UnityEngine.Camera camera,
            bool hasTower,
            Action onPrimarySelected)
        {
            ClearChoices();
            worldTarget = target;
            worldCamera = camera;
            menuRoot.gameObject.SetActive(true);
            choicesRoot.gameObject.SetActive(false);
            primaryButton.gameObject.SetActive(true);
            primaryText.text = hasTower ? "ENFORCE" : "BUILD";
            primaryButton.onClick.RemoveAllListeners();
            primaryButton.onClick.AddListener(() => onPrimarySelected());
        }

        public void ShowChoices(IReadOnlyList<TowerDefinition> definitions, Action<TowerDefinition> onSelected)
        {
            ClearChoices();
            primaryButton.gameObject.SetActive(false);
            choicesRoot.gameObject.SetActive(true);

            var visibleCount = Mathf.Min(definitions.Count, choiceButtons.Length);
            for (var index = 0; index < visibleCount; index++)
            {
                var definition = definitions[index];
                var button = choiceButtons[index];
                button.gameObject.SetActive(true);
                var angle = (90f + index * (360f / visibleCount)) * Mathf.Deg2Rad;
                ((RectTransform)button.transform).anchoredPosition =
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * choiceRadius;
                button.GetComponentInChildren<Text>().text = definition.DisplayName;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onSelected(definition));
            }
        }

        public void ShowUpgradeChoices(IReadOnlyList<TowerUpgradeDefinition> definitions, Action<TowerUpgradeDefinition> onSelected)
        {
            ClearChoices(); primaryButton.gameObject.SetActive(false); choicesRoot.gameObject.SetActive(true);
            var visibleCount = Mathf.Min(definitions.Count, choiceButtons.Length);
            for (var index = 0; index < visibleCount; index++)
            {
                var definition = definitions[index]; var button = choiceButtons[index];
                button.gameObject.SetActive(true);
                var angle = (90f + index * (360f / visibleCount)) * Mathf.Deg2Rad;
                ((RectTransform)button.transform).anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * choiceRadius;
                button.GetComponentInChildren<Text>().text = definition.DisplayName;
                button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => onSelected(definition));
            }
        }

        public void Hide()
        {
            ClearChoices();
            worldTarget = null;
            menuRoot.gameObject.SetActive(false);
        }

        private void ClearChoices()
        {
            if (choicesRoot == null)
                return;

            foreach (var button in choiceButtons)
            {
                if (button == null)
                    continue;
                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
            }
        }
    }
}
