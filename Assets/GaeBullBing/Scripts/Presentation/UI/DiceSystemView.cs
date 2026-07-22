using System;
using System.Collections.Generic;
using GaeBullBing.Core.Dice;
using GaeBullBing.Presentation.Game;
using UnityEngine;
using UnityEngine.UI;

namespace GaeBullBing.Presentation.UI
{
    public sealed class DiceSystemView : MonoBehaviour
    {
        private GameController controller;
        private DiceHudView hud;
        private RectTransform loadoutRoot;
        private RectTransform dropdown;
        private GameObject rewardOverlay;
        private Text rewardText;
        private Button[] slotButtons;
        private int selectedSlot;
        private DiceState pendingReward;

        public void Initialize(GameController gameController, DiceHudView diceHud)
        {
            controller = gameController;
            hud = diceHud;
            if (loadoutRoot == null) Build();
            Refresh();
        }

public void SetVisible(bool visible)
        {
            if (loadoutRoot != null) loadoutRoot.gameObject.SetActive(visible);
            if (!visible && dropdown != null) dropdown.gameObject.SetActive(false);
            if (!visible)
            {
                SetSelectedSlot(-1);
                if (UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }

        }


        public void Refresh()
        {
            if (controller == null || slotButtons == null) return;
            for (var slot = 0; slot < slotButtons.Length; slot++)
            {
                var dice = controller.State.Dice[slot];
                var label = slotButtons[slot].GetComponentInChildren<Text>();
                if (dice == null)
                {
                    slotButtons[slot].GetComponent<Image>().color = new Color(.12f, .11f, .10f, 1f);
                    label.text = "?\n주사위 선택";
                    label.color = Color.white;
                    continue;
                }
                slotButtons[slot].GetComponent<Image>().color = new Color(.11f, .10f, .085f, .98f);
                label.text = $"?\n{dice.DisplayName}";
                label.fontSize = 22;
                label.color = Color.white;
            }
            if (dropdown != null && dropdown.gameObject.activeSelf) ShowDropdown(selectedSlot);
        }

public void ShowLapReward(DiceState reward, Action completed)
        {
            pendingReward = reward;
            rewardOverlay.SetActive(true);
            rewardText.text = $"완주 보상\n\n{reward.DisplayName}\n{FormatFaces(reward)}\n{reward.PassiveDescription}";
            var actions = rewardOverlay.transform.Find("Actions");
            for (var index = 2; index < actions.childCount; index++)
                actions.GetChild(index).gameObject.SetActive(false);
            var acquire = actions.Find("Acquire").GetComponent<Button>();
            var decline = actions.Find("Decline").GetComponent<Button>();
            acquire.gameObject.SetActive(true);
            decline.gameObject.SetActive(true);
            var actionLayout = actions.GetComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 20;
            acquire.GetComponent<RectTransform>().sizeDelta = new Vector2(210, 72);
            decline.GetComponent<RectTransform>().sizeDelta = new Vector2(210, 72);
            acquire.GetComponentInChildren<Text>().text = "획득하기";
            decline.GetComponentInChildren<Text>().text = "보상 포기";
            acquire.GetComponent<Image>().color = new Color(.78f, .53f, .17f);
            decline.GetComponent<Image>().color = new Color(.25f, .27f, .31f);
            acquire.GetComponentInChildren<Text>().color = Color.white;
            decline.GetComponentInChildren<Text>().color = Color.white;
            acquire.onClick.RemoveAllListeners();
            decline.onClick.RemoveAllListeners();
            acquire.onClick.AddListener(() =>
            {
                if (controller.Session.StoreDiceReward(pendingReward))
                {
                    CloseReward(completed);
                    return;
                }
                ShowReplacement(completed);
            });
            decline.onClick.AddListener(() =>
            {
                controller.Session.AddPermanentAllTowerDamageRateBonus(.05f);
                CloseReward(completed);
            });
        }

private void ShowReplacement(Action completed)
        {
            var inventory = controller.State.DiceInventory.Dice;
            rewardText.text = $"인벤토리가 가득 찼습니다.\n교체할 주사위를 선택하세요.\n\n{pendingReward.DisplayName}\n{FormatFaces(pendingReward)}";
            var actions = rewardOverlay.transform.Find("Actions");

            actions.GetComponent<HorizontalLayoutGroup>().spacing = 8;

            while (actions.childCount < inventory.Count)
            {
                var button = CreateButton(actions, "", Color.gray, new Vector2(105, 72));
                button.name = $"Replace{actions.childCount - 1}";
            }

            for (var index = 0; index < actions.childCount; index++)
            {
                var button = actions.GetChild(index).GetComponent<Button>();
                var active = index < inventory.Count;
                button.gameObject.SetActive(active);
                if (!active) continue;

                var inventoryIndex = index;
                var dice = inventory[index];
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(105, 72);
                button.GetComponentInChildren<Text>().text = $"{dice.DisplayName}\n{FormatFaces(dice)}";
                button.GetComponent<Image>().color = DiceColor(dice);
                button.GetComponentInChildren<Text>().color =
                    Luminance(button.GetComponent<Image>().color) > .72f ? Color.black : Color.white;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    controller.Session.ReplaceReserveDice(inventoryIndex, pendingReward);
                    CloseReward(completed);
                });
            }
        }

