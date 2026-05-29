using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DialogueSystem.Scripts.Data.Core;
using DialogueSystem.Scripts.Data.Nodes;
using DialogueSystem.Scripts.Data.Supporting.Classes;
using DialogueSystem.Scripts.Data.Supporting.Enums;
using DialogueSystem.Scripts.Runtime.Events;
using DialogueSystem.Scripts.Runtime.Variables;
using UnityEngine;

namespace DialogueSystem.Scripts.Runtime.Core
{
    /// <summary>
    /// Async execution engine. Walks the graph node by node,
    /// fires events for the UI, and waits for player input signals.
    /// </summary>
    public class DialogueRunner
    {
        // ===== DEPENDENCIES =====
        private readonly DialogueManager manager;
        private readonly VariableStore   variableStore;
        private readonly DialogueEvents  events;
        private readonly DialogueState   state;

        // ===== EXECUTION STATE =====
        private CancellationTokenSource cts;
        private bool isWaitingForInput;
        private bool isWaitingForChoice;
        private int  selectedChoiceIndex = -1;
        private bool skipTypewriter;

        // ===== COMMAND REGISTRY =====
        private readonly Dictionary<string, Action<string[]>> commandHandlers
            = new Dictionary<string, Action<string[]>>();

        // ===== CONSTRUCTOR =====

        public DialogueRunner(DialogueManager manager, VariableStore variableStore,
                              DialogueEvents events, DialogueState state)
        {
            this.manager       = manager;
            this.variableStore = variableStore;
            this.events        = events;
            this.state         = state;
        }

        // ===== PUBLIC API =====

        public async Task StartDialogue(DialogueGraph graph, string startNodeID = null)
        {
            StopDialogue();
            cts = new CancellationTokenSource();

            var startNode = string.IsNullOrEmpty(startNodeID)
                ? graph.GetStartNode()
                : graph.GetNodeByID(startNodeID);

            if (startNode == null)
            {
                Debug.LogError($"[DialogueRunner] Could not find start node in graph '{graph.GraphTitle}'.");
                return;
            }

            state.StartDialogue(graph, startNode);
            events.FireDialogueStarted();

            try
            {
                await ExecuteNode(startNode);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[DialogueRunner] Dialogue cancelled.");
            }
        }

        public void StopDialogue()
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
            isWaitingForInput  = false;
            isWaitingForChoice = false;
            selectedChoiceIndex = -1;
        }

        /// <summary>Advance past the current dialogue line (called by UI continue button).</summary>
        public void Continue()
        {
            if (skipTypewriter)
            {
                skipTypewriter = false;
                events.FireTypewriterSkipped();
            }
            else
            {
                isWaitingForInput = false;
            }
        }

        /// <summary>Register a choice index (called by UI choice buttons).</summary>
        public void SelectChoice(int index)
        {
            selectedChoiceIndex = index;
            isWaitingForChoice  = false;
        }

        /// <summary>Skip the typewriter mid-animation.</summary>
        public void SkipTypewriter() => skipTypewriter = true;

        // ===== COMMAND REGISTRY =====

        public void RegisterCommand(string name, Action<string[]> handler)   => commandHandlers[name] = handler;
        public void UnregisterCommand(string name)                           => commandHandlers.Remove(name);

        // ===== NODE EXECUTION =====

        private async Task ExecuteNode(DialogueNode node)
        {
            if (node == null || cts.IsCancellationRequested) return;

            state.MoveToNode(node);

            switch (node.GetNodeType())
            {
                case NodeType.Start:       await ExecuteStart((Data.Nodes.StartNode)node);           break;
                case NodeType.DialogueLine: await ExecuteDialogueLine((Data.Nodes.DialogueLineNode)node); break;
                case NodeType.Choice:      await ExecuteChoice((Data.Nodes.ChoiceNode)node);         break;
                case NodeType.Branch:      await ExecuteBranch((Data.Nodes.BranchNode)node);         break;
                case NodeType.SetVariable: await ExecuteSetVariable((Data.Nodes.SetVariableNode)node); break;
                case NodeType.Event:       await ExecuteEvent((Data.Nodes.EventNode)node);           break;
                case NodeType.End:         await ExecuteEnd((Data.Nodes.EndNode)node);               break;
                default:
                    Debug.LogWarning($"[DialogueRunner] Unknown node type: {node.GetNodeType()}");
                    break;
            }
        }

