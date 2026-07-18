using System.Collections;
using System.Collections.Generic;
using GaeBullBing.Core.Game;
using GaeBullBing.Core.Towers;
using GaeBullBing.Presentation.Board;
using UnityEngine;

namespace GaeBullBing.Presentation.Towers
{
    public sealed class StonePresenter : MonoBehaviour
    {
        [SerializeField] private BoardTilemapView boardView;
        [SerializeField, Min(0.05f)] private float diameter = 0.62f;
        [SerializeField, Min(0.05f)] private float moveDuration = 0.2f;
        [SerializeField, Min(0.05f)] private float exitDuration = 0.45f;
        [SerializeField, Min(0.1f)] private float fallDistance = 1.15f;

        private readonly Dictionary<int, SpriteRenderer> views = new();
        private Sprite circleSprite;

        public void Initialize(BoardTilemapView view)
        {
            boardView = view;
            if (circleSprite == null) circleSprite = CreateCircleSprite();
        }

        public void Refresh(GameState state)
        {
            if (state == null || boardView == null) return;
            if (circleSprite == null) circleSprite = CreateCircleSprite();

            var activeIds = new HashSet<int>();
            foreach (var tile in state.Board.Tiles)
            {
                if (!tile.HasTower || !tile.Tower.StoneActive) continue;

                var tower = tile.Tower;
                activeIds.Add(tower.InstanceId);
                if (!views.TryGetValue(tower.InstanceId, out var renderer))
                {
                    var stoneObject = new GameObject($"Stone ({tower.InstanceId})");
                    stoneObject.transform.SetParent(transform, false);
                    renderer = stoneObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = circleSprite;
                    renderer.color = Color.white;
                    views.Add(tower.InstanceId, renderer);
                }

                var position = boardView.GetWorldPosition(tower.StoneTileIndex);
                renderer.transform.position = position;
                renderer.transform.localScale = Vector3.one * diameter;
                renderer.sortingOrder = BoardDepthSorting.GetOrder(position, 25);
                renderer.gameObject.SetActive(true);
            }

            foreach (var pair in views)
                if (!activeIds.Contains(pair.Key)) pair.Value.gameObject.SetActive(false);
        }

        public IEnumerator PlayResolvedMovement(GameState state)
        {
            if (state == null || boardView == null) yield break;
            foreach (var tile in state.Board.Tiles)
            {
                if (!tile.HasTower || tile.Tower.StoneTraversalTiles.Count == 0) continue;
                var tower = tile.Tower;
                if (!views.TryGetValue(tower.InstanceId, out var renderer)) continue;

                renderer.gameObject.SetActive(true);
                renderer.color = Color.white;
                renderer.transform.localScale = Vector3.one * diameter;
                var previousPosition = renderer.transform.position;
                foreach (var tileIndex in tower.StoneTraversalTiles)
                {
                    var position = boardView.GetWorldPosition(tileIndex);
                    yield return MoveStone(renderer, previousPosition, position, moveDuration, false);
                    previousPosition = position;
                }

                if (tower.StoneExitAnimation == StoneExitAnimation.FallOffBoard)
                {
                    var direction = tower.StoneTraversalTiles.Count > 1
                        ? previousPosition - boardView.GetWorldPosition(
                            tower.StoneTraversalTiles[tower.StoneTraversalTiles.Count - 2])
                        : previousPosition - boardView.GetWorldPosition(tile.Index);
                    if (direction.sqrMagnitude < 0.001f) direction = Vector3.down;
                    var target = previousPosition + direction.normalized * fallDistance + Vector3.down * 0.45f;
                    yield return MoveStone(renderer, previousPosition, target, exitDuration, true);
                }
                else if (tower.StoneExitAnimation == StoneExitAnimation.ShrinkOnZeroDamage &&
                         tower.StoneExitTileIndex >= 0)
                {
                    var target = boardView.GetWorldPosition(tower.StoneExitTileIndex);
                    yield return MoveStone(renderer, previousPosition, target, exitDuration, true);
                }

                renderer.gameObject.SetActive(false);
                tower.StoneTraversalTiles.Clear();
                tower.StoneExitAnimation = StoneExitAnimation.None;
                tower.StoneExitTileIndex = -1;
            }
        }

        private IEnumerator MoveStone(
            SpriteRenderer renderer,
            Vector3 start,
            Vector3 end,
            float duration,
            bool disappear)
        {
            var elapsed = 0f;
            var startScale = Vector3.one * diameter;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var normalized = Mathf.Clamp01(elapsed / duration);
                var eased = normalized * normalized * (3f - 2f * normalized);
                var position = Vector3.LerpUnclamped(start, end, eased);
                renderer.transform.position = position;
                renderer.sortingOrder = BoardDepthSorting.GetOrder(position, 25);
                if (disappear)
                {
                    renderer.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, eased);
                    var color = renderer.color;
                    color.a = 1f - eased;
                    renderer.color = color;
                }
                yield return null;
            }

            renderer.transform.position = end;
            if (disappear)
            {
                renderer.transform.localScale = Vector3.zero;
                var color = renderer.color;
                color.a = 0f;
                renderer.color = color;
            }
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Temporary Stone Circle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            var radiusSquared = center * center;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = x - center;
                var dy = y - center;
                pixels[y * size + x] = dx * dx + dy * dy <= radiusSquared
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        }
    }
}
