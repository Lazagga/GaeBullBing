using System;
using System.Collections.Generic;
using GaeBullBing.Core.Game;

namespace GaeBullBing.Core.Turns
{
    public interface ITurnPhase
    {
        TurnPhase Phase { get; }
        void Enter(TurnContext context);
        void Exit(TurnContext context);
    }

    public sealed class TurnContext
    {
        public TurnContext(GameState state) => State = state;
        public GameState State { get; }
    }

    public sealed class TurnStateMachine
    {
        private readonly Dictionary<TurnPhase, ITurnPhase> phases = new();
        private readonly TurnContext context;
        private ITurnPhase current;

        public TurnStateMachine(TurnContext context) => this.context = context;

        public void Register(ITurnPhase phase) => phases[phase.Phase] = phase;

        public void ChangeTo(TurnPhase next)
        {
            if (!phases.TryGetValue(next, out var nextPhase))
                throw new InvalidOperationException($"Turn phase is not registered: {next}");

            current?.Exit(context);
            current = nextPhase;
            context.State.CurrentPhase = next;
            current.Enter(context);
        }
    }
}
