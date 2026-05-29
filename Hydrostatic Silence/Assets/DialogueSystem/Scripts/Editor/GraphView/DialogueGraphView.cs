#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueSystem.Scripts.Data.Core;
using DialogueSystem.Scripts.Data.Nodes;
using DialogueSystem.Scripts.Data.Supporting.Enums;
using DialogueSystem.Scripts.Editor.GraphView;
using DialogueSystem.Scripts.Editor.Nodes;
using DialogueSystem.Scripts.Editor.Windows;

namespace DialogueSystem.Scripts.Editor.GraphView
{
    /// <summary>
    /// The main GraphView canvas. Handles node creation, layout, and serialization.
    /// </summary>
    public class DialogueGraphView : UnityEditor.Experimental.GraphView.GraphView
    {
        private readonly DialogueGraphEditorWindow editorWindow;
        private MiniMap minimap;

        public DialogueGraphView(DialogueGraphEditorWindow window)
        {
            editorWindow = window;

            // Load stylesheet
            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/DialogueSystem/Scripts/Editor/Utilities/StyleSheets/DialogueGraphStyles.uss");
            if (stylesheet != null) styleSheets.Add(stylesheet);

            // Grid background
            Insert(0, new GridBackground());

            // Manipulators
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // Right-click context menu
            this.AddManipulator(BuildContextMenu());

            // Minimap
            AddMinimap();

            // Style
            style.flexGrow = 1;
        }

        // ===== MINIMAP =====

        private void AddMinimap()
        {
            minimap = new MiniMap { anchored = true };
            minimap.SetPosition(new Rect(10, 30, 200, 140));
            Add(minimap);
        }

        public void ToggleMinimap() => minimap.visible = !minimap.visible;

        // ===== CONTEXT MENU =====

