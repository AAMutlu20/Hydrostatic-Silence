using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core
{
    /// <summary>
    /// Manages transitions between game scenes.
    /// Handles fade to black, narration during black, and fade into next scene.
    /// Persists across scene loads via DontDestroyOnLoad.
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        [Header("Fade")]
        [SerializeField] private Image fadeOverlay;
        [SerializeField] private float fadeDuration = 1.5f;

        [Header("Transition Narration")]
        [SerializeField] private TextMeshProUGUI narrationText;
        [SerializeField] private CanvasGroup narrationGroup;
        [SerializeField] private AudioSource narrationAudio;

        [Header("References")]
        [SerializeField] private GameState gameState;

        // ReSharper disable once MemberCanBePrivate.Global
        public static SceneTransitionManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (fadeOverlay)
                fadeOverlay.color = new Color(0, 0, 0, 1);
        }

        private void Start()
        {
            // Fade in on first scene
            StartCoroutine(FadeFromBlack());
        }

        /// <summary>
        /// Transition to next scene with narration during the black.
        /// </summary>
        /// <param name="sceneName">Unity scene to load.</param>
        /// <param name="narration">Text to show during the transition. Null to skip.</param>
        /// <param name="audioClip">VO clip to play during transition. Null to skip.</param>
        /// <param name="holdDuration">How long to hold on narration before loading next scene.</param>
        public void TransitionTo(string sceneName, string narration = null,
            AudioClip audioClip = null, float holdDuration = 4f)
        {
            StartCoroutine(TransitionSequence(sceneName, narration, audioClip, holdDuration));
        }

        /// <summary>
        /// Transition with conditional narration based on GameState.
        /// Pass all variants and the method picks the right one.
        /// </summary>
        public void TransitionWithVariant(string sceneName,
            string aliveNarration, string scientificNarration,
            string engineeringNarration, string neutralNarration,
            AudioClip aliveClip = null, AudioClip scientificClip = null,
            AudioClip engineeringClip = null, AudioClip neutralClip = null,
            float holdDuration = 5f)
        {
            string narration;
            AudioClip clip;

            switch (gameState.framework)
            {
                case "alive":
                    narration = aliveNarration;
                    clip = aliveClip;
                    break;
                case "scientific":
                    narration = scientificNarration;
                    clip = scientificClip;
                    break;
                case "engineering":
                    narration = engineeringNarration;
                    clip = engineeringClip;
                    break;
                default:
                    narration = neutralNarration;
                    clip = neutralClip;
                    break;
            }

            TransitionTo(sceneName, narration, clip, holdDuration);
        }

        private IEnumerator TransitionSequence(string sceneName, string narration,
            AudioClip audioClip, float holdDuration)
        {
            // Fade to black
            yield return FadeToBlack();

            // Show narration if provided
            if (!string.IsNullOrEmpty(narration) && narrationText != null)
            {
                narrationText.text = narration;
                yield return FadeCanvasGroup(narrationGroup, 0f, 1f, 0.8f);

                // Play VO
                if (audioClip != null && narrationAudio != null)
                {
                    narrationAudio.clip = audioClip;
                    narrationAudio.Play();
                    // Wait for VO to finish or holdDuration, whichever is longer
                    yield return new WaitForSeconds(Mathf.Max(holdDuration, audioClip.length + 0.5f));
                }
                else
                {
                    yield return new WaitForSeconds(holdDuration);
                }

                yield return FadeCanvasGroup(narrationGroup, 1f, 0f, 0.8f);
            }

            // Load the next scene
            var asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (asyncLoad is { isDone: false })
                yield return null;

            // Fade from black into new scene
            yield return FadeFromBlack();
        }

        private IEnumerator FadeToBlack()
        {
            if (fadeOverlay == null) yield break;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Clamp01(elapsed / fadeDuration);
                fadeOverlay.color = new Color(0, 0, 0, a);
                yield return null;
            }

            fadeOverlay.color = new Color(0, 0, 0, 1);
        }

        private IEnumerator FadeFromBlack()
        {
            if (fadeOverlay == null) yield break;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float a = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                fadeOverlay.color = new Color(0, 0, 0, a);
                yield return null;
            }

            fadeOverlay.color = new Color(0, 0, 0, 0);
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) yield break;

            group.gameObject.SetActive(true);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            group.alpha = to;
            if (to <= 0f) group.gameObject.SetActive(false);
        }
    }
}