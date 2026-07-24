using UnityEngine;
using UnityEngine.UI;
using GaeBullBing.Presentation.Board;

namespace GaeBullBing.Presentation.UI
{
    public sealed class TileInfoPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text towerText;
        [SerializeField] private Text monsterText;
        private ScrollRect monsterScroll;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        public void Show(string title, string towerDescription, string monsterDescription)
        {
            if (titleText != null) titleText.text = title;
            if (towerText != null) towerText.text = towerDescription;
            if (monsterText != null)
            {
                monsterText.text = monsterDescription;
            }
            if (panelRoot != null) panelRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
            if (monsterScroll != null) monsterScroll.verticalNormalizedPosition = 1f;
        }

        private void Awake()
        {
            ConfigureMonsterScroll();
        }

        private void ConfigureMonsterScroll()
        {
            if (monsterText == null || monsterText.transform.parent is not RectTransform viewport)
                return;

            monsterText.fontSize = 27;
            monsterText.resizeTextForBestFit = false;
            monsterText.lineSpacing = 1f;
            monsterText.horizontalOverflow = HorizontalWrapMode.Wrap;
            monsterText.verticalOverflow = VerticalWrapMode.Overflow;

            var content = monsterText.rectTransform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(.5f, 1f);
            content.anchoredPosition = new Vector2(0f, -12f);
            content.sizeDelta = new Vector2(-24f, 0f);

            var fitter = monsterText.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = monsterText.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var mask = viewport.GetComponent<RectMask2D>();
            if (mask == null) mask = viewport.gameObject.AddComponent<RectMask2D>();
            mask.padding = Vector4.zero;

            var viewportImage = viewport.GetComponent<Image>();
            if (viewportImage != null) viewportImage.raycastTarget = true;
            if (viewport.GetComponent<BoardPointerPassthrough>() == null)
                viewport.gameObject.AddComponent<BoardPointerPassthrough>();

            monsterScroll = viewport.GetComponent<ScrollRect>();
            if (monsterScroll == null) monsterScroll = viewport.gameObject.AddComponent<ScrollRect>();
            monsterScroll.viewport = viewport;
            monsterScroll.content = content;
            monsterScroll.horizontal = false;
            monsterScroll.vertical = true;
            monsterScroll.movementType = ScrollRect.MovementType.Clamped;
            monsterScroll.inertia = true;
            monsterScroll.scrollSensitivity = 28f;
        }

        public void Hide()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }
    }
}
