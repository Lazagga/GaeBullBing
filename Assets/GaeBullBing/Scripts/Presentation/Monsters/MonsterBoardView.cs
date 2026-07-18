using System.Collections;
using System;
using GaeBullBing.Presentation.Board;
using UnityEngine;

namespace GaeBullBing.Presentation.Monsters
{
    public sealed class MonsterBoardView : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float stepDuration = 0.18f;
        [SerializeField, Min(0f)] private float hopHeight = 0.18f;
        [SerializeField] private Vector3 positionOffset = new(0f, 0.22f, 0f);

        private BoardTilemapView boardView;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer healthBackgroundRenderer;
        private SpriteRenderer healthFillRenderer;
        private Transform healthFill;
        private Coroutine layoutRoutine;
        private bool isMoving;
        private Vector3 visualHeightOffset = new(0f, .22f, 0f);
        private float healthBarY = .48f;
        private Sprite frontSprite;
        private Sprite backSprite;

        public int InstanceId { get; private set; }
        public event Action<int, int> TileChanged;

        public void Initialize(
            int instanceId,
            BoardTilemapView view,
            int tileIndex,
            Vector3 baseVisualOffset,
            Sprite front,
            Sprite back)
        {
            InstanceId = instanceId;
            CurrentTileIndex = tileIndex;
            boardView = view;
            visualHeightOffset = baseVisualOffset;
            positionOffset = baseVisualOffset;
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            frontSprite = front;
            backSprite = back != null ? back : front;
            ApplyDirectionForDeparture(tileIndex);
            transform.position = GetStandingPosition(tileIndex);
            if (spriteRenderer != null && spriteRenderer.sprite != null)
                healthBarY = Mathf.Max(.48f, spriteRenderer.sprite.bounds.max.y * spriteRenderer.transform.localScale.y + .08f);
            CreateHealthBar();
        }

