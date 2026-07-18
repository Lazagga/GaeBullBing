using System;
using System.Collections.Generic;
using GaeBullBing.Core.Game;
using GaeBullBing.Core.Monsters;

namespace GaeBullBing.Core.Towers
{
    public sealed class TowerEffectService
    {
        public IReadOnlyList<TowerAttackResult> PlaceFireField(GameState state, int tileIndex, int sourceTowerInstanceId = 0) =>
            PlaceTileField(state, tileIndex, true, sourceTowerInstanceId);

        public IReadOnlyList<TowerAttackResult> PlaceIceField(GameState state, int tileIndex, int sourceTowerInstanceId = 0) =>
            PlaceTileField(state, tileIndex, false, sourceTowerInstanceId);

        public IReadOnlyList<TowerAttackResult> ResolveAfterAttacks(GameState state, IReadOnlyList<TowerAttackResult> attacks)
        {
            var extra = new List<TowerAttackResult>();
            foreach (var attack in attacks)
            {
                var towerTile = FindTowerTile(state, attack.TowerInstanceId); if (towerTile == null) continue;
                var target = FindMonster(state, attack.TargetInstanceId);
                var upgrades = towerTile.Tower.AppliedUpgradeIds;
                var attackTileIndex = attack.TargetTileIndex >= 0
                    ? attack.TargetTileIndex
                    : target != null ? target.CurrentTileIndex : -1;
                if (attackTileIndex < 0 || attackTileIndex >= state.Board.TileCount) continue;
                var attackedTiles = new HashSet<int> { attackTileIndex };
                if (towerTile.Tower.DefinitionId == "TOW_04")
                    SpreadTileField(state, attackTileIndex,
                        upgrades.Contains("UPG_ELECTRIC_T2_03") ? 2 : 1,
                        attack.TowerInstanceId, extra);
                if (target != null && upgrades.Contains("UPG_FIRE_T2_01")) target.BurnStacks++;
                if (target != null && upgrades.Contains("UPG_FIRE_T2_04")) target.BurnStacks *= 2;
                if (target != null && upgrades.Contains("UPG_ICE_T2_00") && target.FreezeImmuneLine < 0) target.FrozenMovesRemaining = 1;
                if (target != null && upgrades.Contains("UPG_ELECTRIC_T3_02")) target.Shocked = true;
                if (target != null && upgrades.Contains("UPG_PHYSICS_T3_02") && !target.KnockbackConsumed)
                {
                    var fromTile = target.CurrentTileIndex;
                    var toTile = fromTile == 0 ? 0 : (fromTile + 35) % 36;
                    target.KnockbackImmunityPending = true;
                    target.CurrentTileIndex = toTile;
                    if (toTile != fromTile) target.DistanceTravelled = Math.Max(0, target.DistanceTravelled - 1);
                    extra.Add(new TowerAttackResult(attack.TowerInstanceId, target.InstanceId, 0, false,
                        true, fromTile, toTile, attackTileIndex));
                }
                if (upgrades.Contains("UPG_FIRE_T2_00"))
                {
                    AddAreaTiles(state, attackTileIndex, 1, attackedTiles);
                    DamageArea(state, attackTileIndex, attack.Damage, attack.TowerInstanceId, extra);
                }
                if (target != null && upgrades.Contains("UPG_FIRE_T3_01") && target.BurnStacks >= 10)
                {
                    AddAreaTiles(state, attackTileIndex, 1, attackedTiles);
                    DamageArea(state, attackTileIndex, attack.Damage, attack.TowerInstanceId, extra);
                }
                if (upgrades.Contains("UPG_ELECTRIC_T2_02"))
                {
                    var chainDistance = upgrades.Contains("UPG_ELECTRIC_T2_03") ? 4 : 3;
                    for (var distance = 1; distance <= chainDistance; distance++)
                    {
                        var chainedTile = (attackTileIndex + distance) % state.Board.TileCount;
                        attackedTiles.Add(chainedTile);
                        DamageTile(state, chainedTile, attack.Damage, attack.TowerInstanceId, extra);
                    }
                }
                if (upgrades.Contains("UPG_ELECTRIC_T2_03"))
                    ExpandTileSet(state, attackedTiles, 1);
                if (upgrades.Contains("UPG_FIRE_T3_00"))
                    PlaceFields(state, attackedTiles, true, attack.TowerInstanceId, extra);
                if (upgrades.Contains("UPG_ICE_T2_01"))
                    PlaceFields(state, attackedTiles, false, attack.TowerInstanceId, extra);
                if (target == null || target.IsDead) continue;
                if (upgrades.Contains("UPG_ELECTRIC_T2_00"))
                    SpreadStatuses(state, target, upgrades.Contains("UPG_ELECTRIC_T2_03") ? 2 : 1);
                if (upgrades.Contains("UPG_ICE_T3_01") && state.Board.Tiles[target.CurrentTileIndex].IceTurnsRemaining>0)
                { state.Board.Tiles[target.CurrentTileIndex].IceTurnsRemaining=0; DamageTile(state,target.CurrentTileIndex,attack.Damage,attack.TowerInstanceId,extra); }
                if (upgrades.Contains("UPG_ICE_T3_02") && target.FrozenMovesRemaining>0) ApplyDamage(state,target,attack.Damage*4,attack.TowerInstanceId,extra);
                if (upgrades.Contains("UPG_FIRE_T3_02") && target.BurnStacks>0)
                    ApplyDamage(state, target, attack.Damage * .2f * target.BurnStacks, attack.TowerInstanceId, extra);
            }
            state.Monsters.RemoveAll(m => m.IsDead); return extra;
        }