        private void CloseReward(Action completed)
        {
            rewardOverlay.SetActive(false);
            pendingReward = null;
            hud.RefreshDiceFaces();
            completed?.Invoke();
        }

        private void ToggleDropdown(int slot)
        {
            if (dropdown.gameObject.activeSelf && selectedSlot == slot)
            {
                dropdown.gameObject.SetActive(false);
                SetSelectedSlot(-1);
                hud.SetDiceSelectionOpen(false);
                return;
            }
            ShowDropdown(slot);
        }

private void ShowDropdown(int slot)
        {
            selectedSlot = slot;
            SetSelectedSlot(slot);
            foreach (Transform child in dropdown)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            dropdown.gameObject.SetActive(true);
            hud.SetDiceSelectionOpen(true);

            var inventory = controller.State.DiceInventory.Dice;
            var candidateCount = 0;
            for (var index = 0; index < inventory.Count; index++)
            {
                var dice = inventory[index];
                var equipped = false;
                for (var equippedSlot = 0; equippedSlot < controller.State.Dice.Count; equippedSlot++)
                    if (ReferenceEquals(controller.State.Dice[equippedSlot], dice))
                    {
                        equipped = true;
                        break;
                    }
                if (equipped) continue;

                candidateCount++;
                var inventoryIndex = index;
                var button = CreateButton(dropdown,
                    $"{dice.DisplayName}\n{FormatFaces(dice)}\n{dice.PassiveDescription}",
                    DiceColor(dice), new Vector2(210, 108));
                button.onClick.AddListener(() =>
                {
                    controller.Session.QueueDiceEquip(selectedSlot, inventoryIndex);
                    hud.RefreshRollAvailability();
                    dropdown.gameObject.SetActive(false);
                    SetSelectedSlot(-1);
                    hud.SetDiceSelectionOpen(false);
                    if (UnityEngine.EventSystems.EventSystem.current != null)
                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                    hud.RefreshDiceFaces();
                });
            }

            if (candidateCount == 0)
            {
                var message = CreateText(dropdown, "장착 가능한 다른 주사위가 없습니다.", 17, Color.white);
                message.alignment = TextAnchor.MiddleCenter;
                message.rectTransform.sizeDelta = new Vector2(430, 90);
            }
        }

