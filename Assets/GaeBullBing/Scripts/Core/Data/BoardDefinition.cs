using System;
using UnityEngine;

namespace GaeBullBing.Core.Data
{
    [CreateAssetMenu(menuName = "GaeBullBing/Data/Board", fileName = "BoardDefinition")]
    public sealed class BoardDefinition : ScriptableObject
    {
        [SerializeField] private BoardTilePlacement[] tiles = Array.Empty<BoardTilePlacement>();
        public BoardTilePlacement[] Tiles => tiles;
    }

    [Serializable]
    public struct BoardTilePlacement
    {
        public int Index;
        public TileDefinition Definition;
    }
}
