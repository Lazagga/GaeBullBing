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
        [SerializeField, Min(0f)] private float inwardDistance = 0.38f;
        [SerializeField] private float heightOffset = 0.05f;

        private readonly Dictionary<int, SpriteRenderer> towerViews = new();

        public void SetTower(int tileIndex, TowerDefinition definition)
        {
            if (!towerViews.TryGetValue(tileIndex, out var renderer))
            {
                var towerObject = new GameObject($"Tower {tileIndex}");
                towerObject.transform.SetParent(transform, false);
                renderer = towerObject.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 12;
                towerViews.Add(tileIndex, renderer);
            }

            renderer.sprite = towerSprite;
            renderer.color = GetElementColor(definition.Element);
            var tilePosition = boardView.GetWorldPosition(tileIndex);
            var inwardDirection = boardView.GetInwardDirectionWorld(tileIndex);
            renderer.transform.position =
                tilePosition + inwardDirection * inwardDistance + Vector3.up * heightOffset;
            renderer.gameObject.name = $"Tower {tileIndex} ({definition.Id})";
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
