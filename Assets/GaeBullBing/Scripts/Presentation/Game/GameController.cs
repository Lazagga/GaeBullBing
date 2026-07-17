using System.Collections;
using GaeBullBing.Core;
using GaeBullBing.Core.Board;
using GaeBullBing.Core.Dice;
using GaeBullBing.Core.Game;
using GaeBullBing.Core.Data;
using GaeBullBing.Core.Monsters;
using GaeBullBing.Presentation.Board;
using GaeBullBing.Presentation.Monsters;
using GaeBullBing.Presentation.UI;
using UnityEngine;

namespace GaeBullBing.Presentation.Game
{
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] private BoardTilemapView boardView;
        [SerializeField] private PlayerBoardView playerView;
        [SerializeField] private DiceHudView diceHud;
        [SerializeField] private MonsterPresenter monsterPresenter;
        [SerializeField] private MonsterDefinition defaultMonster;
        [SerializeField, Min(0f)] private float diceRevealDelay = 0.35f;

        private bool isBusy;

        public GameState State { get; private set; }
        public GameSession Session { get; private set; }

        private void Awake()
        {
            State = new GameState();
            Session = new GameSession(
                State,
                new BoardService(),
                new PlayerMovementService(),
                new WeightedDiceRoller(new SystemDiceRandom()),
                new MonsterService());
            Session.StartNewGame();
        }

        private void Start()
        {
            playerView.Initialize(boardView, State.Player.CurrentTileIndex);
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
            diceHud.SetResults(State.LastDiceResults[0], State.LastDiceResults[1]);
            yield return playerView.MoveSteps(startTileIndex, distance);

            diceHud.SetAwaitingEndTurn();
            isBusy = false;
        }

        public void EndTurn()
        {
            if (!isBusy && State.CurrentPhase == TurnPhase.TileResolve)
                StartCoroutine(EndTurnRoutine());
        }

        private IEnumerator EndTurnRoutine()
        {
            isBusy = true;
            diceHud.SetBusy();

            var spawnedMonster = Session.SpawnMonster(defaultMonster);
            monsterPresenter.Spawn(spawnedMonster);
            var moveResults = Session.MoveMonsters();
            foreach (var result in moveResults)
                yield return monsterPresenter.Move(result);

            State.CurrentPhase = TurnPhase.TowerCombat;
            Session.CompleteRound();

            diceHud.BeginPlayerTurn();
            isBusy = false;
        }
    }
}
