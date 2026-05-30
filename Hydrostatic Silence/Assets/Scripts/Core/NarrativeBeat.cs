using System;
using UnityEngine;
using UnityEngine.Events;

namespace Core
{
    /// <summary>
    /// A single beat in the narrative — text displayed, choices offered, events triggered.
    /// These are authored in the Inspector on NarrativeSequence assets.
    /// </summary>
    [Serializable]
    public class NarrativeBeat
    {
        [Tooltip("Unique ID for this beat (e.g. S1_ARRIVE, S1_CHOICE_1)")]
        public string beatID;

        [Tooltip("Who is speaking. Empty = narration / internal monologue.")]
        public string speaker;

        [TextArea(3, 10)]
        [Tooltip("The text displayed to the player.")]
        public string text;

        [Tooltip("If true, this beat presents choices. Fill the choices array.")]
        public bool isChoice;

        [Tooltip("Available choices. Only used if isChoice is true.")]
        public Choice[] choices;

        [Tooltip("Fired when this beat is displayed. Use for lighting changes, sound cues, etc.")]
        public UnityEvent onShow;

        [Tooltip("Seconds to wait before auto-advancing. 0 = wait for click/choice.")]
        public float autoAdvanceDelay;

        [Tooltip("Next beat ID if this isn't a choice. Ignored if isChoice is true.")]
        public string nextBeatID;
    }

    [Serializable]
    public class Choice
    {
        [Tooltip("The label shown on the choice button.")]
        public string label;

        [Tooltip("Beat ID to go to when this choice is selected.")]
        public string targetBeatID;

        [Tooltip("Variable to set when chosen. Format: variableName=value (e.g. framework=alive)")]
        public string setVariable;

        [Tooltip("Fired when this choice is selected.")]
        public UnityEvent onChosen;
    }
}