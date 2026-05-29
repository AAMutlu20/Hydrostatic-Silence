# Claude mi sepraratena failovete, shtoto men me murzi. Ei ti AI generated readme

# Dialogue System - Unity 6 URP

A generic, reusable node-based dialogue system.
No game-specific logic — drop it into any project.

\---

## Folder Structure

```
Assets/DialogueSystem/
├── Scripts/
│   ├── Data/
│   │   ├── Core/
│   │   │   ├── DialogueNode.cs          ← abstract base
│   │   │   └── DialogueGraph.cs         ← master container (ScriptableObject)
│   │   ├── Nodes/
│   │   │   └── AllNodes.cs              ← all 7 node types
│   │   └── Supporting/
│   │       ├── Enums/
│   │       │   ├── NodeType.cs
│   │       │   ├── VariableType.cs
│   │       │   └── OtherEnums.cs        ← ComparisonOperator, VariableOperation, etc.
│   │       └── Classes/
│   │           └── SupportingClasses.cs ← Condition, ChoiceOption, Command, etc.
│   ├── Runtime/
│   │   ├── Core/
│   │   │   ├── DialogueManager.cs       ← singleton MonoBehaviour, entry point
│   │   │   ├── DialogueRunner.cs        ← async execution engine
│   │   │   └── DialogueState.cs         ← current graph/node tracking
│   │   ├── Events/
│   │   │   └── DialogueEvents.cs        ← event bus between Runtime and UI
│   │   └── Variables/
│   │       └── VariableStore.cs         ← runtime variable storage + evaluation
│   ├── UI/
│   │   └── DialogueUI.cs                ← DialogueUI + TypewriterEffect + ChoiceButton
│   └── Editor/
│       ├── GraphView/
│       │   ├── DialogueGraphView.cs     ← the GraphView canvas
│       │   └── NodeViews.cs             ← all 7 node view types + base
│       ├── Windows/
│       │   └── DialogueGraphEditorWindow.cs
│       ├── Utilities/StyleSheets/
│       │   └── DialogueGraphStyles.uss
│       └── DialogueGraphInspector.cs    ← "Open in Editor" button in Inspector
```

\---

## Setup

### 1\. Create a DialogueGraph asset

Right-click in Project → Create → Dialogue System → Dialogue Graph

### 2\. Open the editor

Select the asset → click "Open in Dialogue Editor"  
OR: Tools → Dialogue System → Open Editor

### 3\. Create nodes

Right-click in the canvas to add nodes.
Draw edges between ports to connect them.
Hit **Save** in the toolbar.

### 4\. Add DialogueManager to your scene

Create empty GameObject → Add Component → DialogueManager.
It is a DontDestroyOnLoad singleton.

### 5\. Hook up the UI

Add the DialogueUI component to your Canvas.
Assign the prefab references (dialogue panel, choice button prefab, etc.) in the Inspector.
The UI subscribes to DialogueManager.Instance.Events automatically.

### 6\. Start a dialogue from code

```csharp
await DialogueManager.Instance.StartDialogue(myDialogueGraphAsset);
```

\---

## Variables

Set variables inside the graph with **SetVariableNode**.
Read them from code via:

```csharp
bool val   = DialogueManager.Instance.Variables.GetBool("framework");
int score  = DialogueManager.Instance.Variables.GetInt("someScore");
string txt = DialogueManager.Instance.Variables.GetString("playerName");
```

\---

## Custom Commands

Register a command handler in your game code:

```csharp
DialogueManager.Instance.RegisterCommand("PlayAnimation", args => {
    animator.Play(args\[0]);
});
```

Then fire it from an EventNode with eventName = "PlayAnimation" and parameters = \["Idle"].

\---

## Node Types

|Node|Purpose|
|-|-|
|Start|Entry point of every graph|
|Dialogue Line|Shows a line of text, optional portrait + voice|
|Choice|Shows player choices (with optional conditions)|
|Branch|Checks variables → routes true/false|
|Set Variable|Writes variables (set/add/subtract/multiply/divide)|
|Event|Fires a named event + UnityEvent|
|End|Terminates dialogue (close / restart / load next graph)|



