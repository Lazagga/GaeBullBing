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
                if (target != null && upgrades.Contains("UPG_FIRE_T2_01")) target.BurnStacks++;
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
                if (upgrades.Contains("UPG_FIRE_T3_00"))
                    PlaceFields(state, attackedTiles, true, attack.TowerInstanceId, extra);
                if (upgrades.Contains("UPG_ICE_T2_01"))
                    PlaceFields(state, attackedTiles, false, attack.TowerInstanceId, extra);
                if (target == null || target.IsDead) continue;
                if (upgrades.Contains("UPG_ELECTRIC_T2_00")) SpreadStatuses(state, target);
                if (upgrades.Contains("UPG_ELECTRIC_T2_02"))
                    for (var distance = 1; distance <= 3; distance++)
                        DamageTile(state,(target.CurrentTileIndex+distance)%36,attack.Damage,attack.TowerInstanceId,extra);
                if (upgrades.Contains("UPG_ICE_T3_01") && state.Board.Tiles[target.CurrentTileIndex].IceTurnsRemaining>0)
                { state.Board.Tiles[target.CurrentTileIndex].IceTurnsRemaining=0; DamageTile(state,target.CurrentTileIndex,attack.Damage,attack.TowerInstanceId,extra); }
                if (upgrades.Contains("UPG_ICE_T3_02") && target.FrozenMovesRemaining>0) ApplyDamage(target,attack.Damage*4,attack.TowerInstanceId,extra);
                if (upgrades.Contains("UPG_FIRE_T3_02") && target.BurnStacks>0) ApplyDamage(target,BurnDamage(target),attack.TowerInstanceId,extra);
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
                    ApplyDamage(monster, BurnDamage(monster), 0, results);
                }
                if (monster.BurnStacks > 0) ApplyDamage(monster, BurnDamage(monster), 0, results);
                monster.TouchedFireThisMove = false;
            }
            foreach (var tile in state.Board.Tiles) { if (tile.FireTurnsRemaining > 0) tile.FireTurnsRemaining--; if (tile.IceTurnsRemaining > 0) tile.IceTurnsRemaining--; }
            ResolveStones(state,results);
            state.Monsters.RemoveAll(m => m.IsDead); return results;
        }

        private static int BurnDamage(MonsterState m) => Math.Max(1, (int)Math.Ceiling(m.MaxHealth * .005f * m.BurnStacks));
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
        private static void ApplyDamage(MonsterState m, int damage, int tower, ICollection<TowerAttackResult> results)
        { var actual = m.Shocked ? (int)Math.Ceiling(damage * 1.3f) : damage; m.CurrentHealth -= actual; results.Add(new TowerAttackResult(tower,m.InstanceId,actual,m.IsDead,targetTileIndex:m.CurrentTileIndex)); }
        private static void DamageArea(GameState s,int center,int damage,int tower,ICollection<TowerAttackResult> r)
        { foreach(var m in s.Monsters) if(Math.Min(Math.Abs(m.CurrentTileIndex-center),36-Math.Abs(m.CurrentTileIndex-center))<=1) ApplyDamage(m,damage,tower,r); }
        private static void SpreadStatuses(GameState s, MonsterState source)
        { foreach(var m in s.Monsters) if(m.InstanceId!=source.InstanceId && Math.Min(Math.Abs(m.CurrentTileIndex-source.CurrentTileIndex),36-Math.Abs(m.CurrentTileIndex-source.CurrentTileIndex))<=1) { m.BurnStacks=Math.Max(m.BurnStacks,source.BurnStacks); m.Shocked|=source.Shocked; if(source.FrozenMovesRemaining>0 && m.FreezeImmuneLine<0)m.FrozenMovesRemaining=source.FrozenMovesRemaining; } }
        private static MonsterState FindMonster(GameState s,int id)=>s.Monsters.Find(m=>m.InstanceId==id);
        private static Board.TileState FindTowerTile(GameState s,int id)=>s.Board.Tiles.Find(t=>t.HasTower&&t.Tower.InstanceId==id);
        private static void DamageTile(GameState s,int tile,int damage,int tower,ICollection<TowerAttackResult> r)
        {foreach(var m in s.Monsters)if(m.CurrentTileIndex==tile)ApplyDamage(m,damage,tower,r);}
        private static void ResolveStones(GameState state, ICollection<TowerAttackResult> results)
        {
            foreach (var tile in state.Board.Tiles)
            {
                if (!tile.HasTower || !tile.Tower.StoneActive) continue;
                var stone = tile.Tower;
                stone.StoneTraversalTiles.Clear();

                // Resolve the whole roll in this turn. Ten steps is the natural
                // damage-decay limit; reaching a corner ends it earlier.
                for (var step = 0; step < 10 && stone.StoneActive; step++)
                {
                    if (IsCorner(stone.StoneTileIndex))
                    {
                        stone.StoneActive = false;
                        break;
                    }

                    stone.StoneTileIndex = (stone.StoneTileIndex + state.Board.TileCount - 1) % state.Board.TileCount;
                    var damage = (int)Math.Floor(stone.StoneBaseDamage * stone.StoneDamageMultiplier + .0001f);
                    if (damage <= 0)
                    {
                        stone.StoneActive = false;
                        break;
                    }

                    stone.StoneTraversalTiles.Add(stone.StoneTileIndex);
                    ResolveStoneAttack(state, stone, damage, results);
                    stone.StoneDamageMultiplier = Math.Max(0f, stone.StoneDamageMultiplier - .1f);
                    if (IsCorner(stone.StoneTileIndex)) stone.StoneActive = false;
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
                var actualDamage = monster.Shocked ? (int)Math.Ceiling(damage * 1.3f) : damage;
                monster.CurrentHealth -= actualDamage;
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
