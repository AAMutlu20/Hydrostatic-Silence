using System;
using System.Threading.Tasks;
using DialogueSystem.Scripts.Data.Core;
using DialogueSystem.Scripts.Runtime.Events;
using DialogueSystem.Scripts.Runtime.Variables;
using UnityEngine;

namespace DialogueSystem.Scripts.Runtime.Core
{
    /// <summary>
    /// Singleton MonoBehaviour. Entry point for all dialogue.
    /// Owns the Runner, State, VariableStore, and Events.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        // ===== SINGLETON =====
        public static DialogueManager Instance { get; private set; }

        // ===== INSPECTOR =====
        [Header("Settings")]
        [SerializeField] private bool autoSaveVariables = false;
        [SerializeField] private bool dontDestroyOnLoad = true;

        // ===== SUBSYSTEMS =====
        private DialogueRunner  runner;
        private DialogueState   currentState;
        private VariableStore   variableStore;
        private DialogueEvents  dialogueEvents;

        // ===== PUBLIC ACCESSORS =====
        public DialogueEvents  Events        => dialogueEvents;
        public VariableStore   Variables     => variableStore;
        public DialogueState   State         => currentState;
        public bool            IsDialogueActive => currentState?.IsActive ?? false;

        // ===== LIFECYCLE =====

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
            InitializeSystems();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void InitializeSystems()
        {
            currentState   = new DialogueState();
            variableStore  = new VariableStore();
            dialogueEvents = new DialogueEvents();
            runner         = new DialogueRunner(this, variableStore, dialogueEvents, currentState);

            dialogueEvents.OnDialogueEnded += OnDialogueComplete;
        }

        private void OnDialogueComplete()
        {
            if (autoSaveVariables)
            {
                // Hook your save system here when ready:
                // SaveSystem.SaveVariables(variableStore.GetSaveData());
            }
        }

        // ===== PUBLIC API =====

        /// <summary>Start a dialogue graph from its default start node.</summary>
        public async Task StartDialogue(DialogueGraph graph)
        {
            if (graph == null) { Debug.LogWarning("[DialogueManager] Null graph passed."); return; }
            await runner.StartDialogue(graph);
        }

        /// <summary>Start a dialogue graph from a specific node ID.</summary>
        public async Task StartDialogue(DialogueGraph graph, string startNodeID)
        {
            if (graph == null) { Debug.LogWarning("[DialogueManager] Null graph passed."); return; }
            await runner.StartDialogue(graph, startNodeID);
        }

        /// <summary>Stop dialogue immediately.</summary>
        public void StopDialogue() => runner.StopDialogue();

        /// <summary>Advance past the current line / skip typewriter.</summary>
        public void Continue() => runner.Continue();

        /// <summary>Select a choice by index.</summary>
        public void SelectChoice(int index) => runner.SelectChoice(index);

        /// <summary>Register a custom command handler (e.g. "PlayAnimation").</summary>
        public void RegisterCommand(string commandName, Action<string[]> handler)
            => runner.RegisterCommand(commandName, handler);

        /// <summary>Unregister a custom command handler.</summary>
        public void UnregisterCommand(string commandName)
            => runner.UnregisterCommand(commandName);

        // ===== DEBUG =====

        [ContextMenu("Log All Variables")]
        public void LogAllVariables() => variableStore.LogAll();

        [ContextMenu("Reset All Variables")]
        public void ResetAllVariables() => variableStore.Clear();
    }
}
