

namespace Evrenefeb.Toolkit.GameManagement {
    /// <summary>
    /// Hook for games to add states and services without editing <see cref="GameManager"/>.
    /// Runs after built-in services are registered and before the Running state.
    /// Implement on a MonoBehaviour attached to the GameManager, or assign it in the inspector.
    /// </summary>
    public interface IGameInstaller
    {
        void RegisterStates(GameStateMachine states);
        void RegisterServices(ServiceRegistry services, EventAggregator events, GameManagerConfig config);
    }
}
