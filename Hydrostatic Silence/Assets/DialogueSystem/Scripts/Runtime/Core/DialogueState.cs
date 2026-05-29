using System.Collections.Generic;
using DialogueSystem.Scripts.Data.Core;

namespace DialogueSystem.Scripts.Runtime.Core
{
    /// <summary>
    /// Tracks the current state of dialogue execution (active graph, current node, history).
    /// Not a MonoBehaviour — owned by DialogueManager.
    /// </summary>
    public class DialogueState
    {
        private DialogueGraph currentGraph;
        private DialogueNode  currentNode;
        private bool          isActive;

        private readonly Stack<DialogueNode> nodeHistory = new Stack<DialogueNode>();
        private const int MaxHistorySize = 50;

        public DialogueGraph CurrentGraph => currentGraph;
        public DialogueNode  CurrentNode  => currentNode;
        public bool          IsActive     => isActive;

        public void StartDialogue(DialogueGraph graph, DialogueNode startNode)
        {
            currentGraph = graph;
            currentNode  = startNode;
            isActive     = true;
            nodeHistory.Clear();
        }

        public void MoveToNode(DialogueNode node)
        {
            if (currentNode != null && nodeHistory.Count < MaxHistorySize)
                nodeHistory.Push(currentNode);
            currentNode = node;
        }

        public void EndDialogue()
        {
            isActive = false;
        }

        public void Reset()
        {
            currentGraph = null;
            currentNode  = null;
            isActive     = false;
            nodeHistory.Clear();
        }

        public IReadOnlyCollection<DialogueNode> GetHistory() => nodeHistory;

        public DialogueNode GetPreviousNode() => nodeHistory.Count > 0 ? nodeHistory.Peek() : null;
    }
}