        private IManipulator BuildContextMenu()
        {
            return new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Add Node/Start",       _ => CreateNode(typeof(StartNode),       GetLocalMousePosition(evt.localMousePosition)));
                evt.menu.AppendAction("Add Node/Dialogue Line", _ => CreateNode(typeof(DialogueLineNode), GetLocalMousePosition(evt.localMousePosition)));
                evt.menu.AppendAction("Add Node/Choice",      _ => CreateNode(typeof(ChoiceNode),      GetLocalMousePosition(evt.localMousePosition)));
                evt.menu.AppendAction("Add Node/Branch",      _ => CreateNode(typeof(BranchNode),      GetLocalMousePosition(evt.localMousePosition)));
                evt.menu.AppendAction("Add Node/Set Variable",_ => CreateNode(typeof(SetVariableNode), GetLocalMousePosition(evt.localMousePosition)));
                evt.menu.AppendAction("Add Node/Event",       _ => CreateNode(typeof(EventNode),       GetLocalMousePosition(evt.localMousePosition)));
                evt.menu.AppendAction("Add Node/End",         _ => CreateNode(typeof(EndNode),         GetLocalMousePosition(evt.localMousePosition)));
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Toggle Minimap",       _ => ToggleMinimap());
            });
        }

        // ===== NODE CREATION =====

        public void CreateNode(Type nodeType, Vector2 position)
        {
            var node = ScriptableObject.CreateInstance(nodeType) as DialogueNode;
            if (node == null) return;

            node.GenerateNewID();
            node.SetEditorPosition(position);
            node.SetNodeName(nodeType.Name.Replace("Node", ""));

            var nodeView = CreateNodeView(node);
            nodeView.SetPosition(new Rect(position, Vector2.zero));
            AddElement(nodeView);
        }

        private BaseDialogueNodeView CreateNodeView(DialogueNode node)
        {
            BaseDialogueNodeView view = node switch
            {
                StartNode        => new StartNodeView(node),
                DialogueLineNode => new DialogueLineNodeView(node),
                ChoiceNode       => new ChoiceNodeView(node),
                BranchNode       => new BranchNodeView(node),
                SetVariableNode  => new SetVariableNodeView(node),
                EventNode        => new EventNodeView(node),
                EndNode          => new EndNodeView(node),
                _                => new GenericNodeView(node)
            };
            return view;
        }

        // ===== SAVE / LOAD =====

        /// <summary>Write current view state into a DialogueGraph asset.</summary>
        public void SaveToGraph(DialogueGraph graph)
        {
            graph.SetPanOffset(contentViewContainer.transform.position);
            graph.SetZoom(contentViewContainer.transform.scale.x);

            // Remove old sub-assets
            var existingNodes = new List<string>();
            foreach (var n in graph.Nodes) existingNodes.Add(AssetDatabase.GetAssetPath(n));
            // (simple approach: clear and re-add)
            // A production system would diff and only update changed nodes.

            // Collect all node views
            var nodeViews = new List<BaseDialogueNodeView>();
            nodes.ForEach(n => { if (n is BaseDialogueNodeView v) nodeViews.Add(v); });

            // Collect edges
            var edgeList = new List<Edge>();
            edges.ForEach(e => edgeList.Add(e));

            // Wire up nextNodeIDs from edges before saving
            foreach (var edge in edgeList)
            {
                if (edge.output?.node is BaseDialogueNodeView fromView &&
                    edge.input?.node is BaseDialogueNodeView toView)
                {
                    fromView.ApplyEdge(edge.output.portName, toView.Node.NodeID);
                }
            }

            // Save nodes as sub-assets
            foreach (var view in nodeViews)
            {
                var node = view.Node;
                node.SetEditorPosition(view.GetPosition().position);
                if (!AssetDatabase.IsSubAsset(node))
                    AssetDatabase.AddObjectToAsset(node, graph);
                graph.AddNode(node);
            }

            EditorUtility.SetDirty(graph);
        }

        /// <summary>Populate the view from a saved DialogueGraph asset.</summary>
        public void PopulateFromGraph(DialogueGraph graph)
        {
            ClearView();

            // Restore pan/zoom
            UpdateViewTransform(graph.GraphPanOffset, new Vector3(graph.GraphZoom, graph.GraphZoom, 1f));

            // Recreate node views
            var viewMap = new Dictionary<string, BaseDialogueNodeView>();
            foreach (var node in graph.Nodes)
            {
                var view = CreateNodeView(node);
                view.SetPosition(new Rect(node.EditorPosition, Vector2.zero));
                AddElement(view);
                viewMap[node.NodeID] = view;
            }

            // Recreate edges (node views must expose their ports)
            foreach (var node in graph.Nodes)
            {
                if (!viewMap.TryGetValue(node.NodeID, out var fromView)) continue;
                foreach (var (portName, targetID) in fromView.GetConnections())
                {
                    if (string.IsNullOrEmpty(targetID)) continue;
                    if (!viewMap.TryGetValue(targetID, out var toView)) continue;

                    var outputPort = fromView.GetOutputPort(portName);
                    var inputPort  = toView.GetInputPort();
                    if (outputPort == null || inputPort == null) continue;

                    var edge = outputPort.ConnectTo(inputPort);
                    AddElement(edge);
                }
            }
        }

        public void ClearView()
        {
            DeleteElements(graphElements.ToList());
        }

        public void SaveGraphState(DialogueGraph graph)
        {
            graph.SetPanOffset(contentViewContainer.transform.position);
            graph.SetZoom(contentViewContainer.transform.scale.x);
        }

        // ===== HELPERS =====

        private Vector2 GetLocalMousePosition(Vector2 mousePosition)
        {
            return contentViewContainer.WorldToLocal(mousePosition);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatible = new List<Port>();
            ports.ForEach(port =>
            {
                if (startPort.node != port.node && startPort.direction != port.direction)
                    compatible.Add(port);
            });
            return compatible;
        }
    }
}
#endif
