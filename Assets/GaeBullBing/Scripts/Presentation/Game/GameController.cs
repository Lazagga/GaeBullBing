using System.Collections;
using System.Collections.Generic;
using GaeBullBing.Core;
using GaeBullBing.Core.Board;
using GaeBullBing.Core.Dice;
using GaeBullBing.Core.Game;
using GaeBullBing.Core.Data;
using GaeBullBing.Core.Monsters;
using GaeBullBing.Core.Towers;
using GaeBullBing.Presentation.Board;
using GaeBullBing.Presentation.Camera;
using GaeBullBing.Presentation.Dice;
using GaeBullBing.Presentation.Monsters;
using GaeBullBing.Presentation.Towers;
using GaeBullBing.Presentation.UI;
using UnityEngine;
using System.Text;
using System.Globalization;

namespace GaeBullBing.Presentation.Game
{
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private BoardTilemapView boardView;
        [SerializeField] private PlayerBoardView playerView;
        [SerializeField] private DiceHudView diceHud;
        [SerializeField] private MonsterPresenter monsterPresenter;
        [SerializeField] private MonsterDefinition defaultMonster;
        [SerializeField] private BoardDefinition boardDefinition;
        [SerializeField] private MonsterDefinition[] monsterDefinitions;
        [SerializeField] private TextAsset difficultyPatternJson;
        [SerializeField] private DifficultyPatternData[] difficultyPatterns;
        [SerializeField] private BoardCameraController cameraController;
        [SerializeField] private RadialActionMenu radialMenu;
        [SerializeField] private CornerActionMenu cornerActionMenu;
        [SerializeField] private DiceTuningView diceTuningView;
        [SerializeField] private TowerPresenter towerPresenter;
        [SerializeField] private TowerDefinition[] towerDefinitions;
        [SerializeField] private TowerUpgradeDefinition[] towerUpgradeDefinitions;
        [SerializeField, Min(0f)] private float diceRevealDelay = 0.35f;
        [SerializeField, Min(1)] private int cornerDamageBonus = 10;

        private bool isBusy;
        private MonsterDatabase monsterDatabase;
        private DifficultyService difficultyService;
        private Dice3DPresenter dice3DPresenter;
        private string nextMonsterOverrideId;
        private BoardTileSelectionView tileSelectionView;
        private bool pendingDiceTuning;
        private bool diceTuningComplete;

        public GameState State { get; private set; }
        public GameSession Session { get; private set; }

        public bool SetNextDiceResults(int first, int second, out string message)
        {
            if (first < 1 || first > 6 || second < 1 || second > 6) { message = "주사위 값은 1~6이어야 합니다."; return false; }
            Session.SetNextDiceResults(first, second); message = $"다음 주사위: {first}, {second}"; return true;
        }

        public bool SetNextMonster(string query, out string message)
        {
            var normalizedQuery = NormalizeConsoleToken(query);
            foreach (var definition in monsterDefinitions)
                if (definition != null && (NormalizeConsoleToken(definition.Id) == normalizedQuery || NormalizeConsoleToken(definition.DisplayName) == normalizedQuery))
                { nextMonsterOverrideId = definition.Id; message = $"다음 몬스터: {definition.DisplayName} ({definition.Id})"; return true; }
            message = $"몬스터를 찾을 수 없습니다: {query}"; return false;
        }

        public bool SpawnMonsterFromConsole(string query, int tileIndex, out string message)
        {
            if (tileIndex < 0 || tileIndex >= State.Board.TileCount)
            {
                message = $"타일 번호는 0~{State.Board.TileCount - 1} 범위여야 합니다.";
                return false;
            }

            var normalizedQuery = NormalizeConsoleToken(query);
            MonsterDefinition definition = null;
            foreach (var candidate in monsterDefinitions)
                if (candidate != null && (NormalizeConsoleToken(candidate.Id) == normalizedQuery ||
                    NormalizeConsoleToken(candidate.DisplayName) == normalizedQuery))
                {
                    definition = candidate;
                    break;
                }

            if (definition == null)
            {
                message = $"몬스터를 찾을 수 없습니다: {query}";
                return false;
            }

            var previousPhase = State.CurrentPhase;
            var monster = Session.SpawnMonster(definition, difficultyService.GetHealthMultiplier(State.Difficulty));
            monster.CurrentTileIndex = tileIndex;
            monster.DistanceTravelled = tileIndex;
            State.CurrentPhase = previousPhase;
            monsterPresenter.Spawn(monster);
            monsterPresenter.RefreshLayout();
            message = $"{definition.DisplayName} ({definition.Id})을 {tileIndex}번 타일에 소환했습니다.";
            return true;
        }

