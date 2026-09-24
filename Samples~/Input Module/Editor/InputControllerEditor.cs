// InputControllerEditor.cs
// Evrenefeb.Toolkit.Input
//
// Optional bonus: a custom inspector that shows live action states, the
// active control scheme, and paired device names while in Play Mode.
// Purely a visualization layer — it reads InputController's public debug
// accessors and never mutates gameplay state.
//
// Must live in an "Editor" folder (or be wrapped in #if UNITY_EDITOR, as
// done here) so it is stripped from player builds.

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Evrenefeb.Toolkit.Input
{
    [CustomEditor(typeof(InputController))]
    public class InputControllerEditor : Editor
    {
        private bool _showActionStates = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var controller = (InputController)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Debug", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see live action states, control scheme, and devices.", MessageType.Info);
                return;
            }

            if (!controller.IsInitialized)
            {
                EditorGUILayout.HelpBox("InputController has not finished initializing yet.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Active Control Scheme", controller.DebugControlScheme);
            EditorGUILayout.LabelField("Active Devices", controller.DebugDevices);
            EditorGUILayout.LabelField("Player Index", controller.PlayerIndex.ToString());

            EditorGUILayout.Space();
            _showActionStates = EditorGUILayout.Foldout(_showActionStates, "Discovered Actions", true);
            if (_showActionStates)
            {
                EditorGUI.indentLevel++;
                foreach (var view in controller.DebugActionStates)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var color = view.isPressed ? Color.green : GUI.color;
                        var prevColor = GUI.color;
                        GUI.color = color;
                        EditorGUILayout.LabelField($"{view.mapName}/{view.actionName}", $"[{view.controlType}] {view.currentValue}");
                        GUI.color = prevColor;
                    }
                }
                EditorGUI.indentLevel--;
            }

            // Keep the inspector live-updating while in Play Mode.
            controller.RefreshDebugView();
            Repaint();
        }
    }
}
#endif
