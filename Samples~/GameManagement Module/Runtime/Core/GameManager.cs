using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Toolkit.Core.Events;
using Toolkit.Core.Services;
using Toolkit.Core.State;

namespace Toolkit.Core
{
    [DefaultExecutionOrder(-1000)]
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<GameManager>();
                }
                return _instance;
            }
        }

        public static bool HasInstance => _instance != null;
        public static bool IsInitialized { get; private set; }

        [SerializeField] private GameManagerConfig config;

        [Tooltip("Extra composition hooks. Also picks up IGameInstaller components on this GameObject.")]
        [SerializeField] private MonoBehaviour[] installers;

        private readonly ServiceRegistry _services = new ServiceRegistry();
        private readonly EventAggregator _events = new EventAggregator();
        private readonly List<Action> _initializedListeners = new List<Action>();
        private GameStateMachine _stateMachine;
        private TaskCompletionSource<bool> _readySource = new TaskCompletionSource<bool>();

        public static EventAggregator Events => HasInstance ? _instance._events : null;
        public static GameStateMachine StateMachine => HasInstance ? _instance._stateMachine : null;
        public GameManagerConfig Config => config;



        private async void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _stateMachine = new GameStateMachine(_events);

            if (config == null)
            {
                config = Resources.Load<GameManagerConfig>("GameManagerConfig") ?? ScriptableObject.CreateInstance<GameManagerConfig>();
            }

            GameLog.CurrentLogLevel = config.SystemLogLevel;

            if (config.TargetFrameRate > 0)
            {
                Application.targetFrameRate = config.TargetFrameRate;
            }

            QualitySettings.vSyncCount = config.VSyncCount;

            await InitializeLifecycleAsync();
        }

        private async Task InitializeLifecycleAsync()
        {
            GameLog.Info("Lifecycle: Initializing Core Orchestrator...");

            // 1. Setup State Machine
            _stateMachine.RegisterState(new BootState());
            _stateMachine.RegisterState(new RunningState());
            await _stateMachine.ChangeStateAsync<BootState>();

            // 2. Register Built-in Services
            RegisterBuiltInServices();

            // 3. Game-specific states and services
            RunInstallers();

            await _services.InitializeAllAsync();

            // İlk frame'in tamamlanmasını bekle (UI/Editor yüklemeleri için zaman tanır)
            await Task.Yield();

            if (this == null || _instance != this)
            {
                return;
            }

            // 4. Move to Running State
            await _stateMachine.ChangeStateAsync<RunningState>();

            if (this == null || _instance != this)
            {
                return;
            }

            CompleteInitialization();
        }

        private void RegisterBuiltInServices()
        {
            _services.Register<ISceneService>(new SceneService(config.MinimumLoadingScreenTime));
            _services.Register<IPauseService>(new PauseService(_events));
            _services.Register<IUpdateManagerService>(new UpdateManagerService(
                config.InitialTickCapacity,
                config.InitialFixedTickCapacity,
                config.InitialLateTickCapacity
            ));
        }

        private void RunInstallers()
        {
            var seen = new HashSet<IGameInstaller>();

            var onObject = GetComponents<MonoBehaviour>();
            for (int i = 0; i < onObject.Length; i++)
            {
                if (onObject[i] is IGameInstaller installer)
                {
                    InvokeInstaller(installer, seen);
                }
            }

            if (installers == null)
            {
                return;
            }

            for (int i = 0; i < installers.Length; i++)
            {
                if (installers[i] is IGameInstaller installer)
                {
                    InvokeInstaller(installer, seen);
                }
                else if (installers[i] != null)
                {
                    GameLog.Warning($"{installers[i].name} is assigned as an installer but does not implement IGameInstaller.");
                }
            }
        }

        private void InvokeInstaller(IGameInstaller installer, HashSet<IGameInstaller> seen)
        {
            if (installer == null || !seen.Add(installer))
            {
                return;
            }

            installer.RegisterStates(_stateMachine);
            installer.RegisterServices(_services, _events, config);
            GameLog.Info($"Ran installer: {installer.GetType().Name}");
        }

        private void CompleteInitialization()
        {
            IsInitialized = true;
            _readySource.TrySetResult(true);
            _events.Publish(new GameInitializedEvent());

            for (int i = 0; i < _initializedListeners.Count; i++)
            {
                try
                {
                    _initializedListeners[i]?.Invoke();
                }
                catch (Exception ex)
                {
                    GameLog.Error($"Initialized listener threw {ex.GetType().Name}: {ex.Message}");
                }
            }

            _initializedListeners.Clear();
        }

        /// <summary>
        /// Completes when boot finishes. Already completed if initialization is done.
        /// </summary>
        public static Task WhenReady()
        {
            if (!HasInstance)
            {
                GameLog.Error("GameManager.WhenReady called before an instance exists.");
                return Task.CompletedTask;
            }

            return IsInitialized ? Task.CompletedTask : _instance._readySource.Task;
        }

        /// <summary>
        /// Runs immediately if the manager is already initialized, otherwise once when it is.
        /// Pass a Unity owner so a destroyed listener is dropped instead of leaking.
        /// </summary>
        public static void SubscribeInitialized(Action handler, UnityEngine.Object owner = null)
        {
            if (handler == null)
            {
                return;
            }

            if (!HasInstance)
            {
                GameLog.Error("GameManager.SubscribeInitialized called before an instance exists.");
                return;
            }

            if (IsInitialized)
            {
                if (IsOwnerDestroyed(owner))
                {
                    return;
                }

                handler();
                return;
            }

            _instance._initializedListeners.Add(() =>
            {
                if (IsOwnerDestroyed(owner))
                {
                    return;
                }

                handler();
            });
        }

        private static bool IsOwnerDestroyed(UnityEngine.Object owner)
        {
            return !ReferenceEquals(owner, null) && owner == null;
        }

        public static T Get<T>() where T : class, IGameService
        {
            if (!HasInstance)
            {
                GameLog.Error($"GameManager.Get<{typeof(T).Name}> called before an instance exists.");
                return null;
            }

            return _instance._services.Get<T>();
        }

        public static bool TryGet<T>(out T service) where T : class, IGameService
        {
            if (!HasInstance)
            {
                service = null;
                return false;
            }

            return _instance._services.TryGet(out service);
        }

        public static Task ChangeStateAsync<T>() where T : class, IGameState
        {
            if (!HasInstance || _instance._stateMachine == null)
            {
                GameLog.Error($"GameManager.ChangeStateAsync<{typeof(T).Name}> called before the state machine exists.");
                return Task.CompletedTask;
            }

            return _instance._stateMachine.ChangeStateAsync<T>();
        }

        public static Task ChangeStateAsync<T, TPayload>(TPayload payload) where T : class, IPayloadState<TPayload>
        {
            if (!HasInstance || _instance._stateMachine == null)
            {
                GameLog.Error($"GameManager.ChangeStateAsync<{typeof(T).Name}> called before the state machine exists.");
                return Task.CompletedTask;
            }

            return _instance._stateMachine.ChangeStateAsync<T, TPayload>(payload);
        }

        public static void Pause(PauseReason reason = PauseReason.UserMenu)
        {
            Get<IPauseService>()?.Pause(reason);
        }

        public static void Resume(PauseReason reason = PauseReason.UserMenu)
        {
            Get<IPauseService>()?.Resume(reason);
        }

        public static async Task LoadSceneAsync(string sceneName, System.Action<float> onProgress = null)
        {
            var sceneService = Get<ISceneService>();
            if (sceneService != null)
            {
                await sceneService.LoadSceneAsync(sceneName, onProgress);
            }
        }

        private void Update()
        {
            _services.TickAll();
            _stateMachine?.Tick(Time.deltaTime);
        }

        private void FixedUpdate() => _services.FixedTickAll();
        private void LateUpdate()  => _services.LateTickAll();

        public static bool IsQuitting { get; private set; }

        private void OnApplicationQuit() {
            IsQuitting = true;
        }

        private void OnDestroy() {
            if (_instance == this) {
                IsInitialized = false;
                _readySource.TrySetResult(false);
                _initializedListeners.Clear();
                _services.DisposeAll();
                _events.Clear();
                IsQuitting = true;
                _instance = null;
            }
        }
    }



}
