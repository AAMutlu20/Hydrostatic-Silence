using System;
using System.Collections;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace Core
{
    /// <summary>
    /// Drives the narrative forward. Reads beats from the active NarrativeSequence,
    /// pushes text/choices to DialogueUI, and writes player decisions to GameState.
    /// One instance lives in each scene.
    /// </summary>
    public class NarrativeManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private GameState gameState;
        [SerializeField] private NarrativeSequence sequence;

        [Header("References")]
        [SerializeField] private UI.DialogueUI dialogueUI;

        /// <summary>Fired when a beat is displayed. Hook horror events here.</summary>
        public event Action<string> OnBeatStarted;

        /// <summary>Fired when the sequence has no more beats to show.</summary>
        public event Action OnSequenceComplete;

        private NarrativeBeat _currentBeat;
        private bool _waitingForInput;

        private void Start()
        {
            if (sequence == null || dialogueUI == null || gameState == null)
            {
                Debug.LogError("[NarrativeManager] Missing references. Assign GameState, Sequence, and DialogueUI.");
                return;
            }

            dialogueUI.OnAdvance += HandleAdvance;
            dialogueUI.OnChoiceSelected += HandleChoice;

            PlayBeat(sequence.startBeatID);
        }

        private void OnDestroy()
        {
            if (dialogueUI != null)
            {
                dialogueUI.OnAdvance -= HandleAdvance;
                dialogueUI.OnChoiceSelected -= HandleChoice;
            }
        }

        /// <summary>
        /// Jump to a specific beat by ID. Used for lore returns, branching, etc.
        /// </summary>
        public void PlayBeat(string beatID)
        {
            if (string.IsNullOrEmpty(beatID))
            {
                OnSequenceComplete?.Invoke();
                return;
            }

            var beat = sequence.GetBeat(beatID);
            if (beat == null)
            {
                Debug.LogWarning($"[NarrativeManager] Beat '{beatID}' not found in sequence.");
                OnSequenceComplete?.Invoke();
                return;
            }

            _currentBeat = beat;
            OnBeatStarted?.Invoke(beatID);

            // Fire the beat's Unity event (lighting changes, sound cues, etc.)
            beat.onShow?.Invoke();

            if (beat.isChoice && beat.choices is { Length: > 0 })
            {
                // Show choices
                var labels = new string[beat.choices.Length];
                for (int i = 0; i < beat.choices.Length; i++)
                    labels[i] = beat.choices[i].label;

                dialogueUI.ShowChoice(beat.speaker, beat.text, labels);
                _waitingForInput = true;
            }
            else
            {
                // Show narrative text
                dialogueUI.ShowText(beat.speaker, beat.text);
                _waitingForInput = true;

                if (beat.autoAdvanceDelay > 0)
                    StartCoroutine(AutoAdvance(beat.autoAdvanceDelay));
            }
        }

        private IEnumerator AutoAdvance(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (_waitingForInput)
                HandleAdvance();
        }

        private void HandleAdvance()
        {
            if (!_waitingForInput || _currentBeat == null) return;
            if (_currentBeat.isChoice) return; // Can't click-advance on a choice

            _waitingForInput = false;
            PlayBeat(_currentBeat.nextBeatID);
        }

        private void HandleChoice(int index)
        {
            if (!_waitingForInput || _currentBeat == null) return;
            if (!_currentBeat.isChoice) return;
            if (index < 0 || index >= _currentBeat.choices.Length) return;

            _waitingForInput = false;
            var choice = _currentBeat.choices[index];

            // Set the variable if specified
            if (!string.IsNullOrEmpty(choice.setVariable))
                ApplyVariable(choice.setVariable);

            // Fire the choice's Unity event
            choice.onChosen?.Invoke();

            // Advance to the target beat
            PlayBeat(choice.targetBeatID);
        }

        /// <summary>
        /// Parses "variableName=value" and writes it to GameState.
        /// </summary>
        private void ApplyVariable(string assignment)
        {
            var parts = assignment.Split('=');
            if (parts.Length != 2)
            {
                Debug.LogWarning($"[NarrativeManager] Bad variable format: '{assignment}'. Expected 'name=value'.");
                return;
            }

            var field = parts[0].Trim();
            var value = parts[1].Trim();

            switch (field)
            {
                case "framework":        gameState.framework = value; break;
                case "veraMarenTone":    gameState.veraMarenTone = value; break;
                case "cavallApproach":   gameState.cavallApproach = value; break;
                case "veraAdmission":    gameState.veraAdmission = value; break;
                case "drevResponse":     gameState.drevResponse = value; break;
                case "drevWake":         gameState.drevWake = value; break;

                // Bool lore flags
                case "loreNotebook":     gameState.loreNotebook = true; break;
                case "lorePhoto":        gameState.lorePhoto = true; break;
                case "loreDeepfield1":   gameState.loreDeepfield1 = true; break;
                case "loreManifest":     gameState.loreManifest = true; break;
                case "loreCharter":      gameState.loreCharter = true; break;
                case "loreDrevLog":      gameState.loreDrevLog = true; break;
                case "loreDeepfield2":
                    // Only set if deepfield1 was found — the cross-scene gate
                    if (gameState.loreDeepfield1)
                        gameState.loreDeepfield2 = true;
                    break;

                default:
                    Debug.LogWarning($"[NarrativeManager] Unknown variable: '{field}'");
                    break;
            }
        }
    }
}