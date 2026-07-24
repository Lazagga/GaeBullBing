namespace GaeBullBing.Core
{
    public enum TurnPhase
    {
        None,
        GameStart,
        PlayerTurnStart,
        DiceRoll,
        DiceTuning,
        PlayerMove,
        TileResolve,
        TileAction,
        TowerSelection,
        CornerSelection,
        TowerResolve,
        CameraOverview,
        MonsterSpawn,
        MonsterMove,
        TowerCombat,
        RoundEnd,
        Victory,
        Defeat
    }

    public enum TileType { Normal, Start, Special }
    public enum TowerElement { None, Fire, Ice, Physics, Electric }
    public enum MonsterTier { Normal, Boss }
}
