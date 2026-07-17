using System.Collections;
using UnityEngine;

namespace GaeBullBing.Presentation.Board
{
    public sealed class PlayerBoardView : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float stepDuration = 0.14f;
        [SerializeField, Min(0f)] private float hopHeight = 0.18f;
        [SerializeField] private Vector3 positionOffset = new(0f, 0.28f, 0f);

        private BoardTilemapView boardView;

        public void Initialize(BoardTilemapView view, int tileIndex)
        {
            boardView = view;
            SnapTo(tileIndex);
        }

        public void SnapTo(int tileIndex)
        {
            EnsureBoardView();
            transform.position = boardView.GetWorldPosition(tileIndex) + positionOffset;
        }

        public IEnumerator MoveSteps(int startTileIndex, int distance)
        {
            EnsureBoardView();

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
                    var position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, progress));
                    position.y += Mathf.Sin(progress * Mathf.PI) * hopHeight;
                    transform.position = position;
                    yield return null;
                }

                transform.position = to;
            }

            boardView.PlayPress((startTileIndex + distance) % GaeBullBing.Core.Board.BoardState.DefaultTileCount);
        }

        private void EnsureBoardView()
        {
            if (boardView == null)
                throw new MissingReferenceException("PlayerBoardView has not been initialized with a BoardTilemapView.");
        }
    }
}
