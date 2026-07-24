using System.Collections;
using GaeBullBing.Presentation.Board;
using UnityEngine;

namespace GaeBullBing.Presentation.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class BoardCameraController : MonoBehaviour
    {
        // overviewSize에 잘못된 값이 들어왔을 때 사용하는 안전한 기본값입니다.
        private const float DefaultOverviewSize = 4.7f;
        [Header("Camera Zoom")]
        [Tooltip("플레이어, 보스 또는 특정 타일을 확대해서 보여줄 때의 카메라 크기입니다. 값이 작을수록 더 크게 확대됩니다.")]
        [SerializeField, Min(0.1f)] private float focusSize = 2.8f;
        [Tooltip("전체 보드를 보여줄 때의 카메라 크기입니다. 실행 중에는 보드 크기와 화면 비율을 기준으로 자동 계산됩니다.")]
        [SerializeField, Min(0.1f)] private float overviewSize = 4.7f;

        [Header("Board Framing")]
        [Tooltip("화면에 맞춰 전체 보드 영역을 계산할 때 참조하는 보드입니다.")]
        [SerializeField] private BoardTilemapView boardView;
        [Tooltip("보드 좌우에 추가할 월드 단위 여백입니다. 값을 키우면 화면에 보이는 좌우 영역이 넓어집니다.")]
        [SerializeField, Min(0f)] private float horizontalPadding = 0.35f;
        [Tooltip("보드 위아래에 추가할 월드 단위 여백입니다. 값을 키우면 18번 타일 위의 체력바와 상태 아이콘이 더 여유롭게 보입니다.")]
        [SerializeField, Min(0f)] private float verticalPadding = 2.2f;

        [Header("Camera Movement")]
        [Tooltip("전체 화면과 포커스 화면 사이를 전환하는 데 걸리는 시간(초)입니다.")]
        [SerializeField, Min(0.01f)] private float transitionDuration = 0.45f;
        [Tooltip("카메라가 플레이어나 보스를 따라갈 때의 속도입니다. 값이 클수록 빠르게 따라갑니다.")]
        [SerializeField, Min(0.01f)] private float followSpeed = 8f;

        private UnityEngine.Camera controlledCamera;
        private Vector3 overviewPosition;
        private PlayerBoardView followTarget;
        private Transform followTransform;
        private int transitionRevision;
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
                if (followTarget == null && followTransform == null) controlledCamera.orthographicSize = overviewSize;
            }
            if (followTarget == null && followTransform == null)
                return;

            var followPosition = followTarget != null
                ? followTarget.CameraFollowPosition
                : followTransform.position;
            var targetPosition = new Vector3(followPosition.x, followPosition.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
        }

        public IEnumerator FocusOn(PlayerBoardView target)
        {
            var revision = ++transitionRevision;
            followTarget = target;
            followTransform = null;
            var followPosition = target.CameraFollowPosition;
            yield return TransitionTo(
                new Vector3(followPosition.x, followPosition.y, transform.position.z),
                focusSize,
                revision);
        }

        public IEnumerator FocusOn(Transform target)
        {
            var revision = ++transitionRevision;
            followTarget = null;
            followTransform = target;
            var position = target.position;
            yield return TransitionTo(
                new Vector3(position.x, position.y, transform.position.z),
                focusSize,
                revision);
        }

        public IEnumerator FocusOnTile(int tileIndex)
        {
            var revision = ++transitionRevision;
            followTarget = null;
            followTransform = null;
            var position = boardView.GetWorldPosition(tileIndex);
            yield return TransitionTo(
                new Vector3(position.x, position.y, transform.position.z),
                focusSize,
                revision);
        }

        public IEnumerator ReturnToOverview()
        {
            var revision = ++transitionRevision;
            followTarget = null;
            followTransform = null;
            yield return TransitionTo(overviewPosition, overviewSize, revision);
        }

        private IEnumerator TransitionTo(Vector3 targetPosition, float targetSize, int revision)
        {
            if (!float.IsFinite(targetSize) || targetSize < 0.1f)
                targetSize = DefaultOverviewSize;

            var startPosition = transform.position;
            var startSize = controlledCamera.orthographicSize;
            var elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                if (revision != transitionRevision)
                    yield break;
                elapsed += Time.deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));
                transform.position = Vector3.Lerp(startPosition, targetPosition, progress);
                controlledCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, progress);
                yield return null;
            }

            if (revision == transitionRevision)
            {
                transform.position = targetPosition;
                controlledCamera.orthographicSize = targetSize;
            }
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
            // Orthographic Size는 세로 반경이므로, 가로 길이는 화면 비율로 나눠
            // 동일한 세로 크기 단위로 변환한 뒤 더 큰 쪽을 최종 크기로 사용합니다.
            var halfHeight = (max.y - min.y) * 0.5f + verticalPadding;
            var halfWidthAsHeight = ((max.x - min.x) * 0.5f + horizontalPadding) / aspect;
            overviewSize = Mathf.Max(halfHeight, halfWidthAsHeight);
            var center = (min + max) * 0.5f;
            overviewPosition = new Vector3(center.x, center.y, transform.position.z);
        }
    }
}
