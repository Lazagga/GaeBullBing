using System;
using System.Collections.Generic;
using GaeBullBing.Core.Data;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace GaeBullBing.Presentation.UI
{
    public sealed class RadialActionMenu : MonoBehaviour
    {
        [SerializeField] private RectTransform menuRoot;
        [SerializeField] private Button primaryButton;
        [SerializeField] private Text primaryText;
        [SerializeField] private Sprite buildButtonSprite;
        [SerializeField] private Sprite enhanceButtonSprite;
        [SerializeField] private Vector2 primaryButtonImageSize = new(160f, 78f);
        [SerializeField] private RectTransform choicesRoot;
        [SerializeField] private Button[] choiceButtons = Array.Empty<Button>();
        [SerializeField] private Vector2 screenOffset = new(0f, 145f);
        [SerializeField, Min(20f)] private float choiceRadius = 90f;
        [SerializeField, Min(0f)] private float upgradeScreenLeftMargin = 16f;
        [SerializeField] private float upgradeScreenCenterYOffset;
        [SerializeField] private Vector2 upgradeCardSize = new(220f, 105f);
        [SerializeField, Min(20f)] private float upgradeVerticalSpacing = 115f;
        [SerializeField] private DeveloperConsoleView developerConsole;

        private RectTransform canvasRect;
        private Transform worldTarget;
        private UnityEngine.Camera worldCamera;
        private bool showingUpgradeChoices;
        private int visibleUpgradeChoiceCount;

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
            var menuPosition = localPoint + screenOffset;
            menuRoot.anchoredPosition = menuPosition;
            if (showingUpgradeChoices && visibleUpgradeChoiceCount > 0)
                LayoutUpgradeChoices(GetUpgradeScreenCenter() - menuPosition);
        }

        private void Update()
        {
            if (!menuRoot.gameObject.activeInHierarchy ||
                developerConsole != null && developerConsole.IsOpen)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (primaryButton.gameObject.activeInHierarchy && primaryButton.interactable &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                primaryButton.onClick.Invoke();
                return;
            }

            if (!choicesRoot.gameObject.activeInHierarchy)
                return;

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
                InvokeChoice(0);
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
                InvokeChoice(1);
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
                InvokeChoice(2);
        }

        private void InvokeChoice(int index)
        {
            if (index < 0 || index >= choiceButtons.Length)
                return;
            var button = choiceButtons[index];
            if (button != null && button.gameObject.activeInHierarchy && button.interactable)
                button.onClick.Invoke();
        }

        public void ShowPrimary(
            Transform target,
            UnityEngine.Camera camera,
            bool hasTower,
            Action onPrimarySelected)
        {
            ClearChoices();
            showingUpgradeChoices = false;
            worldTarget = target;
            worldCamera = camera;
            menuRoot.gameObject.SetActive(true);
            choicesRoot.gameObject.SetActive(false);
            primaryButton.gameObject.SetActive(true);
            var actionSprite = hasTower ? enhanceButtonSprite : buildButtonSprite;
            var image = primaryButton.image;
            image.sprite = actionSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            ((RectTransform)primaryButton.transform).sizeDelta = primaryButtonImageSize;
            primaryText.gameObject.SetActive(actionSprite == null);
            primaryText.text = hasTower ? "ENFORCE" : "BUILD";
            primaryButton.onClick.RemoveAllListeners();
            primaryButton.onClick.AddListener(() => onPrimarySelected());
        }

        public void ShowChoices(IReadOnlyList<TowerDefinition> definitions, Action<TowerDefinition> onSelected)
        {
            ClearChoices();
            showingUpgradeChoices = false;
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
            showingUpgradeChoices = true;
            visibleUpgradeChoiceCount = visibleCount;
            for (var index = 0; index < visibleCount; index++)
            {
                var definition = definitions[index]; var button = choiceButtons[index];
                button.gameObject.SetActive(true);
                var rect = (RectTransform)button.transform;
                rect.sizeDelta = upgradeCardSize;
                var label = button.GetComponentInChildren<Text>();
                label.text = $"{definition.DisplayName}\n\n{definition.Description}";
                label.alignment = TextAnchor.MiddleCenter;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 10;
                label.resizeTextMaxSize = 18;
                button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => onSelected(definition));
            }
            LayoutUpgradeChoices(GetUpgradeScreenCenter() - menuRoot.anchoredPosition);
        }

        private Vector2 GetUpgradeScreenCenter()
        {
            var halfWidth = upgradeCardSize.x * .5f;
            var halfHeight = upgradeCardSize.y * .5f +
                (visibleUpgradeChoiceCount - 1) * upgradeVerticalSpacing * .5f;
            return new Vector2(
                canvasRect.rect.xMin + upgradeScreenLeftMargin + halfWidth,
                Mathf.Clamp(
                    upgradeScreenCenterYOffset,
                    canvasRect.rect.yMin + halfHeight,
                    canvasRect.rect.yMax - halfHeight));
        }

        private void LayoutUpgradeChoices(Vector2 localCenter)
        {
            for (var index = 0; index < visibleUpgradeChoiceCount && index < choiceButtons.Length; index++)
            {
                var verticalIndex = (visibleUpgradeChoiceCount - 1) * .5f - index;
                ((RectTransform)choiceButtons[index].transform).anchoredPosition =
                    localCenter + Vector2.up * (verticalIndex * upgradeVerticalSpacing);
            }
        }

        public void Hide()
        {
            ClearChoices();
            showingUpgradeChoices = false;
            visibleUpgradeChoiceCount = 0;
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
