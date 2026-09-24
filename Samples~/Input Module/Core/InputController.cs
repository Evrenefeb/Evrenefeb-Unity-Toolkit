// InputController.cs
// Evrenefeb.Toolkit.Input
//
// A production-ready, plug-and-play input controller built on top of
// Unity's New Input System. Drop this on any GameObject that already has
// (or will get) a PlayerInput component; it will auto-discover every
// Action Map / Action in the assigned InputActionAsset and expose them
// through a simple string-keyed API, C# events, and Inspector-friendly
// UnityEvents — with zero manual binding code and zero per-frame GC.
//
// Design notes:
//  - Discovery happens once, in Awake(). Adding a new action to the
//    .inputactions asset requires no code changes: it is picked up
//    automatically the next time this component initializes.
//  - There is no Update() loop. All "live" values are read on-demand via
//    InputAction.ReadValue<T>() (which does not box/allocate for value
//    types), and all "event" style notifications are pushed by the Input
//    System's own performed/canceled callbacks, subscribed once.
//  - Fully compatible with PlayerInputManager: each player's PlayerInput
//    gets its own InputController instance, its own action dictionary, and
//    its own device/control-scheme state, so split-screen and local/online
//    multiplayer "just work" without extra wiring.

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Evrenefeb.Toolkit.Input
{
    /// <summary>
    /// Per-action UnityEvent hookup exposed in the Inspector so designers can
    /// wire reactions to specific actions (by name) without touching code.
    /// </summary>
    [Serializable]
    public class ActionUnityEvents
    {
        [Tooltip("Must exactly match an Action name inside the assigned InputActionAsset.")]
        public string actionName;

        public UnityEvent onPerformed;
        public UnityEvent onCanceled;
        public UnityEvent onStarted;
    }

    /// <summary>
    /// Read-only debug info about a single discovered action, surfaced purely
    /// for Inspector/runtime visualization (see InputControllerEditor).
    /// </summary>
    [Serializable]
    public class ActionDebugView
    {
        public string mapName;
        public string actionName;
        public string controlType;
        public string currentValue;
        public bool isPressed;
    }

    /// <summary>
    /// Auto-initializing, dynamically-populated input controller.
    /// Requires a <see cref="PlayerInput"/> component (added automatically if missing).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    [AddComponentMenu("Evrenefeb Toolkit/Input/Input Controller")]
    public class InputController : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Configuration")]
        [Tooltip("Optional override. If left empty, the InputActionAsset assigned to this object's PlayerInput component is used.")]
        [SerializeField] private InputActionAsset actionAssetOverride;

        [Tooltip("If true, all discovered actions are enabled automatically on Awake.")]
        [SerializeField] private bool autoEnableActions = true;


        [Header("Designer Events (Inspector wiring, optional)")]
        [Tooltip("Add an entry per action name you want to react to from the Inspector.")]
        [SerializeField] private List<ActionUnityEvents> designerEvents = new List<ActionUnityEvents>();

        [Header("Debug (read-only, populated at runtime)")]
        [SerializeField] private string debugActiveControlScheme;
        [SerializeField] private string debugActiveDevices;
        [SerializeField] private List<ActionDebugView> debugActionStates = new List<ActionDebugView>();

        #endregion

        #region Runtime State

        private PlayerInput _playerInput;
        private InputActionAsset _asset;

        /// <summary>All discovered actions, keyed by Action name. If two actions across different maps share a name, they are additionally keyed as "MapName/ActionName".</summary>
        private readonly Dictionary<string, InputAction> _actionsByName = new Dictionary<string, InputAction>();

        /// <summary>Lookup from action name to its designer-facing UnityEvents (if configured).</summary>
        private readonly Dictionary<string, ActionUnityEvents> _designerEventsByName = new Dictionary<string, ActionUnityEvents>();

        // Cached closures so callback registration in Awake doesn't allocate a new delegate per action.
        private readonly List<(InputAction action, Action<InputAction.CallbackContext> started, Action<InputAction.CallbackContext> performed, Action<InputAction.CallbackContext> canceled)> _subscriptions
            = new List<(InputAction, Action<InputAction.CallbackContext>, Action<InputAction.CallbackContext>, Action<InputAction.CallbackContext>)>();

        public bool IsInitialized { get; private set; }

        #endregion

        #region Public C# Events

        /// <summary>Fired whenever any discovered action starts (name, context).</summary>
        public event Action<string, InputAction.CallbackContext> OnAnyActionStarted;

        /// <summary>Fired whenever any discovered action is performed (name, context).</summary>
        public event Action<string, InputAction.CallbackContext> OnAnyActionPerformed;

        /// <summary>Fired whenever any discovered action is canceled (name, context).</summary>
        public event Action<string, InputAction.CallbackContext> OnAnyActionCanceled;

        /// <summary>Fired when the active control scheme or paired devices change (e.g. keyboard -> gamepad).</summary>
        public event Action<string> OnControlSchemeChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeInternal();
        }

        private void OnEnable()
        {
            if (_playerInput != null)
            {
                _playerInput.onControlsChanged += HandleControlsChanged;
            }
        }

        private void OnDisable()
        {
            if (_playerInput != null)
            {
                _playerInput.onControlsChanged -= HandleControlsChanged;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
        }

        #endregion

        #region Initialization / Auto-Discovery

        /// <summary>
        /// Locates the PlayerInput component, resolves the InputActionAsset, and
        /// (re)discovers every Action Map / Action defined on it. Safe to call
        /// again at runtime (e.g. after swapping the asset) to re-scan.
        /// </summary>
        public void InitializeInternal()
        {
            UnsubscribeAll();
            _actionsByName.Clear();
            _designerEventsByName.Clear();
            debugActionStates.Clear();

            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                Debug.LogError($"[InputController] No PlayerInput component found on '{name}'. " +
                                "This component requires PlayerInput (RequireComponent should have added it).", this);
                enabled = false;
                return;
            }

            _asset = actionAssetOverride != null ? actionAssetOverride : _playerInput.actions;
            if (_asset == null)
            {
                Debug.LogError($"[InputController] No InputActionAsset assigned. Assign one on the PlayerInput " +
                                "component or via the 'Action Asset Override' field.", this);
                enabled = false;
                return;
            }

            foreach (var map in _asset.actionMaps)
            {
                foreach (var action in map.actions)
                {
                    RegisterAction(map.name, action);
                }
            }

            // Index designer-configured UnityEvents by action name for O(1) callback lookup.
            foreach (var entry in designerEvents)
            {
                if (!string.IsNullOrEmpty(entry.actionName) && !_designerEventsByName.ContainsKey(entry.actionName))
                {
                    _designerEventsByName.Add(entry.actionName, entry);
                }
            }

            if (autoEnableActions)
            {
                _asset.Enable();
            }

            RefreshDebugView();
            IsInitialized = true;
        }

        private void RegisterAction(string mapName, InputAction action)
        {
            // Primary key: bare action name (most common lookup, e.g. "Jump").
            if (!_actionsByName.ContainsKey(action.name))
            {
                _actionsByName.Add(action.name, action);
            }

            // Secondary, unambiguous key: "Map/Action", useful if names collide across maps.
            string qualifiedName = $"{mapName}/{action.name}";
            if (!_actionsByName.ContainsKey(qualifiedName))
            {
                _actionsByName.Add(qualifiedName, action);
            }

            // Subscribe once; closures capture the action's own name for dispatch.
            string dispatchName = action.name;

            Action<InputAction.CallbackContext> started = ctx => Dispatch(dispatchName, ctx, DispatchKind.Started);
            Action<InputAction.CallbackContext> performed = ctx => Dispatch(dispatchName, ctx, DispatchKind.Performed);
            Action<InputAction.CallbackContext> canceled = ctx => Dispatch(dispatchName, ctx, DispatchKind.Canceled);

            action.started += started;
            action.performed += performed;
            action.canceled += canceled;

            _subscriptions.Add((action, started, performed, canceled));

            debugActionStates.Add(new ActionDebugView
            {
                mapName = mapName,
                actionName = action.name,
                controlType = action.expectedControlType,
                currentValue = string.Empty,
                isPressed = false
            });
        }

        private void UnsubscribeAll()
        {
            foreach (var sub in _subscriptions)
            {
                sub.action.started -= sub.started;
                sub.action.performed -= sub.performed;
                sub.action.canceled -= sub.canceled;
            }
            _subscriptions.Clear();
        }

        private enum DispatchKind { Started, Performed, Canceled }

        private void Dispatch(string actionName, InputAction.CallbackContext ctx, DispatchKind kind)
        {
            switch (kind)
            {
                case DispatchKind.Started:
                    OnAnyActionStarted?.Invoke(actionName, ctx);
                    break;
                case DispatchKind.Performed:
                    OnAnyActionPerformed?.Invoke(actionName, ctx);
                    break;
                case DispatchKind.Canceled:
                    OnAnyActionCanceled?.Invoke(actionName, ctx);
                    break;
            }

            if (_designerEventsByName.TryGetValue(actionName, out var events))
            {
                switch (kind)
                {
                    case DispatchKind.Started: events.onStarted?.Invoke(); break;
                    case DispatchKind.Performed: events.onPerformed?.Invoke(); break;
                    case DispatchKind.Canceled: events.onCanceled?.Invoke(); break;
                }
            }
        }

        #endregion

        #region Multiplayer / Device Info

        private void HandleControlsChanged(PlayerInput input)
        {
            RefreshDebugView();
            OnControlSchemeChanged?.Invoke(GetActiveControlScheme());
        }

        /// <summary>Current control scheme name (e.g. "Keyboard&amp;Mouse", "Gamepad"), or "None" if unresolved.</summary>
        public string GetActiveControlScheme()
        {
            if (_playerInput == null) return "None";
            return string.IsNullOrEmpty(_playerInput.currentControlScheme) ? "None" : _playerInput.currentControlScheme;
        }

        /// <summary>Comma-separated list of device names currently paired to this player.</summary>
        public string GetActiveDeviceNames()
        {
            if (_playerInput == null || _playerInput.devices.Count == 0) return "None";

            var sb = new StringBuilder();
            for (int i = 0; i < _playerInput.devices.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(_playerInput.devices[i].displayName);
            }
            return sb.ToString();
        }

        /// <summary>This player's index, as assigned by PlayerInputManager (0 for a lone single-player instance).</summary>
        public int PlayerIndex => _playerInput != null ? _playerInput.playerIndex : -1;

        #endregion

        #region Public Getter API (string-keyed, designer/coder friendly)

        /// <summary>Attempts to resolve an InputAction by name (bare name or "Map/Action").</summary>
        public bool TryGetAction(string actionName, out InputAction action)
        {
            return _actionsByName.TryGetValue(actionName, out action);
        }

        /// <summary>True while the named button/action is currently held down.</summary>
        public bool IsPressed(string actionName)
        {
            return TryGetAction(actionName, out var action) && action.IsPressed();
        }

        /// <summary>True exactly on the frame the named action transitioned to pressed.</summary>
        public bool WasPressedThisFrame(string actionName)
        {
            return TryGetAction(actionName, out var action) && action.WasPressedThisFrame();
        }

        /// <summary>True exactly on the frame the named action transitioned to released.</summary>
        public bool WasReleasedThisFrame(string actionName)
        {
            return TryGetAction(actionName, out var action) && action.WasReleasedThisFrame();
        }

        /// <summary>Reads the named action's current value as a Vector2 (e.g. "Move", "Look"). Returns Vector2.zero if not found or mismatched type.</summary>
        public Vector2 GetVector2(string actionName)
        {
            if (!TryGetAction(actionName, out var action)) return Vector2.zero;
            try { return action.ReadValue<Vector2>(); }
            catch (InvalidOperationException) { return Vector2.zero; }
        }

        /// <summary>Reads the named action's current value as a float (e.g. an analog trigger). Returns 0 if not found or mismatched type.</summary>
        public float GetFloat(string actionName)
        {
            if (!TryGetAction(actionName, out var action)) return 0f;
            try { return action.ReadValue<float>(); }
            catch (InvalidOperationException) { return 0f; }
        }

        #endregion

        

        #region Extensibility: Rebinding & Persistence Readiness
        //
        // These are intentionally thin wrappers around the Input System's own
        // rebinding/override serialization. They give downstream code a stable
        // extension point (e.g. a future RebindUI) without committing to a
        // specific rebinding-flow implementation yet.

        /// <summary>
        /// Serializes all current binding overrides (from any prior rebinding)
        /// on the underlying asset to JSON, suitable for saving to disk/PlayerPrefs.
        /// </summary>
        public string SaveBindingOverridesAsJson()
        {
            return _asset != null ? _asset.SaveBindingOverridesAsJson() : string.Empty;
        }

        /// <summary>
        /// Applies previously-saved binding overrides (from <see cref="SaveBindingOverridesAsJson"/>) to the underlying asset.
        /// </summary>
        public void LoadBindingOverridesFromJson(string json)
        {
            if (_asset != null && !string.IsNullOrEmpty(json))
            {
                _asset.LoadBindingOverridesFromJson(json);
            }
        }

        /// <summary>
        /// Begins an interactive rebind for the given action/binding index.
        /// Returns the InputActionRebindingExtensions.RebindingOperation so callers
        /// can subscribe to OnComplete/OnCancel and Dispose() it themselves.
        /// This is the seam future rebind-UI work should build on.
        /// </summary>
        public InputActionRebindingExtensions.RebindingOperation StartInteractiveRebind(string actionName, int bindingIndex = 0)
        {
            if (!TryGetAction(actionName, out var action))
            {
                Debug.LogWarning($"[InputController] Cannot rebind unknown action '{actionName}'.", this);
                return null;
            }

            action.Disable();
            return action.PerformInteractiveRebinding(bindingIndex)
                .OnComplete(_ => action.Enable())
                .OnCancel(_ => action.Enable())
                .Start();
        }

        #endregion

        #region Debug / Inspector Support

        /// <summary>Rebuilds the read-only debug view of all discovered actions. Called on init and on control-scheme change; never per-frame.</summary>
        public void RefreshDebugView()
        {
            debugActiveControlScheme = GetActiveControlScheme();
            debugActiveDevices = GetActiveDeviceNames();

            foreach (var view in debugActionStates)
            {
                if (_actionsByName.TryGetValue(view.actionName, out var action))
                {
                    view.isPressed = action.IsPressed();
                    view.currentValue = SafeReadValueAsString(action);
                }
            }
        }

        private static string SafeReadValueAsString(InputAction action)
        {
            try
            {
                var value = action.ReadValueAsObject();
                return value != null ? value.ToString() : "-";
            }
            catch
            {
                return "-";
            }
        }

        /// <summary>Read-only accessor for the custom editor to draw the current debug state.</summary>
        public IReadOnlyList<ActionDebugView> DebugActionStates => debugActionStates;
        public string DebugControlScheme => debugActiveControlScheme;
        public string DebugDevices => debugActiveDevices;

        #endregion
    }
}
