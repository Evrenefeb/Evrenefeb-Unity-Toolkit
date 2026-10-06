using Toolkit.Core.Services;
using Toolkit.Core.State;

namespace Toolkit.Core.Events
{
    public readonly struct GameInitializedEvent : IGameEvent { }

    public readonly struct GamePausedEvent : IGameEvent
    {
        public readonly bool IsPaused;
        public readonly PauseReason Reason;

        public GamePausedEvent(bool isPaused, PauseReason reason)
        {
            IsPaused = isPaused;
            Reason = reason;
        }
    }

    public readonly struct GameStateChangedEvent : IGameEvent
    {
        public readonly IGameState PreviousState;
        public readonly IGameState NewState;

        public GameStateChangedEvent(IGameState previousState, IGameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }
}
