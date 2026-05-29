using System;
using System.Collections.Generic;
using DialogueSystem.Scripts.Data.Supporting.Classes;
using UnityEngine;

namespace DialogueSystem.Scripts.Runtime.Events
{
    /// <summary>
    /// Central event bus for the dialogue system.
    /// The UI layer listens to these. The Runner fires them.
    /// Nothing talks directly to the Runner or UI from outside.
    /// </summary>
    public class DialogueEvents
    {
        // ===== DIALOGUE LIFECYCLE =====
        public event Action OnDialogueStarted;
        public event Action OnDialogueEnded;

        // ===== LINE EVENTS =====
        public event Action<string, string, Sprite, string> OnDialogueLine;
        // params: text, speakerName, portrait, emotion

        public event Action<float> OnTypewriterStarted;   // speed
        public event Action        OnTypewriterFinished;
        public event Action        OnTypewriterSkipped;

        // ===== CHOICE EVENTS =====
        public event Action<List<ChoiceOption>> OnChoicesPresented;
        public event Action                     OnChoicesHidden;
        public event Action<int>                OnChoiceSelected;

        // ===== CUSTOM EVENTS =====
        public event Action<string, string[]> OnCustomEvent;  // eventName, params

        // ===== AUDIO =====
        public event Action<AudioClip> OnVoiceClipRequested;

        // ===== FIRE METHODS (called by DialogueRunner) =====

        public void FireDialogueStarted()                                   => OnDialogueStarted?.Invoke();
        public void FireDialogueEnded()                                     => OnDialogueEnded?.Invoke();

        public void FireDialogueLine(string text, string speaker, Sprite portrait, string emotion)
            => OnDialogueLine?.Invoke(text, speaker, portrait, emotion);

        public void FireTypewriterStarted(float speed)                      => OnTypewriterStarted?.Invoke(speed);
        public void FireTypewriterFinished()                                => OnTypewriterFinished?.Invoke();
        public void FireTypewriterSkipped()                                 => OnTypewriterSkipped?.Invoke();

        public void FireChoicesPresented(List<ChoiceOption> choices)        => OnChoicesPresented?.Invoke(choices);
        public void FireChoicesHidden()                                     => OnChoicesHidden?.Invoke();
        public void FireChoiceSelected(int index)                           => OnChoiceSelected?.Invoke(index);

        public void FireCustomEvent(string eventName, string[] parameters)  => OnCustomEvent?.Invoke(eventName, parameters);
        public void FireVoiceClipRequested(AudioClip clip)                  => OnVoiceClipRequested?.Invoke(clip);
    }
}
