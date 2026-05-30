using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// ReSharper disable InconsistentNaming

namespace UI
{
    /// <summary>
    /// Handles all narrative text display and choice presentation.
    /// Attach to a Canvas with the required child elements.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("Text Display")]
        [SerializeField] private CanvasGroup textPanel;
        [SerializeField] private TextMeshProUGUI speakerLabel;
        [SerializeField] private TextMeshProUGUI bodyText;
        [SerializeField] private TextMeshProUGUI advancePrompt;

        [Header("Choice Display")]
        [SerializeField] private CanvasGroup choicePanel;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private TextMeshProUGUI[] choiceLabels;

        [Header("Settings")]
        [SerializeField] private float typewriterSpeed = 0.03f;
        [SerializeField] private float fadeSpeed = 2f;
        [SerializeField] private bool useTypewriter = true;

        /// <summary>Player clicked/pressed to advance non-choice text.</summary>
        public event Action OnAdvance;

        /// <summary>Player selected a choice. Index matches the choices array.</summary>
        public event Action<int> OnChoiceSelected;

        private bool _typing;
        private bool _canAdvance;
        private Coroutine _typewriterCoroutine;
        private string _fullText;

        private void Awake()
        {
            // Wire up choice buttons
            for (var i = 0; i < choiceButtons.Length; i++)
            {
                var index = i; // capture for closure
                choiceButtons[i].onClick.AddListener(() => SelectChoice(index));
            }

            HideAll();
        }

        private void Update()
        {
            // Click or space to advance
            if (!_canAdvance || (!Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Space))) return;
            if (_typing)
            {
                // Skip typewriter, show full text
                FinishTyping();
            }
            else
            {
                _canAdvance = false;
                OnAdvance?.Invoke();
            }
        }

        /// <summary>
        /// Display narrative text with optional speaker name.
        /// </summary>
        public void ShowText(string speaker, string text)
        {
            HideAll();

            speakerLabel.text = string.IsNullOrEmpty(speaker) ? "" : speaker;
            speakerLabel.gameObject.SetActive(!string.IsNullOrEmpty(speaker));

            if (advancePrompt)
                advancePrompt.gameObject.SetActive(false);

            _fullText = text;

            if (useTypewriter)
            {
                bodyText.text = "";
                _typing = true;
                _typewriterCoroutine = StartCoroutine(Typewriter(text));
            }
            else
            {
                bodyText.text = text;
                _typing = false;
            }

            _canAdvance = true;
            StartCoroutine(FadeIn(textPanel));
        }

        /// <summary>
        /// Display a choice with context text and option buttons.
        /// </summary>
        public void ShowChoice(string speaker, string contextText, string[] options)
        {
            HideAll();

            // Context text above choices
            speakerLabel.text = string.IsNullOrEmpty(speaker) ? "" : speaker;
            speakerLabel.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
            bodyText.text = contextText ?? "";

            if (advancePrompt)
                advancePrompt.gameObject.SetActive(false);

            // Set up choice buttons
            for (var i = 0; i < choiceButtons.Length; i++)
            {
                if (i < options.Length)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    choiceLabels[i].text = options[i];
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }

            _canAdvance = false; // Can't click-advance on choices
            StartCoroutine(FadeIn(textPanel));
            StartCoroutine(FadeIn(choicePanel));
        }

        public void HideAll()
        {
            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);

            _typing = false;
            _canAdvance = false;
            textPanel.alpha = 0f;
            textPanel.gameObject.SetActive(false);
            choicePanel.alpha = 0f;
            choicePanel.gameObject.SetActive(false);
        }

        private void SelectChoice(int index)
        {
            StartCoroutine(FadeOutThenChoice(index));
        }

        private IEnumerator FadeOutThenChoice(int index)
        {
            yield return FadeOut(choicePanel);
            OnChoiceSelected?.Invoke(index);
        }

        private void FinishTyping()
        {
            if (_typewriterCoroutine != null)
                StopCoroutine(_typewriterCoroutine);

            bodyText.text = _fullText;
            _typing = false;

            if (advancePrompt)
                advancePrompt.gameObject.SetActive(true);
        }

        private IEnumerator Typewriter(string text)
        {
            bodyText.text = "";
            foreach (var c in text)
            {
                bodyText.text += c;
                yield return new WaitForSeconds(typewriterSpeed);
            }

            _typing = false;

            if (advancePrompt)
                advancePrompt.gameObject.SetActive(true);
        }

        private IEnumerator FadeIn(CanvasGroup group)
        {
            group.gameObject.SetActive(true);
            while (group.alpha < 1f)
            {
                group.alpha += Time.deltaTime * fadeSpeed;
                yield return null;
            }
            group.alpha = 1f;
        }

        private IEnumerator FadeOut(CanvasGroup group)
        {
            while (group.alpha > 0f)
            {
                group.alpha -= Time.deltaTime * fadeSpeed;
                yield return null;
            }
            group.alpha = 0f;
            group.gameObject.SetActive(false);
        }
    }
}