        public bool BuildTowerFromConsole(int tileIndex, int tier, out string message)
        {
            if (tileIndex < 0 || tileIndex >= State.Board.TileCount)
            {
                message = $"타일 번호는 0~{State.Board.TileCount - 1} 범위여야 합니다.";
                return false;
            }

            if (tier < 1 || tier > 3)
            {
                message = "타워 티어는 1~3 범위여야 합니다.";
                return false;
            }

            var tile = State.Board.Tiles[tileIndex];
            if (!tile.CanBuildTower)
            {
                message = $"{tileIndex}번 타일에는 지정된 타워가 없습니다.";
                return false;
            }

            var definition = FindTowerDefinition(tile.BuildTowerDefinitionId);
            if (definition == null)
            {
                message = $"타워 데이터를 찾을 수 없습니다: {tile.BuildTowerDefinitionId}";
                return false;
            }

            var upgrades = new List<TowerUpgradeDefinition>();
            for (var upgradeTier = 2; upgradeTier <= tier; upgradeTier++)
            {
                var upgrade = FindConsoleUpgrade(definition.Element, upgradeTier);
                if (upgrade == null)
                {
                    message = $"{definition.Element} {upgradeTier}티어의 0번 강화를 찾을 수 없습니다.";
                    return false;
                }
                upgrades.Add(upgrade);
            }

            var previousPhase = State.CurrentPhase;
            try
            {
                tile.Tower = null;
                Session.BuildTower(tileIndex, definition.Id);
                foreach (var upgrade in upgrades)
                    Session.UpgradeTower(tileIndex, upgrade);
                towerPresenter.SetTower(tileIndex, definition, tier);
                message = $"{tileIndex}번 타일에 {definition.DisplayName} {tier}티어 설치 완료";
                return true;
            }
            finally
            {
                State.CurrentPhase = previousPhase;
            }
        }

        public bool SetTileEffectFromConsole(int tileIndex, string effectName, out string message)
        {
            if (tileIndex < 0 || tileIndex >= State.Board.TileCount)
            {
                message = $"타일 번호는 0~{State.Board.TileCount - 1} 범위여야 합니다.";
                return false;
            }

            var tile = State.Board.Tiles[tileIndex];
            IReadOnlyList<TowerAttackResult> results;
            var exploded = false;
            if (effectName.Equals("frozen", System.StringComparison.OrdinalIgnoreCase))
            {
                exploded = tile.FireTurnsRemaining > 0;
                results = Session.PlaceIceField(tileIndex);
                message = exploded
                    ? $"{tileIndex}번 타일의 불/얼음 장판이 폭발했습니다. 피해: {15 + State.Difficulty.Level * 14}"
                    : $"{tileIndex}번 타일에 얼음 장판을 1턴 동안 설치했습니다.";
            }
            else if (effectName.Equals("ignite", System.StringComparison.OrdinalIgnoreCase))
            {
                exploded = tile.IceTurnsRemaining > 0;
                results = Session.PlaceFireField(tileIndex);
                message = exploded
                    ? $"{tileIndex}번 타일의 불/얼음 장판이 폭발했습니다. 피해: {15 + State.Difficulty.Level * 14}"
                    : $"{tileIndex}번 타일에 불 장판을 1턴 동안 설치했습니다.";
            }
            else
            {
                message = "장판 종류는 frozen 또는 ignite여야 합니다.";
                return false;
            }

            boardView.RefreshTileEffects(State.Board);
            if (results.Count > 0) StartCoroutine(ApplyConsoleEffectResults(results));
            return true;
        }

