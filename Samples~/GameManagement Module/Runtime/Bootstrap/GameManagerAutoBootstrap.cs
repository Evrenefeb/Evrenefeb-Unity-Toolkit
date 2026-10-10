using UnityEngine;

namespace Evrenefeb.Toolkit.GameManagement {
    public static class GameManagerAutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (GameManager.HasInstance) return;

            var config = Resources.Load<GameManagerConfig>("GameManagerConfig");
            if (config != null && !config.AutoBootstrap) return;

            var prefab = Resources.Load<GameObject>("GameManager");
            if (prefab != null)
            {
                var instance = Object.Instantiate(prefab);
                instance.name = "[GameManager]";
            }
            else
            {
                var go = new GameObject("[GameManager]");
                go.AddComponent<GameManager>();
            }
        }
    }
}
