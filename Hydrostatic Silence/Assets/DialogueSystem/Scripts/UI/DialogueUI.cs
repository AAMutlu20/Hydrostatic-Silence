using System.Collections;
using System.Collections.Generic;
using DialogueSystem.Scripts.Data.Supporting.Classes;
using DialogueSystem.Scripts.Runtime.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DialogueSystem.Scripts.UI
{
    // =====================================================================
    // TYPEWRITER EFFECT
    // =====================================================================

    /// <summary>
    /// Reveals TMP text character by character at a given speed.
    /// Attach to your dialogue text object.
    /// </summary>
    public class TypewriterEffect : MonoBehaviour
    {
        [SerializeField] private TMP_Text targetText;

        private Coroutine typewriterCoroutine;

        public bool IsPlaying { get; private set; }

        public void Play(string fullText, float charsPerSecond)
        {
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            targetText.text = fullText;
            targetText.maxVisibleCharacters = 0;
            IsPlaying = true;

            if (charsPerSecond <= 0f)
            {
                targetText.maxVisibleCharacters = fullText.Length;
                IsPlaying = false;
                DialogueManager.Instance?.Events.FireTypewriterFinished();
                return;
            }

            typewriterCoroutine = StartCoroutine(TypewriterCoroutine(fullText, charsPerSecond));
        }

        public void Skip()
        {
            if (!IsPlaying) return;
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            targetText.maxVisibleCharacters = targetText.text.Length;
            IsPlaying = false;
            DialogueManager.Instance?.Events.FireTypewriterFinished();
        }

        private IEnumerator TypewriterCoroutine(string text, float charsPerSecond)
        {
            float delay = 1f / charsPerSecond;
            int visible = 0;

            while (visible < text.Length)
            {
                visible++;
                targetText.maxVisibleCharacters = visible;
                yield return new WaitForSeconds(delay);
            }

            IsPlaying = false;
            DialogueManager.Instance?.Events.FireTypewriterFinished();
        }
    }

    // =====================================================================
    // CHOICE BUTTON
    // =====================================================================

    /// <summary>
    /// A single choice button in the choices panel.
    /// Calls DialogueManager.SelectChoice on click.
    /// </summary>
    public class ChoiceButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private int choiceIndex;

        public void Setup(ChoiceOption option, int index)
        {
            choiceIndex = index;
            label.text  = option.text;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            DialogueManager.Instance?.SelectChoice(choiceIndex);
        }
    }

    // =====================================================================
    // DIALOGUE UI
    // =====================================================================

    /// <summary>
    /// Main UI controller. Subscribes to DialogueEvents and drives the panels.
    /// Assign all references in the Inspector.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private GameObject choicePanel;

        [Header("Dialogue")]
        [SerializeField] private TMP_Text   speakerNameText;
        [SerializeField] private TMP_Text   dialogueText;
        [SerializeField] private Image      portraitImage;
        [SerializeField] private GameObject portraitContainer;
        [SerializeField] private Button     continueButton;

        [Header("Choices")]
        [SerializeField] private Transform  choiceContainer;
        [SerializeField] private GameObject choiceButtonPrefab;

        [Header("Typewriter")]
        [SerializeField] private TypewriterEffect typewriter;

        [Header("Audio")]
        [SerializeField] private AudioSource voiceAudioSource;

        // ===== RUNTIME =====
        private readonly List<ChoiceButton> spawnedButtons = new List<ChoiceButton>();

        // ===== LIFECYCLE =====

        private void OnEnable()
        {
            if (DialogueManager.Instance == null) return;
            var ev = DialogueManager.Instance.Events;

            ev.OnDialogueStarted     += OnDialogueStarted;
            ev.OnDialogueEnded       += OnDialogueEnded;
            ev.OnDialogueLine        += OnDialogueLine;
            ev.OnTypewriterFinished  += OnTypewriterFinished;
            ev.OnTypewriterSkipped   += OnTypewriterSkipped;
            ev.OnChoicesPresented    += OnChoicesPresented;
            ev.OnChoicesHidden       += OnChoicesHidden;
            ev.OnVoiceClipRequested  += OnVoiceClip;
        }

        private void OnDisable()
        {
            if (DialogueManager.Instance == null) return;
            var ev = DialogueManager.Instance.Events;

            ev.OnDialogueStarted     -= OnDialogueStarted;
            ev.OnDialogueEnded       -= OnDialogueEnded;
            ev.OnDialogueLine        -= OnDialogueLine;
            ev.OnTypewriterFinished  -= OnTypewriterFinished;
            ev.OnTypewriterSkipped   -= OnTypewriterSkipped;
            ev.OnChoicesPresented    -= OnChoicesPresented;
            ev.OnChoicesHidden       -= OnChoicesHidden;
            ev.OnVoiceClipRequested  -= OnVoiceClip;
        }

        private void Start()
        {
            dialoguePanel.SetActive(false);
            choicePanel.SetActive(false);

            continueButton.onClick.AddListener(OnContinueClicked);
        }

        // ===== EVENT HANDLERS =====

        private void OnDialogueStarted()
        {
            dialoguePanel.SetActive(true);
            choicePanel.SetActive(false);
        }

        private void OnDialogueEnded()
        {
            dialoguePanel.SetActive(false);
            choicePanel.SetActive(false);
        }

        private void OnDialogueLine(string text, string speaker, UnityEngine.Sprite portrait, string emotion)
        {
            speakerNameText.text = speaker;
            continueButton.gameObject.SetActive(false);

            if (portrait != null)
            {
                portraitImage.sprite = portrait;
                portraitContainer.SetActive(true);
            }
            else
            {
                portraitContainer.SetActive(false);
            }

            // Delegate typewriter to the TypewriterEffect component
            // TypewriterEffect will call Events.FireTypewriterFinished when done
            typewriter.Play(text, 40f);
        }

        private void OnTypewriterFinished()
        {
            continueButton.gameObject.SetActive(true);
        }

        private void OnTypewriterSkipped()
        {
            typewriter.Skip();
        }

        private void OnChoicesPresented(List<ChoiceOption> choices)
        {
            choicePanel.SetActive(true);
            continueButton.gameObject.SetActive(false);

            // Clear old buttons
            foreach (var b in spawnedButtons)
                if (b != null) Destroy(b.gameObject);
            spawnedButtons.Clear();

            // Spawn new
            for (int i = 0; i < choices.Count; i++)
            {
                var go     = Instantiate(choiceButtonPrefab, choiceContainer);
                var btn    = go.GetComponent<ChoiceButton>();
                btn.Setup(choices[i], i);
                spawnedButtons.Add(btn);
            }
        }

        private void OnChoicesHidden()
        {
            choicePanel.SetActive(false);
        }

        private void OnVoiceClip(UnityEngine.AudioClip clip)
        {
            if (voiceAudioSource == null) return;
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
        }

        // ===== INPUT =====

        private void OnContinueClicked()
        {
            if (typewriter.IsPlaying)
                DialogueManager.Instance?.Continue();   // first press skips typewriter
            else
                DialogueManager.Instance?.Continue();   // second press advances
        }

        private void Update()
        {
            // Keyboard / controller shortcut
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
                    DialogueManager.Instance.Continue();
            }
        }
    }
}
