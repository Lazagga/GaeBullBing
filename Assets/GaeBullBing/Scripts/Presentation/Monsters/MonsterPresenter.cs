using System.Collections;
using System.Collections.Generic;
using GaeBullBing.Core.Monsters;
using GaeBullBing.Core.Towers;
using GaeBullBing.Core.Data;
using GaeBullBing.Presentation.Board;
using UnityEngine;
using UnityEngine.UI;

namespace GaeBullBing.Presentation.Monsters
{
    public sealed class MonsterPresenter : MonoBehaviour
    {
        [SerializeField] private BoardTilemapView boardView;
        [SerializeField] private Sprite monsterSprite;
        [SerializeField] private Sprite foxSprite;
        [SerializeField] private MonsterDefinition[] monsterDefinitions;
        [SerializeField] private GameObject overflowPanel;
        [SerializeField] private Text overflowText;

        private readonly Dictionary<int, MonsterBoardView> views = new();
        private readonly Dictionary<int, MonsterState> states = new();
        private readonly Dictionary<int, OverflowIndicatorView> indicators = new();
        private int playerTileIndex;
        private bool hasPlayer;
        public MonsterDefinition[] MonsterDefinitions => monsterDefinitions;
        private static readonly Vector3[][] Slots = {
            new[] { Vector3.zero },
            new[] { new Vector3(-.18f,0), new Vector3(.18f,0) },
            new[] { new Vector3(0,.12f), new Vector3(-.2f,-.1f), new Vector3(.2f,-.1f) },
            new[] { new Vector3(0,.15f), new Vector3(-.22f,0), new Vector3(.22f,0), new Vector3(0,-.15f) }
        };

        [SerializeField] private PlayerBoardView playerView;
        public void SetPlayerTile(int tileIndex) { playerTileIndex = tileIndex; hasPlayer = true; ReflowAll(); }

        public void Spawn(MonsterState state)
        {
            var usePlayerAlignedArt = state.DefinitionId == "MON_002" && foxSprite != null;
            var monsterObject = new GameObject($"Monster {state.InstanceId} ({state.DefinitionId})");
            monsterObject.transform.SetParent(transform, false);
            var visual = new GameObject("Visual"); visual.transform.SetParent(monsterObject.transform, false);
            visual.transform.localScale = usePlayerAlignedArt ? Vector3.one : new Vector3(.3f, .68f, 1f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = usePlayerAlignedArt ? foxSprite : monsterSprite;
            var view = monsterObject.AddComponent<MonsterBoardView>();
            view.Initialize(
                state.InstanceId,
                boardView,
                state.CurrentTileIndex,
                usePlayerAlignedArt ? Vector3.zero : new Vector3(0f, .22f, 0f));
            view.TileChanged += OnMonsterTileChanged;
            view.UpdateHealth(state.CurrentHealth, state.MaxHealth);
            views.Add(state.InstanceId, view);
            states.Add(state.InstanceId, state);
        }

        public IEnumerator Move(MonsterMoveResult result)
        {
            if (!views.TryGetValue(result.InstanceId, out var view))
                yield break;

            yield return view.MoveSteps(result.StartTileIndex, result.Distance);
            if (result.ReachedBase)
            {
                view.TileChanged -= OnMonsterTileChanged;
                views.Remove(result.InstanceId);
                states.Remove(result.InstanceId);
                Destroy(view.gameObject);
            }
        }

        public void RefreshLayout() => ReflowAll();

        private void OnMonsterTileChanged(int previousTileIndex, int currentTileIndex)
        {
            ReflowTile(previousTileIndex);
            if (currentTileIndex != previousTileIndex)
                ReflowTile(currentTileIndex);
        }

        public IEnumerator ApplyAttack(TowerAttackResult result)
        {
            if (!views.TryGetValue(result.TargetInstanceId, out var view))
                yield break;

            yield return view.PlayHit();
            if (states.TryGetValue(result.TargetInstanceId, out var state)) view.UpdateHealth(state.CurrentHealth, state.MaxHealth);
            if (result.Killed)
            {
                var tile = view.CurrentTileIndex;
                view.TileChanged -= OnMonsterTileChanged;
                views.Remove(result.TargetInstanceId);
                states.Remove(result.TargetInstanceId);
                Destroy(view.gameObject);
                ReflowTile(tile);
            }
        }

        private void ReflowAll() { for (var i = 0; i < GaeBullBing.Core.Board.BoardState.DefaultTileCount; i++) ReflowTile(i); }

        private void ReflowTile(int tileIndex)
        {
            var occupants = new List<MonsterBoardView>();
            foreach (var pair in views) if (pair.Value.CurrentTileIndex == tileIndex) occupants.Add(pair.Value);
            occupants.Sort((a,b) => a.InstanceId.CompareTo(b.InstanceId));
            var playerHere = hasPlayer && playerTileIndex == tileIndex;
            var visibleMonsters = Mathf.Min(occupants.Count, playerHere ? 3 : 4);
            var totalVisible = visibleMonsters + (playerHere ? 1 : 0);
            if (totalVisible == 0) { UpdateOverflow(tileIndex, occupants, 0); return; }
            var slots = Slots[totalVisible - 1];
            if (playerHere && playerView != null) playerView.SetLayoutOffset(slots[0]);
            for (var i = 0; i < occupants.Count; i++)
            {
                var visible = i < visibleMonsters; occupants[i].SetVisible(visible);
                if (visible) occupants[i].SetLayoutOffset(slots[i + (playerHere ? 1 : 0)]);
            }
            UpdateOverflow(tileIndex, occupants, visibleMonsters);
            // Player owns slot zero; its layout hook is applied by GameController/player view.
        }

        private void UpdateOverflow(int tileIndex, List<MonsterBoardView> occupants, int visibleCount)
        {
            if (indicators.TryGetValue(tileIndex, out var old)) { indicators.Remove(tileIndex); Destroy(old.gameObject); }
            if (occupants.Count <= visibleCount) return;
            var hidden = occupants.GetRange(visibleCount, occupants.Count - visibleCount);
            var go = new GameObject($"Overflow {tileIndex}"); go.transform.SetParent(transform, false);
            go.transform.position = boardView.GetWorldPosition(tileIndex) + new Vector3(0, .72f);
            var indicator = go.AddComponent<OverflowIndicatorView>();
            indicator.Initialize(hidden.Count, show => ShowOverflow(show, hidden)); indicators[tileIndex] = indicator;
        }

        private void ShowOverflow(bool show, List<MonsterBoardView> hidden)
        {
            if (overflowPanel == null || overflowText == null) return;
            overflowPanel.SetActive(show); if (!show) return;
            var lines = new List<string>();
            foreach (var view in hidden)
                if (states.TryGetValue(view.InstanceId, out var state))
                    lines.Add($"{GetMonsterName(state.DefinitionId)}  {state.CurrentHealth}/{state.MaxHealth}");
            overflowText.text = string.Join("\n", lines);
        }

        private string GetMonsterName(string id)
        {
            foreach (var definition in monsterDefinitions) if (definition != null && definition.Id == id) return definition.DisplayName;
            return id;
        }
    }
}
