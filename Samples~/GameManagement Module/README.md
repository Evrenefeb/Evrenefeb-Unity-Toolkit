# Unity Toolkit - Modular GameManager Architecture

Comprehensive, modular, scalable, and plug-and-play GameManager package for Unity.

## Folder Structure
- `Runtime/Core/`: Base Interfaces, Logging, Service Registry, Configuration, GameManager Orchestrator
- `Runtime/Events/`: Event Aggregator, `IGameEvent` payloads, subscription tokens
- `Runtime/Services/`: Built-in Services (SceneService, PauseService)
- `Runtime/StateMachine/`: Game State Machine & Core States
- `Runtime/Bootstrap/`: Automatic Runtime Initialization
- `Editor/Windows/`: Unity Editor Dashboard & Tooling

## Events
Payloads are `readonly struct`s implementing `IGameEvent`. Subscribe with an `IDisposable` token and/or a Unity owner so handlers cannot leak on the persistent GameManager:

```csharp
_sub = GameManager.Events.Subscribe<GamePausedEvent>(OnPaused, this);
GameManager.Events.Publish(new GamePausedEvent(true, PauseReason.UserMenu));
_sub.Dispose(); // optional if the owner will be destroyed
```

`SubscribeOnce`, handler `priority` (higher runs first), `Clear` / diagnostics, and per-type subscriber counts are available. The GameManager Dashboard lists live subscriber counts in Play Mode.

## Extending GameManager
Implement `IGameInstaller` on a component of the `[GameManager]` object (or assign it in the inspector). Installers run after built-in services and before the Running state. Services that implement `IInitializableService` get `InitializeAsync()` after every installer.

```csharp
public sealed class MyGameInstaller : MonoBehaviour, IGameInstaller
{
    public void RegisterStates(GameStateMachine states)
    {
        states.RegisterState(new MenuState());
    }

    public void RegisterServices(ServiceRegistry services, EventAggregator events, GameManagerConfig config)
    {
        services.Register<ISaveService>(new SaveService());
    }
}
```

`GameManager.IsInitialized` is true after boot. `await GameManager.WhenReady()` waits for that. `SubscribeInitialized(handler, this)` runs immediately if boot already finished, otherwise once when it does.

## Installation
Add as a local package or extract directly into your project's `Packages/` or `Assets/` directory.