        public IReadOnlyList<TowerAttackResult> ResolveMonsterTurnEnd(GameState state)
        {
            var results = new List<TowerAttackResult>();
            foreach (var monster in state.Monsters)
            {
                var tile = state.Board.Tiles[monster.CurrentTileIndex];
                if (tile.FireTurnsRemaining > 0)
                {
                    if (!monster.TouchedFireThisMove) monster.BurnStacks++;
                    ApplyDamage(state, monster, BurnDamage(monster), 0, results);
                }
                if (monster.BurnStacks > 0) ApplyDamage(state, monster, BurnDamage(monster), 0, results);
                monster.TouchedFireThisMove = false;
            }
            foreach (var tile in state.Board.Tiles) { if (tile.FireTurnsRemaining > 0) tile.FireTurnsRemaining--; if (tile.IceTurnsRemaining > 0) tile.IceTurnsRemaining--; }
            ResolveStones(state,results);
            state.Monsters.RemoveAll(m => m.IsDead); return results;
        }

        private static float BurnDamage(MonsterState m) => m.MaxHealth * .005f * m.BurnStacks;
        private static void AddAreaTiles(
            GameState state,
            int centerTileIndex,
            int radius,
            ISet<int> tiles)
        {
            var tileCount = state.Board.TileCount;
            for (var offset = -radius; offset <= radius; offset++)
                tiles.Add((centerTileIndex + offset + tileCount) % tileCount);
        }

        private static void ExpandTileSet(GameState state, ISet<int> tiles, int radius)
        {
            var source = new List<int>(tiles);
            foreach (var tileIndex in source)
                AddAreaTiles(state, tileIndex, radius, tiles);
        }

        private void PlaceFields(
            GameState state,
            IEnumerable<int> tileIndices,
            bool placeFire,
            int sourceTowerInstanceId,
            ICollection<TowerAttackResult> results)
        {
            foreach (var tileIndex in tileIndices)
            {
                var fieldResults = placeFire
                    ? PlaceFireField(state, tileIndex, sourceTowerInstanceId)
                    : PlaceIceField(state, tileIndex, sourceTowerInstanceId);
                foreach (var result in fieldResults)
                    results.Add(result);
            }
        }

        private void SpreadTileField(
            GameState state,
            int sourceTileIndex,
            int radius,
            int sourceTowerInstanceId,
            ICollection<TowerAttackResult> results)
        {
            var sourceTile = state.Board.Tiles[sourceTileIndex];
            if (sourceTile.FireTurnsRemaining <= 0 && sourceTile.IceTurnsRemaining <= 0) return;
            var spreadTiles = new HashSet<int>();
            AddAreaTiles(state, sourceTileIndex, radius, spreadTiles);
            spreadTiles.Remove(sourceTileIndex);
            PlaceFields(state, spreadTiles, sourceTile.FireTurnsRemaining > 0,
                sourceTowerInstanceId, results);
        }

