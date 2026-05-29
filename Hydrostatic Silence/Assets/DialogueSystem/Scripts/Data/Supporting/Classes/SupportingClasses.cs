using System;
using UnityEngine;

namespace DialogueSystem.Scripts.Data.Supporting.Classes
{
    /// <summary>
    /// A variable value that can hold bool, int, float, or string.
    /// </summary>
    [Serializable]
    public class VariableValue
    {
        public bool boolValue;
        public int intValue;
        public float floatValue;
        public string stringValue;
    }

    /// <summary>
    /// A single condition to evaluate against the VariableStore.
    /// </summary>
    [Serializable]
    public class Condition
    {
        public string variableName;
        public Enums.VariableType variableType;
        public Enums.ComparisonOperator comparison;
        public VariableValue compareValue;
    }

    /// <summary>
    /// A command string with optional parameters, fired via EventNode or ChoiceNode.
    /// </summary>
    [Serializable]
    public class Command
    {
        public string commandName;
        public string[] parameters;
    }

    /// <summary>
    /// A variable write operation performed by SetVariableNode.
    /// </summary>
    [Serializable]
    public class VariableAssignment
    {
        public string variableName;
        public Enums.VariableType variableType;
        public Enums.VariableOperation operation;
        public VariableValue value;
    }

    /// <summary>
    /// A single choice option shown to the player in a ChoiceNode.
    /// </summary>
    [Serializable]
    public class ChoiceOption
    {
        [TextArea(1, 3)]
        public string text;
        public string nextNodeID;

        // Optional: conditions that must be met for this choice to appear
        public Condition[] conditions;
        // Optional: commands fired when this choice is selected
        public Command[] commands;
    }

    /// <summary>
    /// Maps an emotion label to a sprite for portrait display.
    /// </summary>
    [Serializable]
    public class EmotionSprite
    {
        public string emotion;
        public Sprite sprite;
    }

    /// <summary>
    /// A named character portrait with emotion variants.
    /// </summary>
    [Serializable]
    public class CharacterPortrait
    {
        public string characterName;
        public EmotionSprite[] emotions;

        public Sprite GetSprite(string emotion)
        {
            foreach (var e in emotions)
                if (e.emotion == emotion) return e.sprite;
            return emotions.Length > 0 ? emotions[0].sprite : null;
        }
    }
}
