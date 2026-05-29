#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueSystem.Scripts.Data.Core;
using DialogueSystem.Scripts.Editor.GraphView;

namespace DialogueSystem.Scripts.Editor.Windows
{
    /// <summary>
    /// The main editor window. Open via Tools > Dialogue System > Open Editor.
    /// </summary>
    public class DialogueGraphEditorWindow : EditorWindow
    {
        private DialogueGraphView graphView;
        private DialogueGraph     currentGraph;
        private Label             graphTitleLabel;

        [MenuItem("Tools/Dialogue System/Open Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<DialogueGraphEditorWindow>();
            window.titleContent = new GUIContent("Dialogue Graph Editor");
            window.minSize      = new Vector2(800, 600);
        }

        /// <summary>Open a specific graph in the editor.</summary>
        public static void OpenGraph(DialogueGraph graph)
        {
            var window = GetWindow<DialogueGraphEditorWindow>();
            window.titleContent = new GUIContent("Dialogue Graph Editor");
            window.minSize      = new Vector2(800, 600);
            window.LoadGraph(graph);
        }

        private void OnEnable()
        {
            BuildUI();
        }

        private void OnDisable()
        {
            // Save pan/zoom back to asset before closing
            if (currentGraph != null && graphView != null)
                graphView.SaveGraphState(currentGraph);
        }

        // ===== UI SETUP =====

        private void BuildUI()
        {
            rootVisualElement.Clear();

            // Toolbar
            var toolbar = new UnityEditor.UIElements.Toolbar();

            var saveButton = new UnityEditor.UIElements.ToolbarButton(SaveGraph) { text = "Save" };
            var loadButton = new UnityEditor.UIElements.ToolbarButton(PromptLoadGraph) { text = "Load" };
            var clearButton = new UnityEditor.UIElements.ToolbarButton(ClearGraph) { text = "Clear" };

            graphTitleLabel = new Label("No graph loaded");
            graphTitleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            graphTitleLabel.style.flexGrow       = 1;

            toolbar.Add(saveButton);
            toolbar.Add(loadButton);
            toolbar.Add(clearButton);
            toolbar.Add(graphTitleLabel);
            rootVisualElement.Add(toolbar);

            // Graph view
            graphView = new DialogueGraphView(this);
            graphView.style.flexGrow = 1;
            rootVisualElement.Add(graphView);
        }

        // ===== GRAPH OPERATIONS =====

        public void LoadGraph(DialogueGraph graph)
        {
            if (graph == null) return;
            currentGraph = graph;
            graphTitleLabel.text = graph.GraphTitle;
            graphView.PopulateFromGraph(graph);
        }

        private void SaveGraph()
        {
            if (currentGraph == null)
            {
                Debug.LogWarning("[DialogueEditor] No graph loaded to save.");
                return;
            }
            graphView.SaveToGraph(currentGraph);
            EditorUtility.SetDirty(currentGraph);
            AssetDatabase.SaveAssets();
            Debug.Log($"[DialogueEditor] Saved graph '{currentGraph.GraphTitle}'.");
        }

        private void PromptLoadGraph()
        {
            string path = EditorUtility.OpenFilePanel("Load Dialogue Graph", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;

            // Convert absolute path to relative
            path = "Assets" + path.Substring(Application.dataPath.Length);
            var graph = AssetDatabase.LoadAssetAtPath<DialogueGraph>(path);
            if (graph != null) LoadGraph(graph);
            else Debug.LogWarning("[DialogueEditor] Could not load graph at: " + path);
        }

        private void ClearGraph()
        {
            if (EditorUtility.DisplayDialog("Clear Graph", "Remove all nodes from the view?", "Yes", "Cancel"))
                graphView.ClearView();
        }
    }
}
#endif
