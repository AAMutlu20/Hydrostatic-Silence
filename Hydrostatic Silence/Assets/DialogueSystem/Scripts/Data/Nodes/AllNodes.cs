using UnityEngine;
using UnityEngine.Events;
using DialogueSystem.Scripts.Data.Core;
using DialogueSystem.Scripts.Data.Supporting.Classes;
using DialogueSystem.Scripts.Data.Supporting.Enums;

namespace DialogueSystem.Scripts.Data.Nodes
{
    // =====================================================================
    // START NODE
    // =====================================================================

    /// <summary>Entry point of every dialogue graph. Has one output port only.</summary>
    [CreateAssetMenu(fileName = "StartNode", menuName = "Dialogue System/Nodes/Start Node")]
    public class StartNode : DialogueNode
    {
        [SerializeField] private string nextNodeID;

        public string NextNodeID => nextNodeID;
        public override NodeType GetNodeType() => NodeType.Start;

#if UNITY_EDITOR
        public void SetNextNodeID(string id) => nextNodeID = id;
#endif
    }

    // =====================================================================
    // DIALOGUE LINE NODE
    // =====================================================================

    /// <summary>Displays a single line of dialogue with optional portrait and audio.</summary>
    [CreateAssetMenu(fileName = "DialogueLineNode", menuName = "Dialogue System/Nodes/Dialogue Line Node")]
    public class DialogueLineNode : DialogueNode
    {
        [Header("Content")]
        [TextArea(3, 8)]
        [SerializeField] private string dialogueText;
        [SerializeField] private string speakerName;

        [Header("Portrait")]
        [SerializeField] private Sprite portrait;
        [SerializeField] private string emotion;

        [Header("Audio")]
        [SerializeField] private AudioClip voiceClip;

        [Header("Timing")]
        [Tooltip("Characters per second. 0 = instant.")]
        [SerializeField] private float typewriterSpeed = 40f;
        [Tooltip("Auto-advance after this delay. 0 = wait for player input.")]
        [SerializeField] private float autoAdvanceDelay = 0f;

        [Header("Flow")]
        [SerializeField] private string nextNodeID;

        public string DialogueText => dialogueText;
        public string SpeakerName => speakerName;
        public Sprite Portrait => portrait;
        public string Emotion => emotion;
        public AudioClip VoiceClip => voiceClip;
        public float TypewriterSpeed => typewriterSpeed;
        public float AutoAdvanceDelay => autoAdvanceDelay;
        public string NextNodeID => nextNodeID;

        public override NodeType GetNodeType() => NodeType.DialogueLine;

#if UNITY_EDITOR
        public void SetNextNodeID(string id) => nextNodeID = id;
#endif
    }

    // =====================================================================
    // CHOICE NODE
    // =====================================================================

    /// <summary>Presents the player with a list of choices. Each choice can have conditions and commands.</summary>
    [CreateAssetMenu(fileName = "ChoiceNode", menuName = "Dialogue System/Nodes/Choice Node")]
    public class ChoiceNode : DialogueNode
    {
        [Header("Prompt (optional)")]
        [TextArea(2, 5)]
        [SerializeField] private string promptText;

        [Header("Choices")]
        [SerializeField] private ChoiceOption[] choices;

        public string PromptText => promptText;
        public ChoiceOption[] Choices => choices;

        public override NodeType GetNodeType() => NodeType.Choice;
    }

    // =====================================================================
    // BRANCH NODE
    // =====================================================================

    /// <summary>Evaluates conditions and routes to a true or false node.</summary>
    [CreateAssetMenu(fileName = "BranchNode", menuName = "Dialogue System/Nodes/Branch Node")]
    public class BranchNode : DialogueNode
    {
        [Header("Conditions")]
        [SerializeField] private Condition[] conditions;
        [SerializeField] private ConditionMode conditionMode = ConditionMode.All;

        [Header("Outputs")]
        [SerializeField] private string trueNodeID;
        [SerializeField] private string falseNodeID;

        public Condition[] Conditions => conditions;
        public ConditionMode ConditionMode => conditionMode;
        public string TrueNodeID => trueNodeID;
        public string FalseNodeID => falseNodeID;

        public override NodeType GetNodeType() => NodeType.Branch;

#if UNITY_EDITOR
        public void SetTrueNodeID(string id) => trueNodeID = id;
        public void SetFalseNodeID(string id) => falseNodeID = id;
#endif
    }

    // =====================================================================
    // SET VARIABLE NODE
    // =====================================================================

    /// <summary>Writes one or more variables to the VariableStore, then advances.</summary>
    [CreateAssetMenu(fileName = "SetVariableNode", menuName = "Dialogue System/Nodes/Set Variable Node")]
    public class SetVariableNode : DialogueNode
    {
        [SerializeField] private VariableAssignment[] assignments;
        [SerializeField] private string nextNodeID;

        public VariableAssignment[] Assignments => assignments;
        public string NextNodeID => nextNodeID;

        public override NodeType GetNodeType() => NodeType.SetVariable;

#if UNITY_EDITOR
        public void SetNextNodeID(string id) => nextNodeID = id;
#endif
    }

    // =====================================================================
    // EVENT NODE
    // =====================================================================

    /// <summary>Fires a named event + UnityEvent so game systems can react, then advances.</summary>
    [CreateAssetMenu(fileName = "EventNode", menuName = "Dialogue System/Nodes/Event Node")]
    public class EventNode : DialogueNode
    {
        [Header("Event")]
        [SerializeField] private string eventName;
        [SerializeField] private string[] parameters;
        [SerializeField] private UnityEvent onExecute;

        [Header("Flow")]
        [SerializeField] private string nextNodeID;

        public string EventName => eventName;
        public string[] Parameters => parameters;
        public UnityEvent OnExecute => onExecute;
        public string NextNodeID => nextNodeID;

        public override NodeType GetNodeType() => NodeType.Event;

#if UNITY_EDITOR
        public void SetNextNodeID(string id) => nextNodeID = id;
#endif
    }

    // =====================================================================
    // END NODE
    // =====================================================================

    /// <summary>Marks the end of dialogue execution and defines what happens next.</summary>
    [CreateAssetMenu(fileName = "EndNode", menuName = "Dialogue System/Nodes/End Node")]
    public class EndNode : DialogueNode
    {
        [SerializeField] private EndBehavior endBehavior = EndBehavior.CloseDialogue;
        [Tooltip("Used when EndBehavior is LoadNextGraph.")]
        [SerializeField] private DialogueGraph nextGraph;

        public EndBehavior EndBehavior => endBehavior;
        public DialogueGraph NextGraph => nextGraph;

        public override NodeType GetNodeType() => NodeType.End;
    }
}
