using System.Collections;
using System.Collections.Generic;
using GaeBullBing.Core.Monsters;
using GaeBullBing.Presentation.Board;
using UnityEngine;

namespace GaeBullBing.Presentation.Monsters
{
    public sealed class MonsterPresenter : MonoBehaviour
    {
        [SerializeField] private BoardTilemapView boardView;
        [SerializeField] private Sprite monsterSprite;

        private readonly Dictionary<int, MonsterBoardView> views = new();

        public void Spawn(MonsterState state)
        {
            var monsterObject = new GameObject($"Monster {state.InstanceId} ({state.DefinitionId})");
            monsterObject.transform.SetParent(transform, false);
            var renderer = monsterObject.AddComponent<SpriteRenderer>();
            renderer.sprite = monsterSprite;
            renderer.sortingOrder = 15;
            var view = monsterObject.AddComponent<MonsterBoardView>();
            view.Initialize(state.InstanceId, boardView, state.CurrentTileIndex);
            views.Add(state.InstanceId, view);
        }

        public IEnumerator Move(MonsterMoveResult result)
        {
            if (!views.TryGetValue(result.InstanceId, out var view))
                yield break;

            yield return view.MoveSteps(result.StartTileIndex, result.Distance);
            if (result.ReachedBase)
            {
                views.Remove(result.InstanceId);
                Destroy(view.gameObject);
            }
        }
    }
}
