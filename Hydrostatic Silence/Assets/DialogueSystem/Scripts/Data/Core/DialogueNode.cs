using UnityEngine;
using DialogueSystem.Scripts.Data.Supporting.Enums;

namespace DialogueSystem.Scripts.Data.Core
{
    /// <summary>
    /// Abstract base class for all dialogue nodes. Stored as ScriptableObjects.
    /// </summary>
    public abstract class DialogueNode : ScriptableObject
    {
        // ===== IDENTITY =====
        [SerializeField] private string nodeID;
        [SerializeField] private string nodeName;

        // ===== EDITOR METADATA =====
        [SerializeField] private Vector2 editorPosition;
        [SerializeField] private Color nodeColor = Color.white;

        // ===== OPTIONAL METADATA =====
        [TextArea(1, 3)]
        [SerializeField] private string comment;
        [SerializeField] private string[] nodeTags;

        // ===== PUBLIC PROPERTIES =====
        public string NodeID => nodeID;
        public string NodeName => nodeName;
        public Vector2 EditorPosition => editorPosition;
        public Color NodeColor => nodeColor;
        public string Comment => comment;
        public string[] NodeTags => nodeTags;

        // ===== ABSTRACT =====
        public abstract NodeType GetNodeType();

#if UNITY_EDITOR
        public void SetNodeID(string id) => nodeID = id;
        public void SetEditorPosition(Vector2 pos) => editorPosition = pos;
        public void SetNodeName(string name) => nodeName = name;
        public void GenerateNewID() => nodeID = System.Guid.NewGuid().ToString();
#endif
    }
}