        private async Task ExecuteStart(Data.Nodes.StartNode node)
        {
            var next = state.CurrentGraph.GetNodeByID(node.NextNodeID);
            await ExecuteNode(next);
        }

        private async Task ExecuteDialogueLine(Data.Nodes.DialogueLineNode node)
        {
            if (node.VoiceClip != null)
                events.FireVoiceClipRequested(node.VoiceClip);

            events.FireDialogueLine(node.DialogueText, node.SpeakerName, node.Portrait, node.Emotion);
            events.FireTypewriterStarted(node.TypewriterSpeed);

            // Wait for typewriter to finish (UI fires FireTypewriterFinished when done)
            // Then wait for player to press continue (unless auto-advance is set)
            if (node.AutoAdvanceDelay > 0f)
            {
                await Task.Delay((int)(node.AutoAdvanceDelay * 1000), cts.Token);
            }
            else
            {
                isWaitingForInput = true;
                while (isWaitingForInput && !cts.IsCancellationRequested)
                    await Task.Yield();
            }

            var next = state.CurrentGraph.GetNodeByID(node.NextNodeID);
            await ExecuteNode(next);
        }

        private async Task ExecuteChoice(Data.Nodes.ChoiceNode node)
        {
            // Filter choices by conditions
            var available = new List<ChoiceOption>();
            foreach (var choice in node.Choices)
            {
                bool valid = true;
                if (choice.conditions != null)
                {
                    foreach (var c in choice.conditions)
                        if (!variableStore.Evaluate(c)) { valid = false; break; }
                }
                if (valid) available.Add(choice);
            }

            events.FireChoicesPresented(available);

            selectedChoiceIndex = -1;
            isWaitingForChoice  = true;
            while (isWaitingForChoice && !cts.IsCancellationRequested)
                await Task.Yield();

            events.FireChoicesHidden();
            events.FireChoiceSelected(selectedChoiceIndex);

            if (selectedChoiceIndex < 0 || selectedChoiceIndex >= available.Count)
            {
                Debug.LogWarning("[DialogueRunner] Invalid choice index.");
                return;
            }

            var chosen = available[selectedChoiceIndex];

            // Fire any commands on the choice
            if (chosen.commands != null)
                foreach (var cmd in chosen.commands)
                    FireCommand(cmd);

            var next = state.CurrentGraph.GetNodeByID(chosen.nextNodeID);
            await ExecuteNode(next);
        }

        private async Task ExecuteBranch(Data.Nodes.BranchNode node)
        {
            bool result;
            if (node.ConditionMode == ConditionMode.All)
                result = node.Conditions.All(c => variableStore.Evaluate(c));
            else
                result = node.Conditions.Any(c => variableStore.Evaluate(c));

            var nextID = result ? node.TrueNodeID : node.FalseNodeID;
            var next   = state.CurrentGraph.GetNodeByID(nextID);
            await ExecuteNode(next);
        }

        private async Task ExecuteSetVariable(Data.Nodes.SetVariableNode node)
        {
            foreach (var assignment in node.Assignments)
                variableStore.Apply(assignment.variableName, assignment.variableType,
                                    assignment.operation, assignment.value);

            var next = state.CurrentGraph.GetNodeByID(node.NextNodeID);
            await ExecuteNode(next);
        }

        private async Task ExecuteEvent(Data.Nodes.EventNode node)
        {
            events.FireCustomEvent(node.EventName, node.Parameters);
            node.OnExecute?.Invoke();
            FireCommand(node.EventName, node.Parameters);

            var next = state.CurrentGraph.GetNodeByID(node.NextNodeID);
            await ExecuteNode(next);
        }

        private async Task ExecuteEnd(Data.Nodes.EndNode node)
        {
            state.EndDialogue();
            events.FireDialogueEnded();

            switch (node.EndBehavior)
            {
                case EndBehavior.RestartDialogue:
                    await StartDialogue(state.CurrentGraph);
                    break;
                case EndBehavior.LoadNextGraph:
                    if (node.NextGraph != null)
                        await StartDialogue(node.NextGraph);
                    break;
                // CloseDialogue: do nothing, dialogue is done
            }

            await Task.CompletedTask;
        }

        // ===== HELPERS =====

        private void FireCommand(string commandName, string[] parameters = null)
        {
            if (commandHandlers.TryGetValue(commandName, out var handler))
                handler?.Invoke(parameters ?? Array.Empty<string>());
        }

        private void FireCommand(Command cmd)
        {
            FireCommand(cmd.commandName, cmd.parameters);
        }
    }
}
