using UnityEngine;

namespace GaeBullBing.Presentation.Board
{
    internal static class BoardDepthSorting
    {
        private const int BaseOrder = 1000;
        private const float Precision = 100f;

        public static int GetOrder(Vector3 worldPosition, int priority = 0)
        {
            // On the isometric board, a lower world Y is closer to the camera.
            return BaseOrder - Mathf.RoundToInt(worldPosition.y * Precision) + priority;
        }
    }
}
