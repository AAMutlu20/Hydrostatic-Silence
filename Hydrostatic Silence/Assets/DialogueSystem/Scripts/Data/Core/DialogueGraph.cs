using System.Collections.Generic;
using UnityEngine;

namespace DialogueSystem.Scripts.Data.Core
{
    /// <summary>
    /// Master ScriptableObject container that holds all nodes for one dialogue graph.
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue Graph", menuName = "Dialogue System/Dialogue Graph")]
    public class DialogueGraph : ScriptableObject
    {
        // ===== METADATA =====
        [SerializeField] private string graphID;
        [SerializeField] private string graphTitle;
        [SerializeField] private string[] tags;
        [TextArea(3, 6)]
        [SerializeField] private string description;

        // ===== NODES =====
        [SerializeField] private List<DialogueNode> nodes = new List<DialogueNode>();
        [SerializeField] private string startNodeID;

        // ===== EDITOR STATE =====
        [SerializeField] private Vector2 graphPanOffset;
        [SerializeField] private float graphZoom = 1f;

        // ===== RUNTIME CACHE (not serialized) =====
        private Dictionary<string, DialogueNode> nodeCache;

        // ===== PUBLIC PROPERTIES =====
        public string GraphID => graphID;
        public string GraphTitle => graphTitle;
        public string[] Tags => tags;
        public string Description => description;
        public IReadOnlyList<DialogueNode> Nodes => nodes;
        public string StartNodeID => startNodeID;
        public Vector2 GraphPanOffset => graphPanOffset;
        public float GraphZoom => graphZoom;

        // ===== RUNTIME METHODS =====

        public DialogueNode GetNodeByID(string id)
        {
            if (nodeCache == null)
            {
                nodeCache = new Dictionary<string, DialogueNode>();
                foreach (var node in nodes)
                    if (node != null && !string.IsNullOrEmpty(node.NodeID))
                        nodeCache[node.NodeID] = node;
            }
            nodeCache.TryGetValue(id, out var result);
            return result;
        }

        public DialogueNode GetStartNode()
        {
            return GetNodeByID(startNodeID);
        }

        public void InvalidateCache() => nodeCache = null;

#if UNITY_EDITOR
        public void AddNode(DialogueNode node)
        {
            nodes.Add(node);
            InvalidateCache();
        }

        public void RemoveNode(DialogueNode node)
        {
            nodes.Remove(node);
            InvalidateCache();
        }

        public void SetStartNodeID(string id) => startNodeID = id;
        public void SetGraphID(string id) => graphID = id;
        public void SetGraphTitle(string title) => graphTitle = title;
        public void SetPanOffset(Vector2 offset) => graphPanOffset = offset;
        public void SetZoom(float zoom) => graphZoom = zoom;
#endif
    }
}
