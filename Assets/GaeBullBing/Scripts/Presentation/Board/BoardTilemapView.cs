using GaeBullBing.Core.Board;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GaeBullBing.Presentation.Board
{
    [RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
    public sealed class BoardTilemapView : MonoBehaviour
    {
        [SerializeField] private TileBase normalTile;
        [SerializeField] private bool buildOnAwake = true;

        private Tilemap tilemap;

        public Tilemap Tilemap => tilemap != null ? tilemap : tilemap = GetComponent<Tilemap>();

        private void Awake()
        {
            ConfigureSorting();
            if (buildOnAwake)
                Rebuild();
        }

        private void OnValidate() => ConfigureSorting();

        private void ConfigureSorting()
        {
            var tilemapRenderer = GetComponent<TilemapRenderer>();
            if (tilemapRenderer == null) return;

            // Draw the back/top cells first so the lower/front cells overlap them.
            tilemapRenderer.mode = TilemapRenderer.Mode.Individual;
            tilemapRenderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;
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
                Tilemap.SetTile(GetCellPosition(index), normalTile);
        }

        public Vector3Int GetCellPosition(int tileIndex)
        {
            var cell = BoardLayout.GetCell(tileIndex);
            return new Vector3Int(cell.X, cell.Y, 0);
        }

        public Vector3 GetWorldPosition(int tileIndex) =>
            Tilemap.GetCellCenterWorld(GetCellPosition(tileIndex));

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

        public void PlayPress(int tileIndex)
        {
            // Sprite-frame press animation will be connected here later.
            _ = GetCellPosition(tileIndex);
        }
    }
}
