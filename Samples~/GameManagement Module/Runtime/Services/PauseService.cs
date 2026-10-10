using System;
using UnityEngine;

namespace Evrenefeb.Toolkit.GameManagement {
    [Flags]
    public enum PauseReason
    {
        None = 0,
        UserMenu = 1 << 0,
        GameplayFocus = 1 << 1,
        SystemModal = 1 << 2
    }

    public interface IPauseService : IGameService
    {
        bool IsPaused { get; }
        PauseReason CurrentPauseReason { get; }
        void Pause(PauseReason reason);
        void Resume(PauseReason reason);
    }

    public class PauseService : IPauseService
    {
        private readonly EventAggregator _events;
        public bool IsPaused => CurrentPauseReason != PauseReason.None;
        public PauseReason CurrentPauseReason { get; private set; } = PauseReason.None;

        public PauseService(EventAggregator events)
        {
            _events = events;
        }

        public void Pause(PauseReason reason)
        {
            CurrentPauseReason |= reason;
            ApplyPauseState();
        }

        public void Resume(PauseReason reason)
        {
            CurrentPauseReason &= ~reason;
            ApplyPauseState();
        }

        private void ApplyPauseState()
        {
            Time.timeScale = IsPaused ? 0f : 1f;
            GameLog.Info($"Pause state updated. IsPaused: {IsPaused}, Reasons: {CurrentPauseReason}");
            _events.Publish(new GamePausedEvent(IsPaused, CurrentPauseReason));
        }
    }
}
