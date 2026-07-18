using System;
using System.Collections.Generic;
using GaeBullBing.Core.Board;
using GaeBullBing.Core.Game;
using GaeBullBing.Core.Monsters;

namespace GaeBullBing.Core.Towers
{
    public enum TowerAttackVisualKind
    {
        None,
        Projectile,
        AreaTile,
        ChainLine,
        RollingStone
    }

    public readonly struct TowerCombatStats
    {
        public TowerCombatStats(int damage, int range, int targetCount, int attackCount = 1)
        {
            Damage = damage;
            Range = range;
            TargetCount = targetCount;
            AttackCount = attackCount;
        }

        public int Damage { get; }
        public int Range { get; }
        public int TargetCount { get; }
        public int AttackCount { get; }
    }

    public readonly struct TowerAttackResult
    {
        public TowerAttackResult(int towerInstanceId, int targetInstanceId, float damage, bool killed,
            bool knockbackApplied = false, int knockbackFromTile = -1, int knockbackToTile = -1,
            int targetTileIndex = -1, TowerAttackVisualKind visualKind = TowerAttackVisualKind.None)
        {
            TowerInstanceId = towerInstanceId;
            TargetInstanceId = targetInstanceId;
            Damage = damage;
            Killed = killed;
            KnockbackApplied = knockbackApplied;
            KnockbackFromTile = knockbackFromTile;
            KnockbackToTile = knockbackToTile;
            TargetTileIndex = targetTileIndex;
            VisualKind = visualKind;
        }

        public int TowerInstanceId { get; }
        public int TargetInstanceId { get; }
        public float Damage { get; }
        public bool Killed { get; }
        public bool KnockbackApplied { get; }
        public int KnockbackFromTile { get; }
        public int KnockbackToTile { get; }
        public int TargetTileIndex { get; }
        public TowerAttackVisualKind VisualKind { get; }
        public TowerAttackResult WithKnockback(int fromTile, int toTile) =>
            new(TowerInstanceId, TargetInstanceId, Damage, Killed, true, fromTile, toTile,
                TargetTileIndex, VisualKind);
    }

    public sealed class TowerCombatService
    {
        public IReadOnlyList<TowerAttackResult> Resolve(
            GameState state,
            IReadOnlyDictionary<string, TowerCombatStats> statsByDefinitionId)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (statsByDefinitionId == null)
                throw new ArgumentNullException(nameof(statsByDefinitionId));

            var results = new List<TowerAttackResult>();
            // Monsters advance clockwise from low to high tile indices, so towers
            // nearer the end of the route resolve first.
            for (var tileIndex = state.Board.Tiles.Count - 1; tileIndex >= 0; tileIndex--)
            {
                var tile = state.Board.Tiles[tileIndex];
                if (!tile.HasTower || tile.Tower.IsFeatherSealed ||
                    !statsByDefinitionId.TryGetValue(tile.Tower.DefinitionId, out var stats))
                    continue;

                for (var attack = 0; attack < Math.Max(1, stats.AttackCount); attack++)
                    ResolveTowerAttack(state, tile, stats, results);
            }
            return results;
        }

        public IReadOnlyList<TowerAttackResult> ResolveByTower(
            GameState state,
            IReadOnlyDictionary<int, TowerCombatStats> statsByTowerInstanceId)
        {
            var results = new List<TowerAttackResult>();
            for (var tileIndex = state.Board.Tiles.Count - 1; tileIndex >= 0; tileIndex--)
            {
                var tile = state.Board.Tiles[tileIndex];
                if (!tile.HasTower || tile.Tower.IsFeatherSealed ||
                    !statsByTowerInstanceId.TryGetValue(tile.Tower.InstanceId, out var stats))
                    continue;
                var tower = tile.Tower; var upgrades = tower.AppliedUpgradeIds;
                if (tower.BonusAttackTurnsRemaining > 0) tower.BonusAttackTurnsRemaining--;
                else tower.BonusAttackCount = 0;
                if (upgrades.Contains("UPG_PHYSICS_T2_03")) continue;
                if (upgrades.Contains("UPG_PHYSICS_T2_01") && tower.AttackCooldownRounds > 0) { tower.AttackCooldownRounds--; continue; }
                if (upgrades.Contains("UPG_PHYSICS_T2_00") && !tower.StoneActive)
                {
                    tower.StoneActive = true;
                    tower.StoneTileIndex = tile.Index;
                    tower.StoneDamageMultiplier = 1f;
                    tower.StoneBaseDamage = stats.Damage;
                    tower.StoneExitAnimation = StoneExitAnimation.None;
                    tower.StoneExitTileIndex = -1;
                }
                if (upgrades.Contains("UPG_PHYSICS_T3_00")) { ResolvePhysicsGuard(state,tile,stats.Damage,results); continue; }
                if (HasEffect(tower, "chain_line", "UPG_ELECTRIC_T3_00")) { ResolveLineAttack(state,tile,stats.Damage,results); continue; }
                if (upgrades.Contains("UPG_ELECTRIC_T3_01")) { ResolveRandomElectric(state,tile,stats.Damage,results); continue; }
                for (var attack = 0; attack < Math.Max(1, stats.AttackCount+tower.BonusAttackCount); attack++)
                {
                    if (HasEffect(tower, "range_attack", "UPG_ICE_T3_00"))
                        ResolveRangeAttack(state, tile, stats, results);
                    else if (HasEffect(tower, "aoe_tile", "UPG_FIRE_T2_03", "UPG_ICE_T2_03"))
                        ResolveTileAreaAttack(state, tile, stats, results);
                    else
                        ResolveTowerAttack(state,tile,stats,results);
                }
                if (upgrades.Contains("UPG_PHYSICS_T2_01")) tower.AttackCooldownRounds=2;
                if (upgrades.Contains("UPG_ELECTRIC_T2_01")) BuffAttackedTileTower(state,results,tower.InstanceId);
            }
            return results;
        }

