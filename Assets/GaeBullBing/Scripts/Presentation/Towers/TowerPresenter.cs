using System.Collections.Generic;
using GaeBullBing.Core;
using GaeBullBing.Core.Data;
using GaeBullBing.Presentation.Board;
using UnityEngine;

namespace GaeBullBing.Presentation.Towers
{
    public sealed class TowerPresenter : MonoBehaviour
    {
        [SerializeField] private BoardTilemapView boardView;
        [SerializeField] private Sprite towerSprite;
        [SerializeField] private Sprite[] fireSprites = new Sprite[6];
        [SerializeField] private Sprite[] iceSprites = new Sprite[6];
        [SerializeField] private Sprite[] physicsSprites = new Sprite[6];
        [SerializeField] private Sprite[] electricSprites = new Sprite[6];
        private readonly Dictionary<int, SpriteRenderer> towerViews = new();

        public void SetTower(int tileIndex, TowerDefinition definition, int tier = 1)
        {
            if (!towerViews.TryGetValue(tileIndex, out var renderer))
            {
                var towerObject = new GameObject($"Tower {tileIndex}");
                towerObject.transform.SetParent(transform, false);
                renderer = towerObject.AddComponent<SpriteRenderer>();
                towerViews.Add(tileIndex, renderer);
            }

            var tilePosition = boardView.GetWorldPosition(tileIndex);
            var inwardDirection = boardView.GetInwardDirectionWorld(tileIndex);
            // Art direction names describe the tower's board-side position,
            // which is opposite to the direction from the tile toward center.
            renderer.sprite = GetTowerSprite(definition.Element, tier, -inwardDirection, out var flipX);
            renderer.flipX = flipX;
            renderer.color = renderer.sprite != null && renderer.sprite != towerSprite
                ? Color.white
                : GetElementColor(definition.Element);
            renderer.transform.localScale = Vector3.one;
            renderer.transform.position = tilePosition;
            renderer.sortingOrder = BoardDepthSorting.GetTowerOrder(tilePosition, tileIndex);
            renderer.gameObject.name = $"Tower {tileIndex} ({definition.Id})";
        }

        private Sprite GetTowerSprite(
            TowerElement element,
            int tier,
            Vector3 inwardDirection,
            out bool flipX)
        {
            var sprites = element switch
            {
                TowerElement.Fire => fireSprites,
                TowerElement.Ice => iceSprites,
                TowerElement.Physics => physicsSprites,
                TowerElement.Electric => electricSprites,
                _ => null
            };

            flipX = false;
            if (sprites == null || sprites.Length < 6)
                return towerSprite;

            var clampedTier = Mathf.Clamp(tier, 1, 3);
            var pointsRight = inwardDirection.x > 0f;
            var pointsUp = inwardDirection.y > 0f;
            int index;

            if (clampedTier == 1)
            {
                // Tier 1 originals: top-right and bottom-left.
                index = pointsUp ? 0 : 1;
                flipX = pointsUp ? !pointsRight : pointsRight;
            }
            else
            {
                // Tier 2/3 originals: top-left and bottom-right.
                index = (clampedTier - 1) * 2 + (pointsUp ? 0 : 1);
                flipX = pointsUp ? pointsRight : !pointsRight;
            }

            return sprites[index] != null ? sprites[index] : towerSprite;
        }

        private static Color GetElementColor(TowerElement element)
        {
            return element switch
            {
                TowerElement.Fire => new Color(1f, 0.28f, 0.08f),
                TowerElement.Ice => new Color(0.25f, 0.75f, 1f),
                TowerElement.Physics => new Color(0.72f, 0.72f, 0.72f),
                TowerElement.Electric => new Color(1f, 0.9f, 0.12f),
                _ => Color.white
            };
        }
    }
}
