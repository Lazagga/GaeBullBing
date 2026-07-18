using System.Collections;
using GaeBullBing.Core.Board;
using GaeBullBing.Presentation.Board;
using GaeBullBing.Presentation.Monsters;
using UnityEngine;

namespace GaeBullBing.Presentation.Game
{
    public sealed class GameBoardTransition : MonoBehaviour
    {
        [SerializeField] private BoardTilemapView boardView;
        [SerializeField] private PlayerBoardView playerView;
        [SerializeField] private MonsterPresenter monsterPresenter;
        [SerializeField, Min(0.5f)] private float verticalDistance = 6f;
        [SerializeField, Min(0.05f)] private float tileDuration = .28f;
        [SerializeField, Min(0f)] private float tileStagger = .035f;
        [SerializeField, Min(0.05f)] private float actorDuration = .45f;

        public void PrepareHidden()
        {
            boardView.SetAllTransitionOffsets(verticalDistance);
            SetActorOffset(verticalDistance);
        }

        public IEnumerator PlayIntro()
        {
            PrepareHidden();
            yield return AnimateTiles(true);
            yield return AnimateActors(verticalDistance, 0f);
            boardView.PlayPress(playerView.CurrentTileIndex);
            yield return new WaitForSeconds(.14f);
        }

        public IEnumerator PlayOutro()
        {
            yield return AnimateActors(0f, verticalDistance);
            yield return AnimateTiles(false);
        }

        private IEnumerator AnimateTiles(bool entering)
        {
            var count = BoardState.DefaultTileCount;
            var totalDuration = tileDuration + tileStagger * (count - 1);
            for (var elapsed = 0f; elapsed < totalDuration; elapsed += Time.deltaTime)
            {
                for (var index = 0; index < count; index++)
                {
                    var sequenceIndex = entering ? index : count - 1 - index;
                    var progress = Mathf.Clamp01((elapsed - sequenceIndex * tileStagger) / tileDuration);
                    var eased = Mathf.SmoothStep(0f, 1f, progress);
                    boardView.SetTransitionOffset(index, entering
                        ? Mathf.Lerp(verticalDistance, 0f, eased)
                        : Mathf.Lerp(0f, verticalDistance, eased));
                }
                yield return null;
            }
            boardView.SetAllTransitionOffsets(entering ? 0f : verticalDistance);
        }

        private IEnumerator AnimateActors(float from, float to)
        {
            for (var elapsed = 0f; elapsed < actorDuration; elapsed += Time.deltaTime)
            {
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / actorDuration));
                SetActorOffset(Mathf.Lerp(from, to, progress));
                yield return null;
            }
            SetActorOffset(to);
        }

        private void SetActorOffset(float y)
        {
            var offset = Vector3.up * y;
            playerView.SetTransitionOffset(offset);
            monsterPresenter.SetTransitionOffset(offset);
        }
    }
}