        public void SetLayoutOffset(Vector3 offset)
        {
            var targetOffset = offset + visualHeightOffset;
            if (layoutRoutine != null) StopCoroutine(layoutRoutine);
            layoutRoutine = StartCoroutine(AnimateLayoutOffset(targetOffset));
        }
        private IEnumerator AnimateLayoutOffset(Vector3 targetOffset)
        {
            var startOffset = positionOffset;
            var elapsed = 0f;
            while (elapsed < .18f)
            {
                elapsed += Time.deltaTime;
                positionOffset = Vector3.Lerp(startOffset, targetOffset, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / .18f)));
                if (!isMoving)
                    transform.position = GetStandingPosition(CurrentTileIndex);
                yield return null;
            }
            positionOffset = targetOffset;
            if (!isMoving)
                transform.position = GetStandingPosition(CurrentTileIndex);
            layoutRoutine = null;
        }
        public int CurrentTileIndex { get; private set; }
        public void SetVisible(bool visible)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = visible;
            if (healthBackgroundRenderer != null) healthBackgroundRenderer.enabled = visible;
            if (healthFillRenderer != null) healthFillRenderer.enabled = visible;
        }

        private void LateUpdate()
        {
            // Player tile presses are visual-only, so stationary monsters must follow
            // the pressed tile every frame without changing their logical tile index.
            if (!isMoving && boardView != null)
                transform.position = GetStandingPosition(CurrentTileIndex);

            // The sprite is raised for readability, but depth belongs to its ground position.
            var order = BoardDepthSorting.GetActorOrder(
                transform.position - visualHeightOffset - GetTileVisualOffset(CurrentTileIndex),
                CurrentTileIndex);
            if (spriteRenderer != null) spriteRenderer.sortingOrder = order;
            if (healthBackgroundRenderer != null) healthBackgroundRenderer.sortingOrder = order + 1;
            if (healthFillRenderer != null) healthFillRenderer.sortingOrder = order + 2;
        }

        public void UpdateHealth(float current, float max)
        {
            var ratio = Mathf.Clamp01(max > 0 ? (float)current / max : 0f);
            healthFill.localScale = new Vector3(.46f * ratio, .055f, 1f);
            healthFill.localPosition = new Vector3(-.23f + .23f * ratio, healthBarY, 0f);
        }

        private void CreateHealthBar()
        {
            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
            var back = new GameObject("Health Background"); back.transform.SetParent(transform, false); back.transform.localPosition = new Vector3(0,healthBarY); back.transform.localScale = new Vector3(.5f,.075f,1);
            healthBackgroundRenderer = back.AddComponent<SpriteRenderer>(); healthBackgroundRenderer.sprite = sprite; healthBackgroundRenderer.color = new Color(.12f,.12f,.12f,.9f);
            var fill = new GameObject("Health Fill"); fill.transform.SetParent(transform, false); healthFill = fill.transform;
            healthFillRenderer = fill.AddComponent<SpriteRenderer>(); healthFillRenderer.sprite = sprite; healthFillRenderer.color = new Color(.2f,.9f,.25f);
        }

        public IEnumerator MoveSteps(int startTileIndex, int distance)
        {
            isMoving = true;
            for (var step = 1; step <= distance; step++)
            {
                var fromIndex = (startTileIndex + step - 1) % GaeBullBing.Core.Board.BoardState.DefaultTileCount;
                var toIndex = (startTileIndex + step) % GaeBullBing.Core.Board.BoardState.DefaultTileCount;
                var from = GetTileGroundPosition(fromIndex);
                var to = GetTileGroundPosition(toIndex);
                ApplyDirectionForDeparture(fromIndex);
                var previousTileIndex = CurrentTileIndex;
                CurrentTileIndex = toIndex;
                TileChanged?.Invoke(previousTileIndex, toIndex);
                var elapsed = 0f;

                while (elapsed < stepDuration)
                {
                    elapsed += Time.deltaTime;
                    var progress = Mathf.Clamp01(elapsed / stepDuration);
                    var position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress)) + positionOffset;
                    position.y += Mathf.Sin(progress * Mathf.PI) * hopHeight;
                    transform.position = position;
                    yield return null;
                }
                transform.position = to + positionOffset;
            }
            isMoving = false;
            transform.position = GetStandingPosition(CurrentTileIndex);
        }

        public IEnumerator PlayKnockback(int fromTileIndex, int toTileIndex)
        {
            if (fromTileIndex == toTileIndex) yield break;
            isMoving = true;
            ApplyDirectionForDeparture(fromTileIndex);
            var from = GetTileGroundPosition(fromTileIndex);
            var to = GetTileGroundPosition(toTileIndex);
            CurrentTileIndex = toTileIndex;
            TileChanged?.Invoke(fromTileIndex, toTileIndex);
            for (var elapsed = 0f; elapsed < stepDuration; elapsed += Time.deltaTime)
            {
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / stepDuration));
                transform.position = Vector3.Lerp(from, to, progress) + positionOffset;
                yield return null;
            }
            transform.position = to + positionOffset;
            isMoving = false;
        }

        private Vector3 GetStandingPosition(int tileIndex)
        {
            return GetTileGroundPosition(tileIndex) + positionOffset;
        }

        private Vector3 GetTileGroundPosition(int tileIndex)
        {
            return boardView.GetWorldPosition(tileIndex) + GetTileVisualOffset(tileIndex);
        }

        private Vector3 GetTileVisualOffset(int tileIndex)
        {
            return boardView != null ? boardView.GetTileVisualWorldOffset(tileIndex) : Vector3.zero;
        }

        private void ApplyDirectionForDeparture(int tileIndex)
        {
            if (spriteRenderer == null || frontSprite == null) return;
            var tileCount = GaeBullBing.Core.Board.BoardState.DefaultTileCount;
            var normalized = (tileIndex % tileCount + tileCount) % tileCount;
            var line = normalized < 9 ? 0 : normalized < 18 ? 1 : normalized < 27 ? 2 : 3;
            spriteRenderer.sprite = line <= 1 ? backSprite : frontSprite;
            spriteRenderer.flipX = line == 1 || line == 2;
        }

        public IEnumerator PlayHit()
        {
            var originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.12f);
            spriteRenderer.color = originalColor;
        }
    }
}
