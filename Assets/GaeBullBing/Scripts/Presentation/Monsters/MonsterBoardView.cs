using System.Collections;
using GaeBullBing.Presentation.Board;
using UnityEngine;

namespace GaeBullBing.Presentation.Monsters
{
    public sealed class MonsterBoardView : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float stepDuration = 0.18f;
        [SerializeField] private Vector3 positionOffset = new(-0.18f, 0.22f, 0f);

        private BoardTilemapView boardView;

        public int InstanceId { get; private set; }

        public void Initialize(int instanceId, BoardTilemapView view, int tileIndex)
        {
            InstanceId = instanceId;
            boardView = view;
            transform.position = boardView.GetWorldPosition(tileIndex) + positionOffset;
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
            }
        }
    }
}
