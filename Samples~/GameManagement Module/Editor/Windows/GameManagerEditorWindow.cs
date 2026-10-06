using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Toolkit.Core;
using Toolkit.Core.Events;
using Toolkit.Core.Services;

namespace Toolkit.Editor
{
    public class GameManagerEditorWindow : EditorWindow
    {
        private bool _eventsFoldout = true;
        private readonly List<EventDiagnostic> _eventDiagnostics = new List<EventDiagnostic>();

        [MenuItem("Toolkit/GameManager Dashboard")]
        public static void ShowWindow()
        {
            GetWindow<GameManagerEditorWindow>("GameManager Dashboard");
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Unity Toolkit - GameManager Dashboard", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Run game in Play Mode to inspect active services and runtime state.", MessageType.Info);
                return;
            }

            if (GameManager.HasInstance)
            {
                EditorGUILayout.LabelField("Status", GameManager.IsInitialized ? "Initialized" : "Booting", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Log Level", GameManager.Instance.Config != null ? GameManager.Instance.Config.SystemLogLevel.ToString() : "Default");

                // Active State
                var state = GameManager.StateMachine?.CurrentState;
                EditorGUILayout.LabelField("Current State", state != null ? state.GetType().Name : "None");

                // Pause status
                if (GameManager.TryGet<IPauseService>(out var pauseService))
                {
                    EditorGUILayout.LabelField("Is Paused", pauseService.IsPaused.ToString());
                    EditorGUILayout.LabelField("Pause Reason", pauseService.CurrentPauseReason.ToString());
                }

                DrawEventDiagnostics();

                EditorGUILayout.Space();
                if (GUILayout.Button("Trigger Garbage Collection"))
                {
                    System.GC.Collect();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No active GameManager instance found in current scene.", MessageType.Warning);
            }
        }

        private void DrawEventDiagnostics()
        {
            var events = GameManager.Events;
            if (events == null)
            {
                return;
            }

            events.GetDiagnostics(_eventDiagnostics);
            int total = events.GetSubscriberCount();

            EditorGUILayout.Space();
            _eventsFoldout = EditorGUILayout.Foldout(_eventsFoldout, $"Event Subscribers ({total})", true);
            if (!_eventsFoldout)
            {
                return;
            }

            EditorGUI.indentLevel++;
            if (_eventDiagnostics.Count == 0)
            {
                EditorGUILayout.LabelField("None");
            }
            else
            {
                for (int i = 0; i < _eventDiagnostics.Count; i++)
                {
                    var diagnostic = _eventDiagnostics[i];
                    EditorGUILayout.LabelField(diagnostic.EventTypeName, diagnostic.SubscriberCount.ToString());
                }
            }
            EditorGUI.indentLevel--;
        }
    }
}