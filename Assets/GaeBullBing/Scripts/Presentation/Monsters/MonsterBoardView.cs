using System.Collections;
using GaeBullBing.Presentation.Board;
using UnityEngine;

namespace GaeBullBing.Presentation.Monsters
{
    public sealed class MonsterBoardView : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float stepDuration = 0.18f;
        [SerializeField] private Vector3 positionOffset = new(0f, 0.22f, 0f);

        private BoardTilemapView boardView;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer healthBackgroundRenderer;
        private SpriteRenderer healthFillRenderer;
        private Transform healthFill;
        private Coroutine layoutRoutine;

        public int InstanceId { get; private set; }

        public void Initialize(int instanceId, BoardTilemapView view, int tileIndex)
        {
            InstanceId = instanceId;
            CurrentTileIndex = tileIndex;
            boardView = view;
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            transform.position = boardView.GetWorldPosition(tileIndex) + positionOffset;
            CreateHealthBar();
        }

        public void SetLayoutOffset(Vector3 offset)
        {
            var targetOffset = offset + new Vector3(0f, .22f);
            if (layoutRoutine != null) StopCoroutine(layoutRoutine);
            layoutRoutine = StartCoroutine(AnimateLayoutOffset(targetOffset));
        }
        private IEnumerator AnimateLayoutOffset(Vector3 targetOffset)
        {
            var start = transform.position; positionOffset = targetOffset;
            var target = boardView.GetWorldPosition(CurrentTileIndex) + positionOffset; var elapsed = 0f;
            while (elapsed < .18f) { elapsed += Time.deltaTime; transform.position = Vector3.Lerp(start,target,Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.18f))); yield return null; }
            transform.position = target; layoutRoutine = null;
        }
        public int CurrentTileIndex { get; private set; }
        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        private void LateUpdate()
        {
            var order = BoardDepthSorting.GetOrder(transform.position);
            if (spriteRenderer != null) spriteRenderer.sortingOrder = order;
            if (healthBackgroundRenderer != null) healthBackgroundRenderer.sortingOrder = order + 1;
            if (healthFillRenderer != null) healthFillRenderer.sortingOrder = order + 2;
        }

        public void UpdateHealth(int current, int max)
        {
            var ratio = Mathf.Clamp01(max > 0 ? (float)current / max : 0f);
            healthFill.localScale = new Vector3(.46f * ratio, .055f, 1f);
            healthFill.localPosition = new Vector3(-.23f + .23f * ratio, .48f, 0f);
        }

        private void CreateHealthBar()
        {
            var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
            var back = new GameObject("Health Background"); back.transform.SetParent(transform, false); back.transform.localPosition = new Vector3(0,.48f); back.transform.localScale = new Vector3(.5f,.075f,1);
            healthBackgroundRenderer = back.AddComponent<SpriteRenderer>(); healthBackgroundRenderer.sprite = sprite; healthBackgroundRenderer.color = new Color(.12f,.12f,.12f,.9f);
            var fill = new GameObject("Health Fill"); fill.transform.SetParent(transform, false); healthFill = fill.transform;
            healthFillRenderer = fill.AddComponent<SpriteRenderer>(); healthFillRenderer.sprite = sprite; healthFillRenderer.color = new Color(.2f,.9f,.25f);
        }

        public IEnumerator MoveSteps(int startTileIndex, int distance)
        {
            for (var step = 1; step <= distance; step++)
            {
                var fromIndex = (startTileIndex + step - 1) % GaeBullBing.Core.Board.BoardState.DefaultTileCount;
                var toIndex = (startTileIndex + step) % GaeBullBing.Core.Board.BoardState.DefaultTileCount;
                var from = boardView.GetWorldPosition(fromIndex) + positionOffset;
                var to = boardView.GetWorldPosition(toIndex) + positionOffset;
                var elapsed = 0f;

                while (elapsed < stepDuration)
                {
                    elapsed += Time.deltaTime;
                    var progress = Mathf.Clamp01(elapsed / stepDuration);
                    transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress));
                    yield return null;
                }
                transform.position = to;
                CurrentTileIndex = toIndex;
            }
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