        private static IReadOnlyList<TowerAttackResult> PlaceTileField(GameState state, int tileIndex, bool placeFire, int sourceTowerInstanceId)
        {
            var results = new List<TowerAttackResult>();
            var tile = state.Board.Tiles[tileIndex];
            var collides = placeFire ? tile.IceTurnsRemaining > 0 : tile.FireTurnsRemaining > 0;
            if (collides)
            {
                tile.FireTurnsRemaining = 0;
                tile.IceTurnsRemaining = 0;
                DamageTile(state, tileIndex, 15 + state.Difficulty.Level * 14, sourceTowerInstanceId, results);
                state.Monsters.RemoveAll(monster => monster.IsDead);
                return results;
            }

            if (placeFire) tile.FireTurnsRemaining = Board.TileState.OneTurnEffectDuration;
            else tile.IceTurnsRemaining = Board.TileState.OneTurnEffectDuration;
            return results;
        }
        private static void ApplyDamage(GameState state, MonsterState monster, float damage, int tower, ICollection<TowerAttackResult> results)
        { var actual = monster.ReceiveDamage(damage, state.Difficulty); results.Add(new TowerAttackResult(tower,monster.InstanceId,actual,monster.IsDead,targetTileIndex:monster.CurrentTileIndex)); }
        private static void DamageArea(GameState s,int center,float damage,int tower,ICollection<TowerAttackResult> r)
        { foreach(var m in s.Monsters) if(Math.Min(Math.Abs(m.CurrentTileIndex-center),36-Math.Abs(m.CurrentTileIndex-center))<=1) ApplyDamage(s,m,damage,tower,r); }
        private static void SpreadStatuses(GameState s, MonsterState source, int radius)
        { foreach(var m in s.Monsters) if(m.InstanceId!=source.InstanceId && Math.Min(Math.Abs(m.CurrentTileIndex-source.CurrentTileIndex),36-Math.Abs(m.CurrentTileIndex-source.CurrentTileIndex))<=radius) { m.BurnStacks=Math.Max(m.BurnStacks,source.BurnStacks); m.Shocked|=source.Shocked; if(source.FrozenMovesRemaining>0 && m.FreezeImmuneLine<0)m.FrozenMovesRemaining=source.FrozenMovesRemaining; } }
        private static MonsterState FindMonster(GameState s,int id)=>s.Monsters.Find(m=>m.InstanceId==id);
        private static Board.TileState FindTowerTile(GameState s,int id)=>s.Board.Tiles.Find(t=>t.HasTower&&t.Tower.InstanceId==id);
        private static void DamageTile(GameState s,int tile,float damage,int tower,ICollection<TowerAttackResult> r)
        {foreach(var m in s.Monsters)if(m.CurrentTileIndex==tile)ApplyDamage(s,m,damage,tower,r);}
        private static void ResolveStones(GameState state, ICollection<TowerAttackResult> results)
        {
            foreach (var tile in state.Board.Tiles)
            {
                if (!tile.HasTower || !tile.Tower.StoneActive) continue;
                var stone = tile.Tower;
                stone.StoneTraversalTiles.Clear();
                stone.StoneExitAnimation = StoneExitAnimation.None;
                stone.StoneExitTileIndex = -1;

                // Resolve the whole roll in this turn. The final zero-damage step
                // is retained for presentation so the stone can shrink while moving.
                for (var step = 0; step < 12 && stone.StoneActive; step++)
                {
                    if (IsCorner(stone.StoneTileIndex))
                    {
                        stone.StoneExitAnimation = StoneExitAnimation.FallOffBoard;
                        stone.StoneActive = false;
                        break;
                    }

                    var nextTileIndex = (stone.StoneTileIndex + state.Board.TileCount - 1) % state.Board.TileCount;
                    var damage = (int)Math.Floor(stone.StoneBaseDamage * stone.StoneDamageMultiplier + .0001f);
                    if (damage <= 0)
                    {
                        stone.StoneExitAnimation = StoneExitAnimation.ShrinkOnZeroDamage;
                        stone.StoneExitTileIndex = nextTileIndex;
                        stone.StoneActive = false;
                        break;
                    }

                    stone.StoneTileIndex = nextTileIndex;
                    stone.StoneTraversalTiles.Add(stone.StoneTileIndex);
                    ResolveStoneAttack(state, stone, damage, results);
                    stone.StoneDamageMultiplier = Math.Max(0f, stone.StoneDamageMultiplier - .1f);
                    if (IsCorner(stone.StoneTileIndex))
                    {
                        stone.StoneExitAnimation = StoneExitAnimation.FallOffBoard;
                        stone.StoneActive = false;
                    }
                }

                stone.StoneActive = false;
            }
            state.Monsters.RemoveAll(monster => monster.IsDead);
        }

        private static bool IsCorner(int tileIndex) =>
            tileIndex == 0 || tileIndex == 9 || tileIndex == 18 || tileIndex == 27;

        private static void ResolveStoneAttack(
            GameState state,
            TowerState sourceTower,
            int damage,
            ICollection<TowerAttackResult> results)
        {
            var appliesKnockback = sourceTower.AppliedUpgradeIds.Contains("UPG_PHYSICS_T3_02");
            foreach (var monster in new List<MonsterState>(state.Monsters))
            {
                if (monster.IsDead || monster.CurrentTileIndex != sourceTower.StoneTileIndex) continue;

                var fromTile = monster.CurrentTileIndex;
                var actualDamage = monster.ReceiveDamage(damage, state.Difficulty);
                var killed = monster.IsDead;
                var knockbackApplied = appliesKnockback && !killed && !monster.KnockbackConsumed;
                var toTile = fromTile;
                if (knockbackApplied)
                {
                    toTile = fromTile == 0
                        ? 0
                        : (fromTile + state.Board.TileCount - 1) % state.Board.TileCount;
                    monster.KnockbackImmunityPending = true;
                    monster.CurrentTileIndex = toTile;
                    if (toTile != fromTile)
                        monster.DistanceTravelled = Math.Max(0, monster.DistanceTravelled - 1);
                }

                results.Add(new TowerAttackResult(
                    sourceTower.InstanceId,
                    monster.InstanceId,
                    actualDamage,
                    killed,
                    knockbackApplied,
                    fromTile,
                    toTile,
                    fromTile));
            }
        }
    }
}
