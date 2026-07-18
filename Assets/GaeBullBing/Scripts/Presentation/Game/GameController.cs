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
using UnityEngine.SceneManagement;
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
        [SerializeField] private TileInfoPanelView tileInfoPanel;
        [SerializeField] private GameFlowView gameFlowView;
        [SerializeField, Min(0f)] private float diceRevealDelay = 0.35f;
        [SerializeField, Range(0f, 1f)] private float cornerDamageRateBonus = .1f;

        private bool isBusy;
        private MonsterDatabase monsterDatabase;
        private DifficultyService difficultyService;
        private int killsPerDifficultyLevel = 10;
        private float healthMultiplierPerDifficultyLevel = 1.15f;
        private float defensePerDifficultyLevel;
        private Dice3DPresenter dice3DPresenter;
        private StonePresenter stonePresenter;
        private TowerAttackEffectPresenter attackEffectPresenter;
        private string nextMonsterOverrideId;
        private BoardTileSelectionView tileSelectionView;
        private bool pendingDiceTuning;
        private bool diceTuningComplete;
        private Coroutine tileInfoCameraRoutine;
        private bool tileInfoOpen;
        private int inspectedTileIndex = -1;
        private bool tileInfoReturnsToPlayerFocus;
        private const int MaxTowerElementDamageBonus = 30;
        private static bool startImmediatelyAfterReload;
        private static bool fadeTitleAfterReload;
        private bool finishRoutineStarted;

        public GameState State { get; private set; }
        public GameSession Session { get; private set; }
        public int TotalKills => State?.Difficulty?.KillCount ?? 0;
        public int RemainingKills => difficultyService == null ? 0 : difficultyService.GetRemainingKills(State.Difficulty);
        public bool IsFinalPattern => difficultyService != null && difficultyService.IsFinalPattern(State.Difficulty);
        public bool IsBossLevel => difficultyService != null && difficultyService.IsBossLevel(State.Difficulty);
        public bool AcceptsGameplayInput { get; private set; }

        public bool FinishGameFromConsole(bool victory, out string message)
        {
            if (finishRoutineStarted)
            {
                message = "이미 게임 종료 처리가 진행 중입니다.";
                return false;
            }

            message = victory ? "즉시 승리 처리합니다." : "즉시 패배 처리합니다.";
            if (victory) FinishVictory();
            else FinishDefeat();
            return true;
        }

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
            diceHud.RefreshDifficulty();
            monsterPresenter.RefreshLayout();
            if (State.IsVictory) FinishVictory();
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
            var bossAppearanceLevel = FindBossDefinition()?.AppearanceWave ?? DifficultyService.FinalBossLevel;
            difficultyService = new DifficultyService(
                difficultyPatterns,
                killsPerDifficultyLevel,
                healthMultiplierPerDifficultyLevel,
                defensePerDifficultyLevel,
                bossAppearanceLevel);
            difficultyService.Reset(State.Difficulty);
            dice3DPresenter = GetComponent<Dice3DPresenter>();
            if (dice3DPresenter == null)
                dice3DPresenter = gameObject.AddComponent<Dice3DPresenter>();
            dice3DPresenter.Initialize(boardView);
            attackEffectPresenter = GetComponent<TowerAttackEffectPresenter>();
            if (attackEffectPresenter == null)
                attackEffectPresenter = gameObject.AddComponent<TowerAttackEffectPresenter>();
            attackEffectPresenter.Initialize(boardView);
            stonePresenter = GetComponent<StonePresenter>();
            if (stonePresenter == null)
                stonePresenter = gameObject.AddComponent<StonePresenter>();
            stonePresenter.Initialize(boardView, attackEffectPresenter.PhysicsAttackSprite);
            tileSelectionView = boardView.GetComponent<BoardTileSelectionView>();
            if (tileSelectionView == null) tileSelectionView = boardView.gameObject.AddComponent<BoardTileSelectionView>();
            tileSelectionView.Initialize(boardView, () => AcceptsGameplayInput);
            tileSelectionView.EnableInspection(ShowTileInformation, CloseTileInformation);
            if (tileInfoPanel == null)
                tileInfoPanel = FindFirstObjectByType<TileInfoPanelView>(FindObjectsInactive.Include);
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
            killsPerDifficultyLevel = killsPerLevel;
            healthMultiplierPerDifficultyLevel = healthMultiplierPerLevel;
            defensePerDifficultyLevel = Mathf.Max(0f, source.wavedata[0].defense_per_wave);

            System.Array.Sort(source.wave_patterns, (left, right) => left.level.CompareTo(right.level));
            var knownMonsterIds = new HashSet<string>();
            foreach (var monster in monsterDefinitions)
                if (monster != null) knownMonsterIds.Add(monster.Id);

            var converted = new List<DifficultyPatternData>(source.wave_patterns.Length);
            var cumulativeRequiredKills = 0;
            var cumulativeHealthMultiplier = 1f;
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
                    HealthMultiplier = cumulativeHealthMultiplier,
                    MonsterIds = pattern.spawn_pattern
                });
                cumulativeRequiredKills += killsPerLevel;
                cumulativeHealthMultiplier *= healthMultiplierPerLevel;
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
            public float defense_per_wave;
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
            playerView.TileMoveStarted += monsterPresenter.SetPlayerTile;
            playerView.TileEntered += OnPlayerTileEntered;
            monsterPresenter.SetPlayerTile(State.Player.CurrentTileIndex);
            stonePresenter.Refresh(State);
            diceHud.Bind(this);
            if (gameFlowView == null)
                gameFlowView = FindFirstObjectByType<GameFlowView>(FindObjectsInactive.Include);
            gameFlowView?.Bind(this);
            if (startImmediatelyAfterReload)
            {
                startImmediatelyAfterReload = false;
                isBusy = true;
                diceHud.SetBusy();
                gameFlowView?.BeginRestart();
            }
            else
            {
                isBusy = true;
                diceHud.SetBusy();
                var fadePortraits = fadeTitleAfterReload;
                fadeTitleAfterReload = false;
                gameFlowView?.ShowTitle(fadePortraits);
            }
        }

        public void StartGameFromTitle()
        {
            gameFlowView?.HideAll();
            isBusy = false;
            AcceptsGameplayInput = true;
            diceHud.BeginPlayerTurn();
        }

        public void ReturnToTitle()
        {
            AcceptsGameplayInput = false;
            startImmediatelyAfterReload = false;
            fadeTitleAfterReload = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void RestartGame()
        {
            AcceptsGameplayInput = false;
            startImmediatelyAfterReload = true;
            fadeTitleAfterReload = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnPlayerTileEntered(int tileIndex)
        {
            monsterPresenter.SetPlayerTile(tileIndex);
        }

        public void RollDiceAndMovePlayer()
        {
            if (!AcceptsGameplayInput || isBusy) return;
            if (tileInfoOpen) StartCoroutine(CloseTileInformationThenRoll());
            else StartCoroutine(RollAndMoveRoutine());
        }

        private IEnumerator CloseTileInformationThenRoll()
        {
            isBusy = true;
            HideTileInformation();
            yield return cameraController.ReturnToOverview();
            yield return RollAndMoveRoutine();
        }

        private void ShowTileInformation(int tileIndex)
        {
            var returnToPlayerFocus = tileInfoOpen
                ? tileInfoReturnsToPlayerFocus
                : State.CurrentPhase == TurnPhase.TileAction || State.CurrentPhase == TurnPhase.TowerSelection;
            ShowTileInformation(tileIndex, true, returnToPlayerFocus);
        }

        private void ShowTileInformation(int tileIndex, bool focusCamera, bool returnToPlayerFocus = false)
        {
            if (tileInfoPanel == null || tileIndex < 0 || tileIndex >= State.Board.TileCount)
                return;

            tileInfoOpen = true;
            inspectedTileIndex = tileIndex;
            tileInfoReturnsToPlayerFocus = returnToPlayerFocus;
            tileInfoPanel.Show(
                $"타일 {tileIndex}",
                BuildTileDescription(tileIndex),
                BuildMonsterDescription(tileIndex));

            if (!focusCamera) return;
            if (tileInfoCameraRoutine != null) StopCoroutine(tileInfoCameraRoutine);
            tileInfoCameraRoutine = StartCoroutine(FocusTileInformation(tileIndex));
        }

        private IEnumerator FocusTileInformation(int tileIndex)
        {
            yield return cameraController.FocusOnTile(tileIndex);
            tileInfoCameraRoutine = null;
        }

        private void CloseTileInformation()
        {
            if (!tileInfoOpen) return;
            var returnToPlayerFocus = tileInfoReturnsToPlayerFocus;
            HideTileInformation();
            tileInfoCameraRoutine = StartCoroutine(ReturnFromTileInformation(returnToPlayerFocus));
        }

        private void HideTileInformation()
        {
            tileInfoOpen = false;
            inspectedTileIndex = -1;
            tileInfoReturnsToPlayerFocus = false;
            tileInfoPanel?.Hide();
            if (tileInfoCameraRoutine != null)
            {
                StopCoroutine(tileInfoCameraRoutine);
                tileInfoCameraRoutine = null;
            }
        }

        private void RefreshOpenTileInformation()
        {
            if (!tileInfoOpen || inspectedTileIndex < 0 || inspectedTileIndex >= State.Board.TileCount)
                return;
            tileInfoPanel?.Show(
                $"타일 {inspectedTileIndex}",
                BuildTileDescription(inspectedTileIndex),
                BuildMonsterDescription(inspectedTileIndex));
        }

        private IEnumerator ReturnFromTileInformation(bool returnToPlayerFocus)
        {
            if (returnToPlayerFocus)
                yield return cameraController.FocusOn(playerView);
            else
                yield return cameraController.ReturnToOverview();
            tileInfoCameraRoutine = null;
        }

        private string BuildTileDescription(int tileIndex)
        {
            var tile = State.Board.Tiles[tileIndex];
            var featherDescription = tile.HasBossFeather
                ? "[타일 상태: 깃털]\n이 타일의 타워는 까마귀가 깃털을 회수할 때까지 공격할 수 없습니다.\n\n"
                : string.Empty;

            if (tileIndex == 0 || tileIndex == 18)
                return featherDescription + "[모서리 능력: 이동]\n원하는 타일을 선택하고 해당 타일까지 시계 방향으로 이동합니다.\n이동 중 출발지를 통과하면 주사위 강화가 발생합니다.";
            if (tileIndex == 9 || tileIndex == 27)
                return featherDescription + $"[모서리 능력: 전체 강화]\n불·얼음·물리·전기 중 하나를 선택해 해당 속성 타워의 공격력을 영구적으로 {cornerDamageRateBonus * 100f:0}% 증가시킵니다.\n이후 건설되는 타워에도 적용됩니다.";

            if (!tile.HasTower)
            {
                var buildable = FindTowerDefinition(tile.BuildTowerDefinitionId);
                return featherDescription + (buildable == null
                    ? "[타워]\n설치된 타워가 없습니다."
                    : $"[타워]\n설치된 타워가 없습니다.\n건설 가능: {buildable.DisplayName} ({GetElementName(buildable.Element)})");
            }

            var definition = FindTowerDefinition(tile.Tower.DefinitionId);
            if (definition == null) return featherDescription + $"[타워]\n정의 없음: {tile.Tower.DefinitionId}";
            var stats = CalculateDisplayStats(definition, tile);
            var builder = new StringBuilder();
            builder.AppendLine($"[타워] {definition.DisplayName}  T{tile.Tower.UpgradeTier}");
            builder.AppendLine($"속성: {GetElementName(definition.Element)}");
            builder.AppendLine($"공격력 {stats.damage}  |  사거리 ±{stats.range}타일");
            builder.AppendLine($"대상 {stats.targets}  |  공격 횟수 {stats.attacks}");
            builder.AppendLine();
            builder.AppendLine("[적용된 업그레이드]");
            if (tile.Tower.AppliedUpgradeIds.Count == 0) builder.Append("없음");
            else foreach (var id in tile.Tower.AppliedUpgradeIds)
            {
                var upgrade = FindUpgradeDefinition(id);
                builder.AppendLine(upgrade == null ? $"• {id}" : $"• T{upgrade.Tier} {upgrade.DisplayName}\n  {upgrade.Description}");
            }
            return featherDescription + builder.ToString().TrimEnd();
        }

        private string BuildMonsterDescription(int tileIndex)
        {
            var builder = new StringBuilder("[몬스터]\n");
            var count = 0;
            foreach (var monster in State.Monsters)
            {
                if (monster.IsDead || monster.CurrentTileIndex != tileIndex) continue;
                count++;
                var definition = FindMonsterDefinition(monster.DefinitionId);
                builder.Append($"{count}. {(definition == null ? monster.DefinitionId : definition.DisplayName)}  HP {Mathf.Max(0, Mathf.FloorToInt(monster.CurrentHealth))}/{Mathf.FloorToInt(monster.MaxHealth)}");
                var statuses = BuildMonsterStatuses(monster);
                if (!string.IsNullOrEmpty(statuses)) builder.Append($"\n   {statuses}");
                builder.AppendLine();
            }
            if (count == 0) builder.Append("없음");
            return builder.ToString().TrimEnd();
        }

        private static string BuildMonsterStatuses(MonsterState monster)
        {
            var values = new List<string>();
            if (monster.BurnStacks > 0) values.Add($"화상 {monster.BurnStacks}중첩");
            if (monster.Shocked) values.Add("감전");
            if (monster.FrozenMovesRemaining > 0) values.Add("빙결");
            if (monster.StunnedMovesRemaining > 0) values.Add("이동 불가");
            if (monster.KnockbackConsumed) values.Add("넉백 면역");
            return string.Join(", ", values);
        }

        private (int damage, int range, int targets, int attacks) CalculateDisplayStats(TowerDefinition definition, TileState tile)
        {
            var tower = tile.Tower;
            var damageAdd = 0f; var damageMultiply = 1f;
            var rangeAdd = 0f; var rangeMultiply = 1f;
            var targetAdd = 0f; var targetMultiply = 1f;
            var attackAdd = 0f; var attackMultiply = 1f;
            float? damageSet = null, rangeSet = null, targetSet = null, attackSet = null;
            foreach (var id in tower.AppliedUpgradeIds)
            {
                var upgrade = FindUpgradeDefinition(id); if (upgrade == null) continue;
                foreach (var modifier in upgrade.StatModifiers)
                {
                    if (upgrade.Id == "UPG_ICE_T3_02" && modifier.Stat.Equals("damage", System.StringComparison.OrdinalIgnoreCase) && modifier.Operation.Equals("Multiply", System.StringComparison.OrdinalIgnoreCase)) continue;
                    var multiply = modifier.Operation.Equals("Multiply", System.StringComparison.OrdinalIgnoreCase);
                    var set = modifier.Operation.Equals("Set", System.StringComparison.OrdinalIgnoreCase);
                    switch (modifier.Stat.ToLowerInvariant())
                    {
                        case "damage": if (set) damageSet = modifier.Value; else if (multiply) damageMultiply *= modifier.Value; else damageAdd += modifier.Value; break;
                        case "range": if (set) rangeSet = modifier.Value; else if (multiply) rangeMultiply *= modifier.Value; else rangeAdd += modifier.Value; break;
                        case "target_count": if (set) targetSet = modifier.Value; else if (multiply) targetMultiply *= modifier.Value; else targetAdd += modifier.Value; break;
                        case "attack_count": if (set) attackSet = modifier.Value; else if (multiply) attackMultiply *= modifier.Value; else attackAdd += modifier.Value; break;
                    }
                }
            }
            return (
                Mathf.Max(0, Mathf.RoundToInt(damageSet ??
                    definition.Damage * damageMultiply *
                    (1f + State.PermanentAllTowerDamageRateBonus +
                     State.GetPermanentTowerDamageRateBonus(definition.Element) +
                     State.GetPermanentLineTowerDamageRateBonus(MonsterService.GetLine(tile.Index)) +
                     GetLineAuraDamageRateBonus(tile)) +
                    damageAdd + State.GetPermanentTowerDamageFlatBonus(definition.Element))),
                Mathf.Max(0, Mathf.RoundToInt(rangeSet ?? (definition.Range + rangeAdd) * rangeMultiply)),
                Mathf.Max(1, Mathf.RoundToInt(targetSet ?? (definition.TargetCount + targetAdd) * targetMultiply)),
                Mathf.Max(1, Mathf.RoundToInt(attackSet ?? (definition.AttackCount + attackAdd) * attackMultiply)));
        }

        private float GetLineAuraDamageRateBonus(TileState targetTile)
        {
            var line = MonsterService.GetLine(targetTile.Index);
            var rate = 0f;
            foreach (var tile in State.Board.Tiles)
                if (tile.HasTower && tile.Tower.InstanceId != targetTile.Tower.InstanceId &&
                    MonsterService.GetLine(tile.Index) == line &&
                    tile.Tower.AppliedUpgradeIds.Contains("UPG_PHYSICS_T2_03"))
                    rate += .2f;
            return rate;
        }

        private TowerUpgradeDefinition FindUpgradeDefinition(string id)
        {
            foreach (var definition in towerUpgradeDefinitions) if (definition != null && definition.Id == id) return definition;
            return null;
        }

        private MonsterDefinition FindMonsterDefinition(string id)
        {
            foreach (var definition in monsterDefinitions) if (definition != null && definition.Id == id) return definition;
            return null;
        }

        private static string GetElementName(TowerElement element) => element switch
        {
            TowerElement.Fire => "불",
            TowerElement.Ice => "얼음",
            TowerElement.Physics => "물리",
            TowerElement.Electric => "전기",
            _ => "없음"
        };

        private IEnumerator RollAndMoveRoutine()
        {
            isBusy = true;
            diceHud.SetRolling(true);
            yield return new WaitForSeconds(diceRevealDelay);

            var startTileIndex = State.Player.CurrentTileIndex;
            var distance = Session.RollDiceAndMovePlayer();
            pendingDiceTuning |= startTileIndex + distance >= State.Board.TileCount;
            diceHud.SetResults(State.LastDiceResults[0], State.LastDiceResults[1]);
            yield return dice3DPresenter.Roll(State.Dice, State.LastDiceResults[0], State.LastDiceResults[1]);
            yield return cameraController.FocusOn(playerView);
            yield return playerView.MoveSteps(startTileIndex, distance);
            BeginCurrentTileAction();
        }

        private void BeginCurrentTileAction()
        {
            State.CurrentPhase = TurnPhase.TileAction;
            diceHud.SetBusy();
            var tile = State.Board.Tiles[State.Player.CurrentTileIndex];
            ApplyArrivalTowerEffects(tile);
            ShowTileInformation(tile.Index, false, tile.HasTower || tile.CanBuildTower);
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

        private void ApplyArrivalTowerEffects(TileState tile)
        {
            if (tile.HasTower && tile.Tower.AppliedUpgradeIds.Contains("UPG_PHYSICS_T2_04"))
                Session.AddPermanentLineTowerDamageRateBonus(MonsterService.GetLine(tile.Index), .1f);
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
            tileSelectionView.BeginSelection(
                SelectTeleportDestination,
                tileIndex => ShowTileInformation(tileIndex, false),
                HideTileInformation);
        }

        public void SelectCornerElement(TowerElement element)
        {
            if (State.CurrentPhase != TurnPhase.CornerSelection || element == TowerElement.None) return;
            Session.AddPermanentTowerDamageRateBonus(element, cornerDamageRateBonus);
            HideTileInformation();
            cornerActionMenu.Hide(); StartCoroutine(CompleteTileActionRoutine());
        }

        public void SelectTeleportDestination(int tileIndex)
        {
            if (State.CurrentPhase != TurnPhase.CornerSelection || tileIndex < 0 || tileIndex >= State.Board.TileCount) return;
            HideTileInformation();
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
                if (upgrades.Count == 0)
                {
                    var definition = FindTowerDefinition(tile.Tower.DefinitionId);
                    if (definition != null)
                        Session.AddPermanentTowerDamageFlatBonus(definition.Element, MaxTowerElementDamageBonus);
                    radialMenu.Hide();
                    HideTileInformation();
                    StartCoroutine(CompleteTileActionRoutine());
                    return;
                }
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
            HideTileInformation();
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
            radialMenu.Hide();
            HideTileInformation();
            StartCoroutine(CompleteTileActionRoutine());
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
                diceTuningView.Show(State.Dice, ApplyDiceTuning, ApplyAllTowerDamageBoost);
                yield return new WaitUntil(() => diceTuningComplete);
            }
            yield return ResolveEnemyTurnRoutine();
        }

        private bool ApplyDiceTuning(int diceIndex, int faceValue, int delta)
        {
            if (!Session.TryShiftDiceWeight(diceIndex, faceValue, delta))
                return false;
            diceHud.RefreshDiceFaces();
            diceTuningComplete = true;
            return true;
        }

        private void ApplyAllTowerDamageBoost()
        {
            Session.AddPermanentAllTowerDamageRateBonus(.05f);
            diceTuningComplete = true;
        }

        private IEnumerator ResolveEnemyTurnRoutine()
        {
            isBusy = true;
            diceHud.SetBusy();

            if (difficultyService.IsBossLevel(State.Difficulty))
            {
                nextMonsterOverrideId = null;
                if (!State.BossSpawned)
                {
                    var bossDefinition = FindBossDefinition();
                    if (bossDefinition == null)
                    {
                        Debug.LogError("BOSS_001 MonsterDefinition을 찾을 수 없습니다.", this);
                        State.CurrentPhase = TurnPhase.Defeat;
                    }
                    else
                    {
                        var boss = Session.SpawnMonster(bossDefinition, 1f);
                        monsterPresenter.Spawn(boss);
                    }
                }
            }
            else
            {
                var scheduledMonsterId = difficultyService.GetNextMonsterId(State.Difficulty);
                var monsterId = string.IsNullOrEmpty(nextMonsterOverrideId)
                    ? scheduledMonsterId
                    : nextMonsterOverrideId;
                nextMonsterOverrideId = null;
                var definition = monsterDatabase.Get(monsterId);
                var spawnedMonster = Session.SpawnMonster(definition,
                    difficultyService.GetHealthMultiplier(State.Difficulty));
                monsterPresenter.Spawn(spawnedMonster);
            }

            if (State.IsGameOver)
            {
                FinishDefeat();
                yield break;
            }

            var moveResults = Session.MoveMonsters(towerDefinitions, towerUpgradeDefinitions);
            diceHud.RefreshPlayerHealth();
            foreach (var result in moveResults)
            {
                if (result.IsBoss &&
                    monsterPresenter.TryGetViewTransform(result.InstanceId, out var bossTransform))
                {
                    yield return cameraController.FocusOn(bossTransform);
                    yield return monsterPresenter.Move(result);
                    yield return cameraController.ReturnToOverview();
                }
                else
                    yield return monsterPresenter.Move(result);
            }
            monsterPresenter.RefreshLayout();
            boardView.RefreshTileEffects(State.Board);

            if (State.IsGameOver)
            {
                FinishDefeat();
                yield break;
            }

            var attackResults = Session.ResolveTowerCombat(towerDefinitions, towerUpgradeDefinitions);
            stonePresenter.Refresh(State);
            boardView.RefreshTileEffects(State.Board);
            var illuminatedLineTowerIds = new HashSet<int>();
            for (var attackIndex = 0; attackIndex < attackResults.Count; attackIndex++)
            {
                var attackResult = attackResults[attackIndex];
                if (attackResult.VisualKind != TowerAttackVisualKind.AreaTile)
                {
                    yield return PlayAttackResult(attackResult, illuminatedLineTowerIds);
                    continue;
                }

                var areaTowerId = attackResult.TowerInstanceId;
                var areaTiles = new List<int>();
                while (attackIndex < attackResults.Count &&
                       attackResults[attackIndex].VisualKind == TowerAttackVisualKind.AreaTile &&
                       attackResults[attackIndex].TowerInstanceId == areaTowerId)
                {
                    areaTiles.Add(attackResults[attackIndex].TargetTileIndex);
                    attackIndex++;
                }
                attackIndex--;
                if (attackEffectPresenter != null)
                    yield return attackEffectPresenter.PlayAreaTiles(State, areaTowerId, areaTiles);
            }
            if (State.IsVictory)
            {
                boardView.RefreshTileEffects(State.Board);
                FinishVictory();
                yield break;
            }
            var statusResults = Session.ResolveMonsterTurnEndEffects();
            var consumedStoneResults = new HashSet<int>();
            for (var resultIndex = 0; resultIndex < statusResults.Count; resultIndex++)
            {
                if (statusResults[resultIndex].VisualKind == TowerAttackVisualKind.RollingStone) continue;
                consumedStoneResults.Add(resultIndex);
                yield return PlayAttackResult(statusResults[resultIndex], illuminatedLineTowerIds);
            }
            yield return stonePresenter.PlayResolvedMovement(
                State,
                statusResults,
                consumedStoneResults,
                result => PlayAttackResult(result, illuminatedLineTowerIds));
            stonePresenter.Refresh(State);
            boardView.RefreshTileEffects(State.Board);
            for (var resultIndex = 0; resultIndex < statusResults.Count; resultIndex++)
                if (!consumedStoneResults.Contains(resultIndex))
                    yield return PlayAttackResult(statusResults[resultIndex], illuminatedLineTowerIds);
            var killedCount = 0;
            foreach (var attackResult in attackResults)
                if (attackResult.Killed) killedCount++;
            foreach (var statusResult in statusResults)
                if (statusResult.Killed) killedCount++;
            difficultyService.AddKills(State.Difficulty, killedCount);
            diceHud.RefreshDifficulty();

            if (State.IsVictory)
            {
                boardView.RefreshTileEffects(State.Board);
                FinishVictory();
                yield break;
            }

            Session.CompleteRound();

            RefreshOpenTileInformation();
            diceHud.BeginPlayerTurn();
            isBusy = false;
        }

        private MonsterDefinition FindBossDefinition()
        {
            foreach (var definition in monsterDefinitions)
                if (definition != null &&
                    (definition.Tier == MonsterTier.Boss || definition.Id == "BOSS_001"))
                    return definition;
            return null;
        }

        private void FinishVictory()
        {
            if (finishRoutineStarted) return;
            finishRoutineStarted = true;
            AcceptsGameplayInput = false;
            isBusy = true;
            diceHud.SetBusy();
            radialMenu.Hide();
            cornerActionMenu?.Hide();
            HideTileInformation();
            StartCoroutine(FinishVictoryRoutine());
        }

        private void FinishDefeat()
        {
            if (finishRoutineStarted) return;
            finishRoutineStarted = true;
            AcceptsGameplayInput = false;
            isBusy = true;
            diceHud.SetBusy();
            radialMenu.Hide();
            cornerActionMenu?.Hide();
            diceTuningView?.Hide();
            HideTileInformation();
            StartCoroutine(FinishDefeatRoutine());
        }

        private IEnumerator FinishVictoryRoutine()
        {
            if (gameFlowView != null)
                yield return gameFlowView.PlayOutro();
            diceHud.ShowGameClear();
            gameFlowView?.ShowVictory();
            isBusy = false;
        }

        private IEnumerator FinishDefeatRoutine()
        {
            if (gameFlowView != null)
                yield return gameFlowView.PlayOutro();
            diceHud.ShowGameOver(State.EscapedMonsterCount, State.EscapeLimit);
            gameFlowView?.ShowDefeat();
            isBusy = false;
        }

        private IEnumerator PlayAttackResult(
            TowerAttackResult result,
            ISet<int> illuminatedLineTowerIds)
        {
            if (attackEffectPresenter != null)
                yield return attackEffectPresenter.Play(State, result, illuminatedLineTowerIds);
            yield return monsterPresenter.ApplyAttack(result);
        }
    }
}
