using GaeBullBing.Core.Board;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GaeBullBing.Presentation.Board
{
    [RequireComponent(typeof(Tilemap))]
    public sealed class BoardTilemapView : MonoBehaviour
    {
        [SerializeField] private TileBase normalTile;
        [SerializeField] private bool buildOnAwake = true;

        private Tilemap tilemap;

        public Tilemap Tilemap => tilemap != null ? tilemap : tilemap = GetComponent<Tilemap>();

        private void Awake()
        {
            if (buildOnAwake)
                Rebuild();
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

        public void PlayPress(int tileIndex)
        {
            // Sprite-frame press animation will be connected here later.
            _ = GetCellPosition(tileIndex);
        }
    }
}
