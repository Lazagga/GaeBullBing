using System.Collections;
using GaeBullBing.Presentation.Board;
using UnityEngine;

namespace GaeBullBing.Presentation.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class BoardCameraController : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float focusSize = 2.8f;
        [SerializeField, Min(0.01f)] private float transitionDuration = 0.45f;
        [SerializeField, Min(0.01f)] private float followSpeed = 8f;

        private UnityEngine.Camera controlledCamera;
        private Vector3 overviewPosition;
        private float overviewSize;
        private PlayerBoardView followTarget;

        private void Awake()
        {
            controlledCamera = GetComponent<UnityEngine.Camera>();
            overviewPosition = transform.position;
            overviewSize = controlledCamera.orthographicSize;
        }

        private void LateUpdate()
        {
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

        public IEnumerator ReturnToOverview()
        {
            followTarget = null;
            yield return TransitionTo(overviewPosition, overviewSize);
        }

        private IEnumerator TransitionTo(Vector3 targetPosition, float targetSize)
        {
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
    }
}
