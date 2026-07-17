using UnityEngine;

namespace GaeBullBing.Presentation.Board
{
    internal static class BoardDepthSorting
    {
        private const int BaseOrder = 1000;
        private const float Precision = 100f;

        public static int GetOrder(Vector3 worldPosition, int priority = 0)
        {
            // A lower world Y is closer and must be rendered later/on top.
            return BaseOrder - Mathf.RoundToInt(worldPosition.y * Precision) + priority;
        }
    }
}
