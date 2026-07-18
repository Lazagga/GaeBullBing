using UnityEngine;

namespace GaeBullBing.Presentation.Board
{
    internal static class BoardDepthSorting
    {
        private const int BaseOrder = 1000;
        private const float Precision = 100f;
        private const int TowerFrontOffset = 30;

        public static int GetOrder(Vector3 worldPosition, int priority = 0)
        {
            // A lower world Y is closer and must be rendered later/on top.
            return BaseOrder - Mathf.RoundToInt(worldPosition.y * Precision) + priority;
        }

        public static int GetTowerOrder(Vector3 worldPosition, int tileIndex)
        {
            // On the right/back half (lines 1 and 2), towers occlude actors.
            // On the left/front half (lines 0 and 3), actors occlude towers.
            var towerIsInFront = tileIndex > 9 && tileIndex < 27;
            return GetOrder(worldPosition, towerIsInFront ? TowerFrontOffset : 0);
        }
    }
}
