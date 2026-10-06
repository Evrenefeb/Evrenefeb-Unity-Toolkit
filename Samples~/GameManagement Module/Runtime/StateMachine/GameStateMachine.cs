using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Toolkit.Core.Events;

namespace Toolkit.Core.State
{
    public interface IGameState
    {
        Task EnterAsync();
        Task ExitAsync();
        void Update(float deltaTime);
    }

    public interface IPayloadState<in TPayload> : IGameState
    {
        Task EnterAsync(TPayload payload);
    }

    public abstract class BaseGameState : IGameState
    {
        public virtual Task EnterAsync() => Task.CompletedTask;
        public virtual Task ExitAsync() => Task.CompletedTask;
        public virtual void Update(float deltaTime) { }
    }

    public abstract class BasePayloadGameState<TPayload> : BaseGameState, IPayloadState<TPayload>
    {
        // Hide parameterless EnterAsync, as this state expects a payload.
        public sealed override Task EnterAsync()
        {
            GameLog.Error($"State {GetType().Name} requires a payload of type {typeof(TPayload).Name} to enter!");
            return Task.CompletedTask;
        }

        public virtual Task EnterAsync(TPayload payload) => Task.CompletedTask;
    }

    public class GameStateMachine
    {
        private readonly Dictionary<Type, IGameState> _states = new Dictionary<Type, IGameState>();
        private readonly EventAggregator _events;

        public IGameState CurrentState { get; private set; }

        public GameStateMachine(EventAggregator events)
        {
            _events = events;
        }

        public void RegisterState<T>(T state) where T : IGameState
        {
            _states[typeof(T)] = state;
        }

        public async Task ChangeStateAsync<T>() where T : class, IGameState
        {
            var type = typeof(T);
            if (_states.TryGetValue(type, out var newState))
            {
                await ExecuteTransitionAsync(newState, state => state.EnterAsync());
            }
            else
            {
                GameLog.Error($"State {type.Name} is not registered in GameStateMachine!");
            }
        }

        public async Task ChangeStateAsync<T, TPayload>(TPayload payload) where T : class, IPayloadState<TPayload>
        {
            var type = typeof(T);
            if (_states.TryGetValue(type, out var newState) && newState is IPayloadState<TPayload> payloadState)
            {
                await ExecuteTransitionAsync(newState, _ => payloadState.EnterAsync(payload));
            }
            else
            {
                GameLog.Error($"State {type.Name} is not registered or does not accept payload of type {typeof(TPayload).Name}!");
            }
        }

        private async Task ExecuteTransitionAsync(IGameState newState, Func<IGameState, Task> enterStateFunc)
        {
            IGameState previousState = CurrentState;

            if (CurrentState != null)
            {
                await CurrentState.ExitAsync();
            }

            CurrentState = newState;
            GameLog.Info($"Game State changed to: {newState.GetType().Name}");

            await enterStateFunc(newState);

            _events?.Publish(new GameStateChangedEvent(previousState, CurrentState));
        }

        public void Tick(float deltaTime)
        {
            CurrentState?.Update(deltaTime);
        }
    }
}