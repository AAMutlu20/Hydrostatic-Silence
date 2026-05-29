#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueSystem.Scripts.Data.Core;
using DialogueSystem.Scripts.Data.Nodes;

namespace DialogueSystem.Scripts.Editor.GraphView
{
    // =====================================================================
    // BASE NODE VIEW
    // =====================================================================

    /// <summary>
    /// Base class for all node views. Manages ports and provides hooks for subclasses.
    /// </summary>
    public abstract class BaseDialogueNodeView : Node
    {
        public DialogueNode Node { get; private set; }

        protected Port inputPort;
        protected readonly Dictionary<string, Port> outputPorts = new Dictionary<string, Port>();

        protected BaseDialogueNodeView(DialogueNode node, string title, Color titleColor)
        {
            Node = node;
            this.title = title;
            titleContainer.style.backgroundColor = new StyleColor(titleColor);

            AddInputPort();
            BuildContent();
            RefreshExpandedState();
            RefreshPorts();
        }

        // ===== PORTS =====

        protected void AddInputPort(string portName = "In")
        {
            inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            inputPort.portName = portName;
            inputContainer.Add(inputPort);
        }

        protected Port AddOutputPort(string portName = "Out", Port.Capacity capacity = Port.Capacity.Single)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, capacity, typeof(bool));
            port.portName = portName;
            outputContainer.Add(port);
            outputPorts[portName] = port;
            return port;
        }

        public Port GetInputPort()                      => inputPort;
        public Port GetOutputPort(string portName)      => outputPorts.TryGetValue(portName, out var p) ? p : null;
        public Port GetFirstOutputPort()                => outputPorts.Count > 0 ? outputPorts["Out"] : null;

        // ===== ABSTRACT =====

        protected abstract void BuildContent();

        /// <summary>Return a list of (outputPortName, targetNodeID) for edge reconstruction on load.</summary>
        public abstract IEnumerable<(string portName, string targetID)> GetConnections();

        /// <summary>Called when an edge is drawn, to write the target ID back to the node data.</summary>
        public abstract void ApplyEdge(string outputPortName, string targetNodeID);

        // ===== HELPERS =====

        protected TextField CreateTextField(string label, string value, EventCallback<ChangeEvent<string>> onChange)
        {
            var field = new TextField(label) { value = value, multiline = true };
            field.RegisterValueChangedCallback(onChange);
            return field;
        }
    }

    // =====================================================================
    // START NODE VIEW
    // =====================================================================

    public class StartNodeView : BaseDialogueNodeView
    {
        private StartNode startNode => (StartNode)Node;

        public StartNodeView(DialogueNode node) : base(node, "▶ Start", new Color(0.25f, 0.55f, 0.25f)) { }

        protected override void BuildContent()
        {
            AddOutputPort("Out");
        }

        public override IEnumerable<(string, string)> GetConnections()
        {
            yield return ("Out", startNode.NextNodeID);
        }

        public override void ApplyEdge(string outputPortName, string targetNodeID)
        {
            startNode.SetNextNodeID(targetNodeID);
        }
    }

    // =====================================================================
    // DIALOGUE LINE NODE VIEW
    // =====================================================================

    public class DialogueLineNodeView : BaseDialogueNodeView
    {
        private DialogueLineNode lineNode => (DialogueLineNode)Node;

        public DialogueLineNodeView(DialogueNode node) : base(node, "💬 Dialogue Line", new Color(0.2f, 0.35f, 0.55f)) { }

        protected override void BuildContent()
        {
            var speakerField = new TextField("Speaker") { value = lineNode.SpeakerName };
            speakerField.RegisterValueChangedCallback(e =>
                typeof(DialogueLineNode).GetField("speakerName",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(lineNode, e.newValue));
            extensionContainer.Add(speakerField);

            var textField = new TextField("Text") { value = lineNode.DialogueText, multiline = true };
            textField.style.whiteSpace = WhiteSpace.Normal;
            textField.style.minHeight  = 60;
            extensionContainer.Add(textField);

            AddOutputPort("Out");
        }

        public override IEnumerable<(string, string)> GetConnections()
        {
            yield return ("Out", lineNode.NextNodeID);
        }

        public override void ApplyEdge(string outputPortName, string targetNodeID)
        {
            lineNode.SetNextNodeID(targetNodeID);
        }
    }

    // =====================================================================
    // CHOICE NODE VIEW
    // =====================================================================

    public class ChoiceNodeView : BaseDialogueNodeView
    {
        private ChoiceNode choiceNode => (ChoiceNode)Node;

        public ChoiceNodeView(DialogueNode node) : base(node, "🔀 Choice", new Color(0.55f, 0.40f, 0.10f)) { }

        protected override void BuildContent()
        {
            if (choiceNode.Choices != null)
                foreach (var choice in choiceNode.Choices)
                    AddChoicePort(choice.text);

            var addBtn = new Button(AddNewChoicePort) { text = "+ Add Choice" };
            extensionContainer.Add(addBtn);
        }

        private void AddChoicePort(string label)
        {
            var port = AddOutputPort(string.IsNullOrEmpty(label) ? "Choice" : label);
        }

        private void AddNewChoicePort()
        {
            AddChoicePort($"Choice {outputPorts.Count + 1}");
            RefreshPorts();
            RefreshExpandedState();
        }

        public override IEnumerable<(string, string)> GetConnections()
        {
            if (choiceNode.Choices == null) yield break;
            for (int i = 0; i < choiceNode.Choices.Length; i++)
                yield return (choiceNode.Choices[i].text, choiceNode.Choices[i].nextNodeID);
        }

        public override void ApplyEdge(string outputPortName, string targetNodeID)
        {
            if (choiceNode.Choices == null) return;
            foreach (var c in choiceNode.Choices)
                if (c.text == outputPortName) c.nextNodeID = targetNodeID;
        }
    }

    // =====================================================================
    // BRANCH NODE VIEW
    // =====================================================================

    public class BranchNodeView : BaseDialogueNodeView
    {
        private BranchNode branchNode => (BranchNode)Node;

        public BranchNodeView(DialogueNode node) : base(node, "⚙ Branch", new Color(0.45f, 0.20f, 0.55f)) { }

        protected override void BuildContent()
        {
            AddOutputPort("True");
            AddOutputPort("False");
        }

        public override IEnumerable<(string, string)> GetConnections()
        {
            yield return ("True",  branchNode.TrueNodeID);
            yield return ("False", branchNode.FalseNodeID);
        }

        public override void ApplyEdge(string outputPortName, string targetNodeID)
        {
            if (outputPortName == "True")  branchNode.SetTrueNodeID(targetNodeID);
            if (outputPortName == "False") branchNode.SetFalseNodeID(targetNodeID);
        }
    }

    // =====================================================================
    // SET VARIABLE NODE VIEW
    // =====================================================================

    public class SetVariableNodeView : BaseDialogueNodeView
    {
        private SetVariableNode setVarNode => (SetVariableNode)Node;

        public SetVariableNodeView(DialogueNode node) : base(node, "📝 Set Variable", new Color(0.20f, 0.45f, 0.45f)) { }

        protected override void BuildContent()
        {
            if (setVarNode.Assignments != null)
                foreach (var a in setVarNode.Assignments)
                    extensionContainer.Add(new Label($"{a.variableName} {a.operation} {a.value?.intValue}"));

            AddOutputPort("Out");
        }

        public override IEnumerable<(string, string)> GetConnections()
        {
            yield return ("Out", setVarNode.NextNodeID);
        }

        public override void ApplyEdge(string outputPortName, string targetNodeID)
        {
            setVarNode.SetNextNodeID(targetNodeID);
        }
    }

    // =====================================================================
    // EVENT NODE VIEW
    // =====================================================================

    public class EventNodeView : BaseDialogueNodeView
    {
        private EventNode eventNode => (EventNode)Node;

        public EventNodeView(DialogueNode node) : base(node, "⚡ Event", new Color(0.55f, 0.45f, 0.10f)) { }

        protected override void BuildContent()
        {
            extensionContainer.Add(new Label(eventNode.EventName));
            AddOutputPort("Out");
        }

        public override IEnumerable<(string, string)> GetConnections()
        {
            yield return ("Out", eventNode.NextNodeID);
        }

        public override void ApplyEdge(string outputPortName, string targetNodeID)
        {
            eventNode.SetNextNodeID(targetNodeID);
        }
    }

    // =====================================================================
    // END NODE VIEW
    // =====================================================================

    public class EndNodeView : BaseDialogueNodeView
    {
        public EndNodeView(DialogueNode node) : base(node, "⏹ End", new Color(0.55f, 0.20f, 0.20f)) { }

        protected override void BuildContent()
        {
            // No output ports — this is a terminal node
        }

        public override IEnumerable<(string, string)> GetConnections()
        {
            yield break;
        }

        public override void ApplyEdge(string outputPortName, string targetNodeID) { }
    }

    // =====================================================================
    // GENERIC FALLBACK NODE VIEW
    // =====================================================================

    public class GenericNodeView : BaseDialogueNodeView
    {
        public GenericNodeView(DialogueNode node) : base(node, node.GetType().Name, Color.gray) { }

        protected override void BuildContent()
        {
            AddOutputPort("Out");
        }

        public override IEnumerable<(string, string)> GetConnections()
        {
            yield break;
        }

        public override void ApplyEdge(string outputPortName, string targetNodeID) { }
    }
}

namespace DialogueSystem.Scripts.Editor.Nodes
{
    // Namespace alias so the editor window can reference node view types cleanly
    using DialogueSystem.Scripts.Editor.GraphView;
}
#endif
