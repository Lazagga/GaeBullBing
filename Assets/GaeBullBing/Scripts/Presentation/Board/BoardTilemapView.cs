using System.Collections;
using System.Collections.Generic;
using GaeBullBing.Core.Board;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GaeBullBing.Presentation.Board
{
    [RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
    public sealed class BoardTilemapView : MonoBehaviour
    {
        [SerializeField] private TileBase normalTile;
        [SerializeField] private TileBase frozenTile;
        [SerializeField] private TileBase igniteTile;
        [SerializeField] private TileBase featherTile;
        [SerializeField] private bool buildOnAwake = true;
        [SerializeField, Min(0f)] private float playerPressDepth = 0.1f;
        [SerializeField, Min(0.01f)] private float playerPressDuration = 0.07f;

        private Tilemap tilemap;
        private readonly Dictionary<int, Coroutine> pressRoutines = new();
        private readonly float[] pressAmounts = new float[BoardState.DefaultTileCount];
        private readonly float[] transitionOffsets = new float[BoardState.DefaultTileCount];
        private BoardState currentBoardState;

        public Tilemap Tilemap => tilemap != null ? tilemap : tilemap = GetComponent<Tilemap>();
        public float PressPulseDuration => playerPressDuration * 2f;

        private void Awake()
        {
            ConfigureSorting();
            if (buildOnAwake)
                Rebuild();
        }

        private void Reset() => ConfigureSorting();

        private void ConfigureSorting()
        {
            var tilemapRenderer = GetComponent<TilemapRenderer>();
            if (tilemapRenderer == null) return;

            tilemapRenderer.enabled = true;
            tilemapRenderer.mode = TilemapRenderer.Mode.Individual;
            tilemapRenderer.sortOrder = TilemapRenderer.SortOrder.TopRight;
        }

        [ContextMenu("Rebuild Board")]
        public void Rebuild()
        {
            if (normalTile == null)
            {
                Debug.LogWarning("BoardTilemapView requires a normal tile.", this);
                return;
            }

            Tilemap.ClearAllTiles();
            for (var index = 0; index < BoardLayout.Cells.Count; index++)
            {
                Tilemap.SetTile(GetCellPosition(index), normalTile);
                ApplyPressTransform(index);
            }
        }

public void RefreshTileEffects(BoardState board)
        {
            if (board == null) return;
            currentBoardState = board;
            var count = Mathf.Min(board.TileCount, BoardLayout.Cells.Count);
            for (var index = 0; index < count; index++)
                RefreshTileEffect(board, index);
            RefreshRendererSorting();
        }

public void RefreshTileEffect(BoardState board, int tileIndex)
        {
            if (board == null || tileIndex < 0 || tileIndex >= board.TileCount ||
                tileIndex >= BoardLayout.Cells.Count) return;
            currentBoardState = board;
            var state = board.Tiles[tileIndex];
            var tile = state.HasBossFeather && featherTile != null
                ? featherTile
                : state.FireTurnsRemaining > 0 && igniteTile != null
                    ? igniteTile
                    : state.IceTurnsRemaining > 0 && frozenTile != null
                        ? frozenTile
                        : normalTile;
            Tilemap.SetTile(GetCellPosition(tileIndex), tile);
            Tilemap.SetColor(GetCellPosition(tileIndex), Color.white);
            ApplyPressTransform(tileIndex);
        }


        public Vector3Int GetCellPosition(int tileIndex)
        {
            var cell = BoardLayout.GetCell(tileIndex);
            return new Vector3Int(cell.X, cell.Y, 0);
        }

        public Vector3 GetWorldPosition(int tileIndex) =>
            Tilemap.GetCellCenterWorld(GetCellPosition(tileIndex));

        public void SetBossFeatherVisual(int tileIndex, bool active)
        {
            if (tileIndex < 0 || tileIndex >= BoardLayout.Cells.Count) return;
            if (currentBoardState != null)
            {
                RefreshTileEffects(currentBoardState);
                return;
            }
            var cell = GetCellPosition(tileIndex);
            Tilemap.SetTileFlags(cell, TileFlags.None);
            Tilemap.SetTile(cell, active && featherTile != null ? featherTile : normalTile);
            Tilemap.SetColor(cell, Color.white);
            ApplyPressTransform(tileIndex);
            RefreshRendererSorting();
        }

        private void RefreshRendererSorting()
        {
            Tilemap.RefreshAllTiles();
            ConfigureSorting();
            var tilemapRenderer = GetComponent<TilemapRenderer>();
            if (tilemapRenderer == null) return;
            tilemapRenderer.enabled = false;
            tilemapRenderer.enabled = true;
        }

        public Vector3 GetPlayerStandWorldPosition(int tileIndex) =>
            GetWorldPosition(tileIndex) + GetTileVisualWorldOffset(tileIndex);

        public Vector3 GetTileVisualWorldOffset(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= pressAmounts.Length)
                return Vector3.zero;
            var localOffset = Vector3.up * transitionOffsets[tileIndex] +
                Vector3.down * (playerPressDepth * pressAmounts[tileIndex]);
            return Tilemap.transform.TransformVector(localOffset);
        }

        public void SetTransitionOffset(int tileIndex, float localYOffset)
        {
            if (tileIndex < 0 || tileIndex >= transitionOffsets.Length)
                return;
            transitionOffsets[tileIndex] = localYOffset;
            ApplyPressTransform(tileIndex);
        }

        public void SetAllTransitionOffsets(float localYOffset)
        {
            for (var index = 0; index < transitionOffsets.Length; index++)
                SetTransitionOffset(index, localYOffset);
        }

        public Vector3 GetBoardCenterWorld()
        {
            var center = Vector3.zero;
            for (var index = 0; index < BoardLayout.Cells.Count; index++)
                center += GetWorldPosition(index);
            return center / BoardLayout.Cells.Count;
        }

        public Vector3 GetInwardDirectionWorld(int tileIndex)
        {
            var cell = GetCellPosition(tileIndex);
            var last = BoardLayout.SideLength - 1;
            Vector3Int inwardCellDirection;

            if (cell.x == 0 && cell.y > 0 && cell.y < last)
                inwardCellDirection = Vector3Int.right;
            else if (cell.y == last && cell.x > 0 && cell.x < last)
                inwardCellDirection = Vector3Int.down;
            else if (cell.x == last && cell.y > 0 && cell.y < last)
                inwardCellDirection = Vector3Int.left;
            else if (cell.y == 0 && cell.x > 0 && cell.x < last)
                inwardCellDirection = Vector3Int.up;
            else
                return Vector3.zero;

            return (Tilemap.CellToWorld(cell + inwardCellDirection) - Tilemap.CellToWorld(cell)).normalized;
        }

        public void ResetPress(int tileIndex)
        {
            SetPressAmount(tileIndex, 0f, true);
        }

        public void ReleasePlayerTile(int tileIndex)
        {
            SetPressAmount(tileIndex, 0f, false);
        }

        public void PlayPress(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= pressAmounts.Length)
                return;
            if (pressRoutines.Remove(tileIndex, out var routine) && routine != null)
                StopCoroutine(routine);
            pressRoutines[tileIndex] = StartCoroutine(AnimatePressPulse(tileIndex));
        }

        private void SetPressAmount(int tileIndex, float target, bool instant)
        {
            if (tileIndex < 0 || tileIndex >= pressAmounts.Length)
                return;
            if (pressRoutines.Remove(tileIndex, out var routine) && routine != null)
                StopCoroutine(routine);

            if (instant)
            {
                pressAmounts[tileIndex] = target;
                ApplyPressTransform(tileIndex);
                return;
            }

            pressRoutines[tileIndex] = StartCoroutine(AnimatePress(tileIndex, target));
        }

        private IEnumerator AnimatePressPulse(int tileIndex)
        {
            yield return AnimatePressPhase(tileIndex, 1f);
            yield return AnimatePressPhase(tileIndex, 0f);
            pressRoutines.Remove(tileIndex);
        }

        private IEnumerator AnimatePress(int tileIndex, float target)
        {
            yield return AnimatePressPhase(tileIndex, target);
            pressRoutines.Remove(tileIndex);
        }

        private IEnumerator AnimatePressPhase(int tileIndex, float target)
        {
            var start = pressAmounts[tileIndex];
            var elapsed = 0f;
            while (elapsed < playerPressDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / playerPressDuration));
                pressAmounts[tileIndex] = Mathf.Lerp(start, target, progress);
                ApplyPressTransform(tileIndex);
                yield return null;
            }

            pressAmounts[tileIndex] = target;
            ApplyPressTransform(tileIndex);
        }

        private void ApplyPressTransform(int tileIndex)
        {
            var cell = GetCellPosition(tileIndex);
            Tilemap.SetTileFlags(cell, TileFlags.None);
            Tilemap.SetTransformMatrix(cell, Matrix4x4.TRS(
                Vector3.up * transitionOffsets[tileIndex] +
                Vector3.down * (playerPressDepth * pressAmounts[tileIndex]),
                Quaternion.identity,
                Vector3.one));
        }
    }
}
