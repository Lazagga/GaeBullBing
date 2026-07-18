using System.Collections;
using GaeBullBing.Presentation.Board;
using UnityEngine;

namespace GaeBullBing.Presentation.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class BoardCameraController : MonoBehaviour
    {
        private const float DefaultOverviewSize = 4.7f;

        [SerializeField, Min(0.1f)] private float focusSize = 2.8f;
        [SerializeField, Min(0.1f)] private float overviewSize = 4.7f;
        [SerializeField] private BoardTilemapView boardView;
        [SerializeField, Min(0f)] private float horizontalPadding = 0.65f;
        [SerializeField, Min(0f)] private float verticalPadding = 1.35f;
        [SerializeField, Min(0.01f)] private float transitionDuration = 0.45f;
        [SerializeField, Min(0.01f)] private float followSpeed = 8f;

        private UnityEngine.Camera controlledCamera;
        private Vector3 overviewPosition;
        private PlayerBoardView followTarget;
        private int lastScreenWidth;
        private int lastScreenHeight;

        private void Awake()
        {
            ValidateSizes();
            controlledCamera = GetComponent<UnityEngine.Camera>();
            overviewPosition = transform.position;
            RefreshResponsiveOverview();
            controlledCamera.orthographicSize = overviewSize;
        }

        private void OnValidate() => ValidateSizes();

        private void ValidateSizes()
        {
            if (!float.IsFinite(overviewSize) || overviewSize < 0.1f)
                overviewSize = DefaultOverviewSize;
            if (!float.IsFinite(focusSize) || focusSize < 0.1f)
                focusSize = 2.8f;
        }

        private void LateUpdate()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                RefreshResponsiveOverview();
                if (followTarget == null) controlledCamera.orthographicSize = overviewSize;
            }
            if (followTarget == null)
                return;

            var followPosition = followTarget.CameraFollowPosition;
            var targetPosition = new Vector3(followPosition.x, followPosition.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
        }

        public IEnumerator FocusOn(PlayerBoardView target)
        {
            followTarget = target;
            var followPosition = target.CameraFollowPosition;
            yield return TransitionTo(
                new Vector3(followPosition.x, followPosition.y, transform.position.z),
                focusSize);
        }

        public IEnumerator FocusOnTile(int tileIndex)
        {
            followTarget = null;
            var position = boardView.GetWorldPosition(tileIndex);
            yield return TransitionTo(
                new Vector3(position.x, position.y, transform.position.z),
                focusSize);
        }

        public IEnumerator ReturnToOverview()
        {
            followTarget = null;
            yield return TransitionTo(overviewPosition, overviewSize);
        }

        private IEnumerator TransitionTo(Vector3 targetPosition, float targetSize)
        {
            if (!float.IsFinite(targetSize) || targetSize < 0.1f)
                targetSize = DefaultOverviewSize;

            var startPosition = transform.position;
            var startSize = controlledCamera.orthographicSize;
            var elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));
                transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
                controlledCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, progress);
                yield return null;
            }

            transform.position = targetPosition;
            controlledCamera.orthographicSize = targetSize;
        }

        private void RefreshResponsiveOverview()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            if (boardView == null || lastScreenHeight <= 0) return;

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var index = 0; index < GaeBullBing.Core.Board.BoardState.DefaultTileCount; index++)
            {
                var position = boardView.GetWorldPosition(index);
                min = Vector2.Min(min, position);
                max = Vector2.Max(max, position);
            }

            var aspect = (float)lastScreenWidth / lastScreenHeight;
            var halfHeight = (max.y - min.y) * 0.5f + verticalPadding;
            var halfWidthAsHeight = ((max.x - min.x) * 0.5f + horizontalPadding) / aspect;
            overviewSize = Mathf.Max(halfHeight, halfWidthAsHeight);
            var center = (min + max) * 0.5f;
            overviewPosition = new Vector3(center.x, center.y, transform.position.z);
        }
    }
}