        private static void ResolvePhysicsGuard(GameState s,TileState tile,int damage,ICollection<TowerAttackResult> r)
        { foreach(var m in new List<MonsterState>(s.Monsters)) if(m.CurrentTileIndex==tile.Index&&m.PhysicsGuardTriggeredThisTurn){m.PhysicsGuardTriggeredThisTurn=false;Damage(s,m,damage,tile.Tower.InstanceId,r,TowerAttackVisualKind.Projectile);} s.Monsters.RemoveAll(m=>m.IsDead); }
        private static void ResolveLineAttack(GameState s,TileState tile,int damage,ICollection<TowerAttackResult> r)
        { var line=MonsterService.GetLine(tile.Index);foreach(var m in new List<MonsterState>(s.Monsters))if(MonsterService.GetLine(m.CurrentTileIndex)==line)Damage(s,m,damage,tile.Tower.InstanceId,r,TowerAttackVisualKind.ChainLine);s.Monsters.RemoveAll(m=>m.IsDead); }
        private static readonly Random EffectRandom=new Random();
        private static void ResolveRandomElectric(GameState s,TileState source,int damage,ICollection<TowerAttackResult> r)
        {
            var tiles=s.Board.Tiles.FindAll(t=>t.HasTower&&t.Tower.DefinitionId=="TOW_04"&&t.Tower.InstanceId!=source.Tower.InstanceId);
            if(tiles.Count==0)return;
            var occupied=tiles.FindAll(t=>s.Monsters.Exists(m=>!m.IsDead&&m.CurrentTileIndex==t.Index));
            var candidates=occupied.Count>0?occupied:tiles;
            for(var i=0;i<3;i++)
            {
                var chosen=candidates[EffectRandom.Next(candidates.Count)];
                foreach(var m in new List<MonsterState>(s.Monsters))if(m.CurrentTileIndex==chosen.Index)Damage(s,m,damage,source.Tower.InstanceId,r,TowerAttackVisualKind.Projectile);
            }
            s.Monsters.RemoveAll(m=>m.IsDead);
        }
        private static void BuffAttackedTileTower(GameState s,IEnumerable<TowerAttackResult> results,int source)
        { foreach(var a in results)if(a.TowerInstanceId==source){var m=s.Monsters.Find(x=>x.InstanceId==a.TargetInstanceId);if(m!=null){var t=s.Board.Tiles[m.CurrentTileIndex];if(t.HasTower){t.Tower.BonusAttackCount=1;t.Tower.BonusAttackTurnsRemaining=2;}}} }
        private static void Damage(GameState state,MonsterState monster,float damage,int tower,ICollection<TowerAttackResult> results,
            TowerAttackVisualKind visualKind = TowerAttackVisualKind.None)
        {var actual=monster.ReceiveDamage(damage,state.Difficulty);results.Add(new TowerAttackResult(tower,monster.InstanceId,actual,monster.IsDead,targetTileIndex:monster.CurrentTileIndex,visualKind:visualKind));}

        private static void ResolveTowerAttack(
            GameState state,
            TileState tile,
            TowerCombatStats stats,
            ICollection<TowerAttackResult> results)
        {
            var tower = tile.Tower;
            var candidates = new List<MonsterState>();
            foreach (var monster in state.Monsters)
            {
                if (!monster.IsDead && GetBoardDistance(tile.Index, monster.CurrentTileIndex) <= stats.Range)
                    candidates.Add(monster);
            }

            candidates.Sort((left, right) => CompareTargets(tower, tile.Index, left, right));
            tower.TargetInstanceIds.Clear();

            var selectedCount = Math.Min(stats.TargetCount, candidates.Count);
            for (var index = 0; index < selectedCount; index++)
            {
                var target = candidates[index];
                var actualDamage = target.ReceiveDamage(stats.Damage, state.Difficulty);
                var killed = target.IsDead;
                results.Add(new TowerAttackResult(tower.InstanceId, target.InstanceId, actualDamage, killed,
                    targetTileIndex: target.CurrentTileIndex, visualKind: TowerAttackVisualKind.Projectile));
                if (!killed)
                    tower.TargetInstanceIds.Add(target.InstanceId);
            }

            state.Monsters.RemoveAll(monster => monster.IsDead);
        }

