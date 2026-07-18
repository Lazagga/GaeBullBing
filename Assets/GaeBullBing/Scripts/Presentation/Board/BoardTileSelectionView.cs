using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

namespace GaeBullBing.Presentation.Board
{
    public sealed class BoardTileSelectionView : MonoBehaviour
    {
        [SerializeField] private Color hoverColor = new(1f, .82f, .25f, 1f);
        [SerializeField, Min(.1f)] private float selectionRadius = .75f;

        private BoardTilemapView boardView;
        private Action<int> selected;
        private Action<int> selectionHovered;
        private Action selectionHoverExited;
        private Action<int> inspected;
        private Action inspectionClosed;
        private Func<bool> inputAllowed;
        private int hoveredIndex = -1;
        private Color hoveredOriginalColor;

        public void Initialize(BoardTilemapView view, Func<bool> canReceiveInput = null)
        {
            boardView = view;
            inputAllowed = canReceiveInput;
        }

        public void BeginSelection(
            Action<int> onSelected,
            Action<int> onHovered = null,
            Action onHoverExited = null)
        {
            EndSelection();
            selected = onSelected;
            selectionHovered = onHovered;
            selectionHoverExited = onHoverExited;
        }

        public void EnableInspection(Action<int> onInspected, Action onClosed)
        {
            inspected = onInspected;
            inspectionClosed = onClosed;
        }

        private void Update()
        {
            if (inputAllowed != null && !inputAllowed())
            {
                SetHovered(-1);
                return;
            }
            if (selected == null && inspected == null || boardView == null || Mouse.current == null)
                return;

            var camera = UnityEngine.Camera.main;
            if (camera == null) return;
            var ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            var plane = new Plane(Vector3.forward, boardView.transform.position);
            if (!plane.Raycast(ray, out var distance)) { SetHovered(-1); return; }

            var world = ray.GetPoint(distance);
            var nearest = FindNearestTile(world);
            SetHovered(nearest);

            if (!Mouse.current.leftButton.wasPressedThisFrame ||
                EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (selected != null)
            {
                if (nearest >= 0) Select(nearest);
                return;
            }

            if (nearest >= 0) inspected?.Invoke(nearest);
            else inspectionClosed?.Invoke();
        }

        public void EndSelection()
        {
            SetHovered(-1);
            selected = null;
            selectionHovered = null;
            selectionHoverExited = null;
        }

        private int FindNearestTile(Vector3 world)
        {
            var nearest = -1; var nearestDistance = selectionRadius * selectionRadius;
            for (var index = 0; index < GaeBullBing.Core.Board.BoardState.DefaultTileCount; index++)
            {
                var sqrDistance = (boardView.GetWorldPosition(index) - world).sqrMagnitude;
                if (sqrDistance <= nearestDistance) { nearestDistance = sqrDistance; nearest = index; }
            }
            return nearest;
        }

        private void SetHovered(int index)
        {
            if (hoveredIndex == index) return;
            if (hoveredIndex >= 0)
                boardView.Tilemap.SetColor(boardView.GetCellPosition(hoveredIndex), hoveredOriginalColor);
            hoveredIndex = index;
            if (hoveredIndex < 0)
            {
                if (selected != null)
                    selectionHoverExited?.Invoke();
                return;
            }
            var cell = boardView.GetCellPosition(hoveredIndex);
            boardView.Tilemap.SetTileFlags(cell, TileFlags.None);
            hoveredOriginalColor = boardView.Tilemap.GetColor(cell);
            boardView.Tilemap.SetColor(cell, hoverColor);
            if (selected != null)
                selectionHovered?.Invoke(hoveredIndex);
        }

        private void Select(int index)
        {
            var callback = selected;
            EndSelection();
            callback?.Invoke(index);
        }
    }
}
