using GaeBullBing.Core.Game;
using GaeBullBing.Core.Turns;
using UnityEngine;

namespace GaeBullBing.Presentation.Game
{
    public sealed class GameController : MonoBehaviour
    {
        public GameState State { get; private set; }
        public TurnStateMachine Turns { get; private set; }

        private void Awake()
        {
            State = new GameState();
            Turns = new TurnStateMachine(new TurnContext(State));
        }
    }
}
