using UnityEngine;
using GaeBullBing.Presentation.Board;

namespace GaeBullBing.Presentation.Camera
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CameraBackgroundView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera targetCamera;
        [SerializeField] private BoardTilemapView boardView;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField, Min(.01f)] private float overscan = 1.01f;
        [SerializeField, Min(.1f)] private float depth = 20f;
        [SerializeField, Min(.1f)] private float focusSize = 2.8f;

        private void Reset() => Configure();
        private void OnEnable() => Configure();
        private void OnValidate() => Configure();
        private void LateUpdate() => FitToCamera();

        private void Configure()
        {
            if (targetCamera == null)
                targetCamera = UnityEngine.Camera.main;
            if (boardView == null)
                boardView = FindFirstObjectByType<BoardTilemapView>();
            if (backgroundRenderer == null)
                backgroundRenderer = GetComponent<SpriteRenderer>();
            if (backgroundRenderer != null)
            {
                backgroundRenderer.sortingOrder = -10000;
                backgroundRenderer.color = Color.white;
            }
            FitToBoardCameraRange();
        }

        private void FitToCamera() => FitToBoardCameraRange();

        private void FitToBoardCameraRange()
        {
            if (targetCamera == null || boardView == null || backgroundRenderer == null ||
                backgroundRenderer.sprite == null)
                return;

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var index = 0; index < GaeBullBing.Core.Board.BoardState.DefaultTileCount; index++)
            {
                var position = boardView.GetWorldPosition(index);
                min = Vector2.Min(min, position);
                max = Vector2.Max(max, position);
            }

            var center = (min + max) * .5f;
            transform.position = new Vector3(center.x, center.y, depth);
            transform.rotation = Quaternion.identity;

            var aspect = Mathf.Max(.01f, targetCamera.aspect);
            var overviewHalfHeight = targetCamera.orthographicSize;
            var overviewHalfWidth = overviewHalfHeight * aspect;
            var boardHalfSize = (max - min) * .5f;
            var requiredHalfWidth = Mathf.Max(overviewHalfWidth, boardHalfSize.x + focusSize * aspect);
            var requiredHalfHeight = Mathf.Max(overviewHalfHeight, boardHalfSize.y + focusSize);
            var spriteSize = backgroundRenderer.sprite.bounds.size;
            if (spriteSize.x <= .001f || spriteSize.y <= .001f)
                return;

            // The background stays fixed to the board. Size it for both the overview and
            // a focus camera positioned on any edge tile, while preserving its aspect.
            var scale = Mathf.Max(requiredHalfWidth * 2f / spriteSize.x,
                requiredHalfHeight * 2f / spriteSize.y) * overscan;
            var currentScale = transform.lossyScale.x;
            scale = Mathf.Max(scale, currentScale);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
