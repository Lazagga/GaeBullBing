using System.Collections;
using System;
using UnityEngine;

namespace GaeBullBing.Presentation.Board
{
    public sealed class PlayerBoardView : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float stepDuration = 0.14f;
        [SerializeField, Min(0f)] private float hopHeight = 0.18f;
        [SerializeField] private Vector3 positionOffset = Vector3.zero;

        private BoardTilemapView boardView;
        private SpriteRenderer visualRenderer;
        private int currentTileIndex;
        private Coroutine layoutRoutine;
        private bool isMoving;

        public Vector3 CameraFollowPosition { get; private set; }
        public event Action<int> TileEntered;

        public void Initialize(BoardTilemapView view, int tileIndex)
        {
            boardView = view;
            ConfigureVisual();
            SnapTo(tileIndex);
        }

        private void ConfigureVisual()
        {
            var source = GetComponent<SpriteRenderer>();
            if (source == null || transform.Find("Visual") != null) return;
            var visual = new GameObject("Visual"); visual.transform.SetParent(transform, false);
            visual.transform.localScale = Vector3.one;
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = source.sprite; renderer.color = source.color; renderer.sharedMaterial = source.sharedMaterial;
            renderer.sortingLayerID = source.sortingLayerID; renderer.sortingOrder = source.sortingOrder;
            renderer.flipX = source.flipX; renderer.flipY = source.flipY; source.enabled = false;
            visualRenderer = renderer;
        }

        private void LateUpdate()
        {
            if (visualRenderer == null)
                visualRenderer = transform.Find("Visual")?.GetComponent<SpriteRenderer>();
            if (visualRenderer != null)
                visualRenderer.sortingOrder = BoardDepthSorting.GetOrder(CameraFollowPosition, 20);
        }

        public void SnapTo(int tileIndex)
        {
            EnsureBoardView();
            currentTileIndex = tileIndex;
            transform.position = boardView.GetWorldPosition(tileIndex) + positionOffset;
            CameraFollowPosition = transform.position;
        }

        public void SetLayoutOffset(Vector3 offset)
        {
            var targetOffset = offset;
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
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / .18f));
                positionOffset = Vector3.Lerp(startOffset, targetOffset, progress);
                if (!isMoving)
                {
                    transform.position = boardView.GetWorldPosition(currentTileIndex) + positionOffset;
                    CameraFollowPosition = transform.position;
                }
                yield return null;
            }
            positionOffset = targetOffset;
            if (!isMoving)
            {
                transform.position = boardView.GetWorldPosition(currentTileIndex) + positionOffset;
                CameraFollowPosition = transform.position;
            }
            layoutRoutine = null;
        }

        public IEnumerator MoveSteps(int startTileIndex, int distance)
        {
            EnsureBoardView();
            isMoving = true;

            for (var step = 1; step <= distance; step++)
            {
                var fromIndex = (startTileIndex + step - 1) % GaeBullBing.Core.Board.BoardState.DefaultTileCount;
                var toIndex = (startTileIndex + step) % GaeBullBing.Core.Board.BoardState.DefaultTileCount;
                var from = boardView.GetWorldPosition(fromIndex);
                var to = boardView.GetWorldPosition(toIndex);
                var elapsed = 0f;

                while (elapsed < stepDuration)
                {
                    elapsed += Time.deltaTime;
                    var progress = Mathf.Clamp01(elapsed / stepDuration);
                    var position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress)) + positionOffset;
                    CameraFollowPosition = position;
                    position.y += Mathf.Sin(progress * Mathf.PI) * hopHeight;
                    transform.position = position;
                    yield return null;
                }

                currentTileIndex = toIndex;
                transform.position = to + positionOffset;
                CameraFollowPosition = transform.position;
                TileEntered?.Invoke(toIndex);
            }

            isMoving = false;
            transform.position = boardView.GetWorldPosition(currentTileIndex) + positionOffset;
            CameraFollowPosition = transform.position;
            boardView.PlayPress((startTileIndex + distance) % GaeBullBing.Core.Board.BoardState.DefaultTileCount);
        }

        private void EnsureBoardView()
        {
            if (boardView == null)
                throw new MissingReferenceException("PlayerBoardView has not been initialized with a BoardTilemapView.");
        }
    }
}