        private void Build()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            var root = new GameObject("Dice Loadout", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            loadoutRoot = root.GetComponent<RectTransform>();
            loadoutRoot.anchorMin = loadoutRoot.anchorMax = new Vector2(.5f, .67f);
            loadoutRoot.sizeDelta = new Vector2(350, 150);

            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            slotButtons = new Button[2];
            for (var slot = 0; slot < 2; slot++)
            {
                var captured = slot;
                slotButtons[slot] = CreateButton(loadoutRoot, "?", new Color(.11f, .10f, .085f), new Vector2(155, 145));
                var navigation = slotButtons[slot].navigation;
                navigation.mode = Navigation.Mode.None;
                slotButtons[slot].navigation = navigation;
                var outline = slotButtons[slot].gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(1f, .82f, .36f, 1f);
                outline.effectDistance = new Vector2(4f, -4f);
                outline.enabled = false;

                slotButtons[slot].onClick.AddListener(() => ToggleDropdown(captured));
            }

            var dropObject = new GameObject("Dice Inventory Dropdown", typeof(RectTransform), typeof(Image),
                typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            dropObject.transform.SetParent(canvas.transform, false);
            dropdown = dropObject.GetComponent<RectTransform>();
            dropdown.anchorMin = dropdown.anchorMax = new Vector2(.5f, .465f);
            dropdown.sizeDelta = new Vector2(500, 132);
            dropObject.GetComponent<Image>().color = new Color(.08f, .07f, .06f, .96f);
            var dropLayout = dropObject.GetComponent<HorizontalLayoutGroup>();
            dropLayout.padding = new RectOffset(18, 18, 12, 12);
            dropLayout.spacing = 14;
            dropLayout.childAlignment = TextAnchor.MiddleCenter;
            dropLayout.childControlWidth = false;
            dropLayout.childControlHeight = false;
            dropObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            dropObject.SetActive(false);

            rewardOverlay = new GameObject("Dice Reward Overlay", typeof(RectTransform), typeof(Image));
            rewardOverlay.transform.SetParent(canvas.transform, false);
            var rewardRect = rewardOverlay.GetComponent<RectTransform>();
            rewardRect.anchorMin = new Vector2(.22f, .15f);
            rewardRect.anchorMax = new Vector2(.78f, .85f);
            rewardRect.offsetMin = rewardRect.offsetMax = Vector2.zero;
            rewardOverlay.GetComponent<Image>().color = new Color(.035f, .035f, .04f, .97f);
            rewardText = CreateText(rewardRect, "", 28, new Color(1f, .84f, .48f));
            rewardText.alignment = TextAnchor.MiddleCenter;
            rewardText.rectTransform.anchorMin = new Vector2(.08f, .28f);
            rewardText.rectTransform.anchorMax = new Vector2(.92f, .94f);
            rewardText.rectTransform.offsetMin = rewardText.rectTransform.offsetMax = Vector2.zero;

            var actionObject = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            actionObject.transform.SetParent(rewardRect, false);
            var actionRect = actionObject.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(.04f, .06f);
            actionRect.anchorMax = new Vector2(.96f, .25f);
            actionRect.offsetMin = actionRect.offsetMax = Vector2.zero;
            var actionLayout = actionObject.GetComponent<HorizontalLayoutGroup>();
            actionLayout.spacing = 20;
            actionLayout.childAlignment = TextAnchor.MiddleCenter;
            actionLayout.childControlWidth = false;
            actionLayout.childControlHeight = false;
            var take = CreateButton(actionRect, "획득하기", new Color(.78f, .53f, .17f), new Vector2(210, 72));
            take.name = "Acquire";
            var skip = CreateButton(actionRect, "보상 포기", new Color(.25f, .27f, .31f), new Vector2(210, 72));
            skip.name = "Decline";
            rewardOverlay.SetActive(false);
        }

        private static Button CreateButton(Transform parent, string label, Color color, Vector2 size)
        {
            var obj = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            obj.GetComponent<RectTransform>().sizeDelta = size;
            obj.GetComponent<Image>().color = color;
            var text = CreateText(obj.GetComponent<RectTransform>(), label, 17,
                Luminance(color) > .72f ? Color.black : Color.white);
            text.alignment = TextAnchor.MiddleCenter;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(8, 6);
            text.rectTransform.offsetMax = new Vector2(-8, -6);
            return obj.GetComponent<Button>();
        }

        private static Text CreateText(Transform parent, string value, int size, Color color)
        {
            var obj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            obj.transform.SetParent(parent, false);
            var text = obj.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = color;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static string FormatFaces(DiceState dice)
        {
            var values = new List<int>(6);
            for (var face = 0; face < dice.Faces.Length; face++)
                for (var count = 0; count < dice.Weights[face] && values.Count < 6; count++)
                    values.Add(dice.Faces[face]);
            return string.Join(" ", values);
        }

        private static Color DiceColor(DiceState dice) =>
            new Color(dice.Red, dice.Green, dice.Blue, 1f);

        private static float Luminance(Color color) =>
            color.r * .299f + color.g * .587f + color.b * .114f;
    

private void SetSelectedSlot(int slot)
        {
            if (slotButtons == null) return;
            for (var index = 0; index < slotButtons.Length; index++)
            {
                var outline = slotButtons[index].GetComponent<Outline>();
                if (outline != null) outline.enabled = index == slot;
            }
        }
}
}