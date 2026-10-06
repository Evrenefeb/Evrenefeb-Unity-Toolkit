# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development & Workflow
- This repository is a Unity package (`com.toolkit.gamemanager`) providing a modular GameManager system.
- Since it's a Unity package designed to be extracted locally or added via package manager, test it within an active Unity project running Play Mode. 
- You can inspect active states at runtime via the included Editor window: **Toolkit -> GameManager Dashboard**.

## High-Level Architecture
- **GameManager (Singleton Orchestrator)**: `GameManager.cs` acts as the entry point and `MonoBehaviour` host. It persists via `DontDestroyOnLoad` and initializes subsystems like the state machine, services, and event aggregator.
  - Boot order: core states → built-in services → `IGameInstaller` hooks → `IInitializableService.InitializeAsync()` → Running → `GameInitializedEvent`.
  - Extend without editing the package: implement `IGameInstaller` on a component of the GameManager (or assign it in the `installers` list).
  - `GameManager.IsInitialized` / `WhenReady()` / `SubscribeInitialized` are the ready gate. `Get`, `TryGet`, and `ChangeStateAsync` no-op with a log when no instance exists.
- **Service Locator (`ServiceRegistry.cs`)**: Used for decoupling subsystems. Centralized in `GameManager._services`.
  - Fetch services using `GameManager.Get<T>()` or `GameManager.TryGet<T>(out var)`.
  - Custom services must implement `IGameService`. Register them during setup or inside `InitializeLifecycleAsync()`. 
  - Included services: `SceneService` (async scene loading), `PauseService` (bitflag-based multi-reason pausing), `UpdateManagerService` (high-performance entity tick manager).
- **Entity Tick System (`UpdateManagerService.cs`)**: For scene objects that need per-frame updates (enemies, vehicles, AI), implement `IEntityTick` / `IFixedEntityTick` / `ILateEntityTick` and register with `GameManager.Get<IUpdateManagerService>()`. Do NOT use MonoBehaviour `Update()` for large numbers of objects — route everything through this service instead. Removal uses O(1) swap-and-pop to avoid list-shift overhead.
- **Event Bus (`EventAggregator.cs`)**: A type-based pub/sub system decoupled from `MonoBehaviour` events.
  - Event payloads must be `readonly struct`s that implement `IGameEvent`.
  - Usage: `GameManager.Events.Subscribe<MyEvent>(Handler, owner: this)` and `GameManager.Events.Publish(new MyEvent())`.
  - Subscribe returns `IDisposable` — dispose it (or pass a Unity `owner`) so handlers do not leak against the DontDestroyOnLoad bus. `SubscribeOnce` auto-removes after the first invoke. Higher `priority` runs first.
  - Core events reside as readonly structs in `CoreEvents.cs` (e.g., `GameInitializedEvent`, `GamePausedEvent`).
- **State Machine (`GameStateMachine.cs`)**: Type-based FSM driving high-level game flows. 
  - Async-first transitions: Substates should inherit from `BaseGameState` or `BasePayloadGameState<T>`. Transitions use `await GameManager.ChangeStateAsync<TState>()`.
  - Payloads supported via `IPayloadState<TPayload>`.
  - State changes automatically publish a `GameStateChangedEvent` through `EventAggregator`.
- **Bootstrapping**: Instantiation is handled automatically before scene load by `GameManagerAutoBootstrap.cs` if enabled in the configuration. It will optionally instantiate a prefab named `GameManager` from a `Resources` folder, or create a default `GameObject` fallback.
- **Configuration**: Globally configurable via ScriptableObject `GameManagerConfig`, which must be placed in a `Resources` folder for runtime loading. Controls log verbosity, auto-bootstrap, transition times, frame rate/VSync, and entity tick pre-allocation capacities.

## Code Style & Conventions
- Use `Toolkit.Core` / `Toolkit.Core.*` namespaces for runtime code, and `Toolkit.Editor` for editor scripts.
- Prioritize constructor dependency injection for standalone classes (e.g. `PauseService(EventAggregator events)`), avoiding singletons where feasible outside of `GameManager`.
- Use the package's builtin `GameLog` wrapper instead of `Debug.Log`. Log levels are controlled by `GameManagerConfig`.
- Stick to standard C# naming conventions: PascalCase for classes/methods/properties, camelCase for local variables, and `_camelCase` for private fields (as seen in `_minLoadingTime` / `_instance`).