#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using DialogueSystem.Scripts.Data.Core;
using DialogueSystem.Scripts.Editor.Windows;

namespace DialogueSystem.Scripts.Editor
{
    /// <summary>
    /// Custom inspector for DialogueGraph assets.
    /// Adds an "Open in Editor" button in the Project window inspector.
    /// </summary>
    [CustomEditor(typeof(DialogueGraph))]
    public class DialogueGraphInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var graph = (DialogueGraph)target;

            EditorGUILayout.LabelField("Dialogue Graph", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (GUILayout.Button("Open in Dialogue Editor", GUILayout.Height(30)))
                DialogueGraphEditorWindow.OpenGraph(graph);

            EditorGUILayout.Space(8);
            DrawDefaultInspector();
        }
    }
}
#endif