        private static void ResolveTileAreaAttack(
            GameState state,
            TileState tile,
            TowerCombatStats stats,
            ICollection<TowerAttackResult> results)
        {
            var tower = tile.Tower;
            var candidates = new List<MonsterState>();
            foreach (var monster in state.Monsters)
                if (!monster.IsDead && GetBoardDistance(tile.Index, monster.CurrentTileIndex) <= stats.Range)
                    candidates.Add(monster);
            candidates.Sort((left, right) => CompareTargets(tower, tile.Index, left, right));

            var selectedTiles = new HashSet<int>();
            foreach (var candidate in candidates)
            {
                if (selectedTiles.Count >= stats.TargetCount) break;
                selectedTiles.Add(candidate.CurrentTileIndex);
            }

            tower.TargetInstanceIds.Clear();
            AddAreaTileMarkers(tower.InstanceId, selectedTiles, results);
            foreach (var monster in new List<MonsterState>(state.Monsters))
            {
                if (monster.IsDead || !selectedTiles.Contains(monster.CurrentTileIndex)) continue;
                var actualDamage = monster.ReceiveDamage(stats.Damage, state.Difficulty);
                results.Add(new TowerAttackResult(tower.InstanceId, monster.InstanceId, actualDamage,
                    monster.IsDead, targetTileIndex: monster.CurrentTileIndex));
                if (!monster.IsDead) tower.TargetInstanceIds.Add(monster.InstanceId);
            }
            state.Monsters.RemoveAll(monster => monster.IsDead);
        }

        private static void ResolveRangeAttack(
            GameState state,
            TileState tile,
            TowerCombatStats stats,
            ICollection<TowerAttackResult> results)
        {
            var targets = new List<MonsterState>();
            var attackedTiles = new HashSet<int>();
            foreach (var monster in state.Monsters)
                if (!monster.IsDead && GetBoardDistance(tile.Index, monster.CurrentTileIndex) <= stats.Range)
                {
                    targets.Add(monster);
                    attackedTiles.Add(monster.CurrentTileIndex);
                }
            targets.Sort((left, right) => CompareTargets(tile.Tower, tile.Index, left, right));
            AddAreaTileMarkers(tile.Tower.InstanceId, attackedTiles, results);
            tile.Tower.TargetInstanceIds.Clear();
            foreach (var monster in targets)
            {
                var actualDamage = monster.ReceiveDamage(stats.Damage, state.Difficulty);
                results.Add(new TowerAttackResult(tile.Tower.InstanceId, monster.InstanceId, actualDamage,
                    monster.IsDead, targetTileIndex: monster.CurrentTileIndex));
                if (!monster.IsDead) tile.Tower.TargetInstanceIds.Add(monster.InstanceId);
            }
            state.Monsters.RemoveAll(monster => monster.IsDead);
        }

        private static void AddAreaTileMarkers(
            int towerInstanceId,
            IEnumerable<int> tileIndices,
            ICollection<TowerAttackResult> results)
        {
            foreach (var tileIndex in tileIndices)
                results.Add(new TowerAttackResult(towerInstanceId, 0, 0f, false,
                    targetTileIndex: tileIndex, visualKind: TowerAttackVisualKind.AreaTile));
        }

        private static bool HasEffect(TowerState tower, string effectId, params string[] legacyUpgradeIds)
        {
            if (tower.AppliedEffectIds.Contains(effectId)) return true;
            foreach (var upgradeId in legacyUpgradeIds)
                if (tower.AppliedUpgradeIds.Contains(upgradeId)) return true;
            return false;
        }

        private static int CompareTargets(
            TowerState tower,
            int towerTileIndex,
            MonsterState left,
            MonsterState right)
        {
            var leftWasTargeted = tower.TargetInstanceIds.Contains(left.InstanceId);
            var rightWasTargeted = tower.TargetInstanceIds.Contains(right.InstanceId);
            var comparison = rightWasTargeted.CompareTo(leftWasTargeted);
            if (comparison != 0)
                return comparison;

            comparison = GetBoardDistance(towerTileIndex, left.CurrentTileIndex)
                .CompareTo(GetBoardDistance(towerTileIndex, right.CurrentTileIndex));
            if (comparison != 0)
                return comparison;

            comparison = right.CurrentHealth.CompareTo(left.CurrentHealth);
            if (comparison != 0)
                return comparison;

            comparison = right.DistanceTravelled.CompareTo(left.DistanceTravelled);
            return comparison != 0 ? comparison : left.InstanceId.CompareTo(right.InstanceId);
        }

        public static int GetBoardDistance(int firstTileIndex, int secondTileIndex)
        {
            var directDistance = Math.Abs(firstTileIndex - secondTileIndex);
            return Math.Min(directDistance, BoardState.DefaultTileCount - directDistance);
        }
    }
}
