using System.Collections;
using System.Collections.Generic;
using GaeBullBing.Core.Game;
using GaeBullBing.Presentation.Board;
using UnityEngine;

namespace GaeBullBing.Presentation.Towers
{
    public sealed class StonePresenter : MonoBehaviour
    {
        [SerializeField] private BoardTilemapView boardView;
        [SerializeField, Min(0.05f)] private float diameter = 0.42f;

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
                foreach (var tileIndex in tower.StoneTraversalTiles)
                {
                    var position = boardView.GetWorldPosition(tileIndex);
                    renderer.transform.position = position;
                    renderer.sortingOrder = BoardDepthSorting.GetOrder(position, 25);
                    yield return new WaitForSeconds(0.07f);
                }
                renderer.gameObject.SetActive(false);
                tower.StoneTraversalTiles.Clear();
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
