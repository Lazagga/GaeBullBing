namespace GaeBullBing.Core
{
    public enum TurnPhase
    {
        None,
        GameStart,
        PlayerTurnStart,
        DiceRoll,
        PlayerMove,
        TileResolve,
        MonsterSpawn,
        MonsterMove,
        TowerCombat,
        RoundEnd,
        Victory,
        Defeat
    }

    public enum TileType { Normal, Start, Special }
    public enum TowerElement { None, Fire, Water, Wind, Earth }
    public enum TowerAttackType { Single, Area, Slow }
    public enum DiceUpgradeType { IncreaseWeight, DecreaseWeight, ReplaceFace, RemoveFace }
    public enum MonsterTier { Normal, Boss }
}
