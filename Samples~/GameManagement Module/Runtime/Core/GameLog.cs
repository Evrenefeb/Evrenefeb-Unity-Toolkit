using System;
using UnityEngine;

namespace Toolkit.Core
{
    public static class GameLog
    {
        public static LogLevel CurrentLogLevel { get; set; } = LogLevel.Info;

        public static void Info(string message)
        {
            if (CurrentLogLevel <= LogLevel.Info)
                Debug.Log($"<color=#4CAF50>[GameToolkit]</color> {message}");
        }

        public static void Warning(string message)
        {
            if (CurrentLogLevel <= LogLevel.Warning)
                Debug.LogWarning($"<color=#FFC107>[GameToolkit]</color> {message}");
        }

        public static void Error(string message)
        {
            if (CurrentLogLevel <= LogLevel.Error)
                Debug.LogError($"<color=#F44336>[GameToolkit]</color> {message}");
        }
    }
}
