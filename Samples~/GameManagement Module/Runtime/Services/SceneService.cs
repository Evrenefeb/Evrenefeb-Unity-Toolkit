using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Evrenefeb.Toolkit.GameManagement {
    public interface ISceneService : IGameService
    {
        Task LoadSceneAsync(string sceneName, Action<float> onProgress = null);
    }

    public class SceneService : ISceneService
    {
        private readonly float _minLoadingTime;

        public SceneService(float minLoadingTime)
        {
            _minLoadingTime = minLoadingTime;
        }

        public async Task LoadSceneAsync(string sceneName, Action<float> onProgress = null)
        {
            GameLog.Info($"Starting async load for scene: {sceneName}");
            float startTime = Time.realtimeSinceStartup;

            var operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                onProgress?.Invoke(progress);

                if (operation.progress >= 0.9f)
                {
                    float elapsedTime = Time.realtimeSinceStartup - startTime;
                    if (elapsedTime < _minLoadingTime)
                    {
                        await Task.Delay((int)((_minLoadingTime - elapsedTime) * 1000));
                    }
                    operation.allowSceneActivation = true;
                }

                await Task.Yield();
            }

            GameLog.Info($"Successfully loaded scene: {sceneName}");
        }
    }
}
