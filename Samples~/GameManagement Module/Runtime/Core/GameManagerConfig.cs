using UnityEngine;

namespace Toolkit.Core
{
    [CreateAssetMenu(fileName = "GameManagerConfig", menuName = "Toolkit/GameManager Config")]
    public class GameManagerConfig : ScriptableObject
    {
        [Header("Bootstrap & Diagnostics")]
        [Tooltip("Automatically instantiate the GameManager on start before the first scene loads.")]
        public bool AutoBootstrap = true;

        [Tooltip("Controls the verbosity of the internal GameLog system.")]
        public LogLevel SystemLogLevel = LogLevel.Info;

        [Header("Scene Management")]
        [Tooltip("Minimum duration in seconds for scene transitions (ensures loading screens don't flicker).")]
        [Min(0f)]
        public float MinimumLoadingScreenTime = 0.5f;

        [Header("Entity Tick System Capacities")]
        [Tooltip("Pre-allocated capacity for standard tickable entities to prevent GC allocation spikes.")]
        [Min(10)]
        public int InitialTickCapacity = 1000;

        [Tooltip("Pre-allocated capacity for physics/fixed tickable entities.")]
        [Min(10)]
        public int InitialFixedTickCapacity = 500;

        [Tooltip("Pre-allocated capacity for late tickable entities.")]
        [Min(10)]
        public int InitialLateTickCapacity = 500;

        [Header("Application & Performance")]
        [Tooltip("Set a target framerate on launch (-1 to use platform default).")]
        public int TargetFrameRate = -1;

        [Tooltip("Configure VSync count (0 = Disabled, 1 = Every VBlank, 2 = Every Second VBlank).")]
        [Range(0, 2)]
        public int VSyncCount = 0;
    }
}