        private IEnumerator ApplyConsoleEffectResults(IReadOnlyList<TowerAttackResult> results)
        {
            var killedCount = 0;
            foreach (var result in results)
            {
                yield return monsterPresenter.ApplyAttack(result);
                if (result.Killed) killedCount++;
            }
            difficultyService.AddKills(State.Difficulty, killedCount);
            monsterPresenter.RefreshLayout();
        }

        private TowerUpgradeDefinition FindConsoleUpgrade(TowerElement element, int tier)
        {
            foreach (var upgrade in towerUpgradeDefinitions)
                if (upgrade != null && upgrade.Element == element && upgrade.Tier == tier &&
                    upgrade.Id.EndsWith("_00", System.StringComparison.OrdinalIgnoreCase))
                    return upgrade;
            return null;
        }

        private static string NormalizeConsoleToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var normalized = value.Trim().Normalize(NormalizationForm.FormKC);
            var builder = new StringBuilder(normalized.Length);
            foreach (var character in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category != UnicodeCategory.Format && category != UnicodeCategory.Control)
                    builder.Append(char.ToUpperInvariant(character));
            }
            return builder.ToString();
        }

        private void Awake()
        {
            State = new GameState();
            Session = new GameSession(
                State,
                new BoardService(),
                new PlayerMovementService(),
                new WeightedDiceRoller(new SystemDiceRandom()),
                new MonsterService(),
                new TowerService(),
                new TowerCombatService());
            Session.StartNewGame(boardDefinition: boardDefinition);
            if (monsterDefinitions == null || monsterDefinitions.Length == 0)
                monsterDefinitions = monsterPresenter != null && monsterPresenter.MonsterDefinitions != null && monsterPresenter.MonsterDefinitions.Length > 0
                    ? monsterPresenter.MonsterDefinitions
                    : new[] { defaultMonster };
            LoadDifficultyPatternsFromJson();
            if (difficultyPatterns == null || difficultyPatterns.Length == 0)
                difficultyPatterns = new[] { new DifficultyPatternData { MonsterIds = new[] { defaultMonster.Id } } };
            monsterDatabase = new MonsterDatabase(monsterDefinitions);
            difficultyService = new DifficultyService(difficultyPatterns);
            difficultyService.Reset(State.Difficulty);
            dice3DPresenter = GetComponent<Dice3DPresenter>();
            if (dice3DPresenter == null)
                dice3DPresenter = gameObject.AddComponent<Dice3DPresenter>();
            dice3DPresenter.Initialize(boardView);
            tileSelectionView = boardView.GetComponent<BoardTileSelectionView>();
            if (tileSelectionView == null) tileSelectionView = boardView.gameObject.AddComponent<BoardTileSelectionView>();
            tileSelectionView.Initialize(boardView);
        }

        private void LoadDifficultyPatternsFromJson()
        {
            if (difficultyPatternJson == null) return;
            var source = JsonUtility.FromJson<DifficultyPatternDatabaseJson>(difficultyPatternJson.text);
            if (source?.wave_patterns == null || source.wave_patterns.Length == 0)
            {
                Debug.LogError($"난이도 패턴 배열을 읽을 수 없습니다: {difficultyPatternJson.name}");
                return;
            }

            if (source.wavedata == null || source.wavedata.Length == 0)
            {
                Debug.LogError($"공통 난이도 설정인 wavedata를 읽을 수 없습니다: {difficultyPatternJson.name}");
                return;
            }

            var killsPerLevel = Mathf.Max(1, source.wavedata[0].required_kills);
            var healthMultiplierPerLevel = source.wavedata[0].multiplier > 0f
                ? source.wavedata[0].multiplier
                : 1f;

            System.Array.Sort(source.wave_patterns, (left, right) => left.level.CompareTo(right.level));
            var knownMonsterIds = new HashSet<string>();
            foreach (var monster in monsterDefinitions)
                if (monster != null) knownMonsterIds.Add(monster.Id);

            var converted = new List<DifficultyPatternData>(source.wave_patterns.Length);
            var cumulativeRequiredKills = 0;
            foreach (var pattern in source.wave_patterns)
            {
                if (pattern == null || pattern.spawn_pattern == null || pattern.spawn_pattern.Length == 0)
                {
                    Debug.LogError($"난이도 {pattern?.level ?? 0}의 spawn_pattern이 비어 있습니다.");
                    continue;
                }

                var valid = true;
                foreach (var monsterId in pattern.spawn_pattern)
                    if (!knownMonsterIds.Contains(monsterId))
                    {
                        Debug.LogError($"난이도 {pattern.level}가 존재하지 않는 몬스터를 참조합니다: {monsterId}");
                        valid = false;
                    }
                if (!valid) continue;

                converted.Add(new DifficultyPatternData
                {
                    RequiredKills = cumulativeRequiredKills,
                    HealthMultiplier = Mathf.Pow(healthMultiplierPerLevel, converted.Count),
                    MonsterIds = pattern.spawn_pattern
                });
                cumulativeRequiredKills += killsPerLevel;
            }

            if (converted.Count > 0) difficultyPatterns = converted.ToArray();
        }

        [System.Serializable]
        private sealed class DifficultyPatternDatabaseJson
        {
            public DifficultyCommonJson[] wavedata;
            public DifficultyPatternJson[] wave_patterns;
        }

        [System.Serializable]
        private sealed class DifficultyCommonJson
        {
            public int required_kills;
            public float multiplier;
        }

        [System.Serializable]
        private sealed class DifficultyPatternJson
        {
            public int level;
            public string[] spawn_pattern;
        }

        private void Start()
        {
            playerView.Initialize(boardView, State.Player.CurrentTileIndex);
            playerView.TileEntered += monsterPresenter.SetPlayerTile;
            monsterPresenter.SetPlayerTile(State.Player.CurrentTileIndex);
            diceHud.Bind(this);
        }

        public void RollDiceAndMovePlayer()
        {
            if (!isBusy)
                StartCoroutine(RollAndMoveRoutine());
        }

        private IEnumerator RollAndMoveRoutine()
        {
            isBusy = true;
            diceHud.SetRolling(true);
            yield return new WaitForSeconds(diceRevealDelay);

            var startTileIndex = State.Player.CurrentTileIndex;
            var distance = Session.RollDiceAndMovePlayer();
            pendingDiceTuning |= startTileIndex + distance >= State.Board.TileCount;
            diceHud.SetResults(State.LastDiceResults[0], State.LastDiceResults[1]);
            yield return dice3DPresenter.Roll(State.LastDiceResults[0], State.LastDiceResults[1]);
            yield return cameraController.FocusOn(playerView);
            yield return playerView.MoveSteps(startTileIndex, distance);
            BeginCurrentTileAction();
        }

        private void BeginCurrentTileAction()
        {
            State.CurrentPhase = TurnPhase.TileAction;
            diceHud.SetBusy();
            var tile = State.Board.Tiles[State.Player.CurrentTileIndex];
            if (TryOpenCornerAction(State.Player.CurrentTileIndex))
                return;
            if (!tile.HasTower && !tile.CanBuildTower)
            {
                StartCoroutine(CompleteTileActionRoutine());
                return;
            }
            radialMenu.ShowPrimary(
                playerView.transform,
                UnityEngine.Camera.main,
                tile.HasTower,
                OpenTowerChoices);
        }

        private bool TryOpenCornerAction(int tileIndex)
        {
            if (cornerActionMenu == null) return false;
            if (tileIndex == 9 || tileIndex == 27)
            {
                State.CurrentPhase = TurnPhase.CornerSelection;
                cornerActionMenu.ShowElementSelection(SelectCornerElement);
                return true;
            }
            if (tileIndex == 0 || tileIndex == 18)
            {
                State.CurrentPhase = TurnPhase.CornerSelection;
                cornerActionMenu.Hide();
                StartCoroutine(PrepareTileSelectionRoutine());
                return true;
            }
            return false;
        }

        private IEnumerator PrepareTileSelectionRoutine()
        {
            yield return cameraController.ReturnToOverview();
            tileSelectionView.BeginSelection(SelectTeleportDestination);
        }

        public void SelectCornerElement(TowerElement element)
        {
            if (State.CurrentPhase != TurnPhase.CornerSelection || element == TowerElement.None) return;
            Session.AddPermanentTowerDamageBonus(element, cornerDamageBonus);
            cornerActionMenu.Hide(); StartCoroutine(CompleteTileActionRoutine());
        }

        public void SelectTeleportDestination(int tileIndex)
        {
            if (State.CurrentPhase != TurnPhase.CornerSelection || tileIndex < 0 || tileIndex >= State.Board.TileCount) return;
            StartCoroutine(MoveToSelectedTileRoutine(tileIndex));
        }

        private IEnumerator MoveToSelectedTileRoutine(int tileIndex)
        {
            var start = State.Player.CurrentTileIndex;
            var distance = (tileIndex - start + State.Board.TileCount) % State.Board.TileCount;
            if (start == 0 || distance > 0 && start + distance >= State.Board.TileCount)
                pendingDiceTuning = true;
            var focusRoutine = StartCoroutine(cameraController.FocusOn(playerView));
            yield return playerView.MoveSteps(start, distance);
            yield return focusRoutine;
            Session.TeleportPlayer(tileIndex);
            if (tileIndex == 0 || tileIndex == 18)
            {
                yield return CompleteTileActionRoutine();
                yield break;
            }
            BeginCurrentTileAction();
        }

        public void OpenTowerChoices()
        {
            if (State.CurrentPhase != TurnPhase.TileAction)
                return;

            State.CurrentPhase = TurnPhase.TowerSelection;
            var tile = State.Board.Tiles[State.Player.CurrentTileIndex];
            if (tile.HasTower)
            {
                var upgrades = GetUpgradeChoices(tile);
                if (upgrades.Count == 0) { radialMenu.Hide(); StartCoroutine(CompleteTileActionRoutine()); return; }
                radialMenu.ShowUpgradeChoices(upgrades, SelectUpgrade);
                return;
            }
            var choices = GetTowerChoices(tile);
            if (choices.Count == 0)
            {
                radialMenu.Hide();
                StartCoroutine(CompleteTileActionRoutine());
                return;
            }

            if (choices.Count == 1)
            {
                SelectTower(choices[0]);
                return;
            }

            radialMenu.ShowChoices(choices, SelectTower);
        }

        public void SelectTower(TowerDefinition definition)
        {
            if (State.CurrentPhase != TurnPhase.TowerSelection || definition == null)
                return;

            var tileIndex = State.Player.CurrentTileIndex;
            var tile = State.Board.Tiles[tileIndex];
            Session.BuildTower(tileIndex, definition.Id);

            towerPresenter.SetTower(tileIndex, definition);
            radialMenu.Hide();
            StartCoroutine(CompleteTileActionRoutine());
        }

        public void SelectUpgrade(TowerUpgradeDefinition upgrade)
        {
            if (State.CurrentPhase != TurnPhase.TowerSelection || upgrade == null) return;
            var tileIndex = State.Player.CurrentTileIndex;
            Session.UpgradeTower(tileIndex, upgrade);
            var tile = State.Board.Tiles[tileIndex];
            var definition = FindTowerDefinition(tile.Tower.DefinitionId);
            if (definition != null)
                towerPresenter.SetTower(tileIndex, definition, tile.Tower.UpgradeTier);
            radialMenu.Hide(); StartCoroutine(CompleteTileActionRoutine());
        }

        private List<TowerDefinition> GetTowerChoices(TileState tile)
        {
            var choices = new List<TowerDefinition>();
            if (!tile.HasTower)
            {
                var buildDefinition = FindTowerDefinition(tile.BuildTowerDefinitionId);
                if (buildDefinition != null)
                    choices.Add(buildDefinition);
                return choices;
            }

            return choices;
        }

        private List<TowerUpgradeDefinition> GetUpgradeChoices(TileState tile)
        {
            var result = new List<TowerUpgradeDefinition>();
            var tower = FindTowerDefinition(tile.Tower.DefinitionId); if (tower == null) return result;
            var pool = new List<TowerUpgradeDefinition>();
            foreach (var upgrade in towerUpgradeDefinitions)
                if (upgrade != null && upgrade.Element == tower.Element && upgrade.Tier == tile.Tower.UpgradeTier + 1 && !tile.Tower.AppliedUpgradeIds.Contains(upgrade.Id)) pool.Add(upgrade);
            while (pool.Count > 0 && result.Count < 3)
            {
                var total = 0; foreach (var item in pool) total += Mathf.Max(0, item.Weight);
                var roll = total > 0 ? Random.Range(0, total) : Random.Range(0, pool.Count);
                var selected = 0;
                if (total > 0) { for (var i = 0; i < pool.Count; i++) { roll -= Mathf.Max(0, pool[i].Weight); if (roll < 0) { selected = i; break; } } }
                else selected = roll;
                result.Add(pool[selected]); pool.RemoveAt(selected);
            }
            return result;
        }

        private TowerDefinition FindTowerDefinition(string id)
        {
            foreach (var definition in towerDefinitions)
                if (definition != null && definition.Id == id)
                    return definition;
            return null;
        }

        private IEnumerator CompleteTileActionRoutine()
        {
            State.CurrentPhase = TurnPhase.CameraOverview;
            yield return cameraController.ReturnToOverview();
            if (pendingDiceTuning && diceTuningView != null)
            {
                pendingDiceTuning = false;
                diceTuningComplete = false;
                State.CurrentPhase = TurnPhase.DiceTuning;
                diceTuningView.Show(State.Dice, ApplyDiceTuning);
                yield return new WaitUntil(() => diceTuningComplete);
            }
            yield return ResolveEnemyTurnRoutine();
        }

        private bool ApplyDiceTuning(int diceIndex, int faceValue, int delta)
        {
            if (!Session.TryShiftDiceWeight(diceIndex, faceValue, delta))
                return false;
            diceTuningComplete = true;
            return true;
        }

        private IEnumerator ResolveEnemyTurnRoutine()
        {
            isBusy = true;
            diceHud.SetBusy();

            var scheduledMonsterId = difficultyService.GetNextMonsterId(State.Difficulty);
            var monsterId = string.IsNullOrEmpty(nextMonsterOverrideId) ? scheduledMonsterId : nextMonsterOverrideId;
            nextMonsterOverrideId = null;
            var definition = monsterDatabase.Get(monsterId);
            var spawnedMonster = Session.SpawnMonster(definition,
                difficultyService.GetHealthMultiplier(State.Difficulty));
            monsterPresenter.Spawn(spawnedMonster);
            var moveResults = Session.MoveMonsters();
            foreach (var result in moveResults)
                yield return monsterPresenter.Move(result);
            monsterPresenter.RefreshLayout();

            if (State.IsGameOver)
            {
                radialMenu.Hide();
                diceHud.ShowGameOver(State.EscapedMonsterCount, State.EscapeLimit);
                isBusy = false;
                yield break;
            }

            var attackResults = Session.ResolveTowerCombat(towerDefinitions, towerUpgradeDefinitions);
            boardView.RefreshTileEffects(State.Board);
            foreach (var attackResult in attackResults)
                yield return monsterPresenter.ApplyAttack(attackResult);
            var statusResults = Session.ResolveMonsterTurnEndEffects();
            boardView.RefreshTileEffects(State.Board);
            foreach (var statusResult in statusResults)
                yield return monsterPresenter.ApplyAttack(statusResult);
            var killedCount = 0;
            foreach (var attackResult in attackResults)
                if (attackResult.Killed) killedCount++;
            foreach (var statusResult in statusResults)
                if (statusResult.Killed) killedCount++;
            difficultyService.AddKills(State.Difficulty, killedCount);

            Session.CompleteRound();

            diceHud.BeginPlayerTurn();
            isBusy = false;
        }
    }
}
