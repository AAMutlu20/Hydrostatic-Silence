using Core;
using TMPro;
using UnityEngine;
// ReSharper disable InconsistentNaming

namespace Interaction
{
    /// <summary>
    /// First-person interaction system. Raycasts from the camera center,
    /// highlights interactable objects, and triggers them on key press.
    /// Attach to the player camera.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] private float interactRange = 3f;
        [SerializeField] private LayerMask interactLayer;

        [Header("UI")]
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private CanvasGroup promptGroup;

        [Header("References")]
        [SerializeField] private NarrativeManager narrativeManager;

        [Header("Input")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private LoreInteractable _currentTarget;
        private Camera _cam;
        private bool _interactionEnabled = true;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (!_cam) _cam = Camera.main;

            if (promptGroup)
                promptGroup.alpha = 0f;
        }

        private void Update()
        {
            if (!_interactionEnabled) return;

            var ray = new Ray(_cam.transform.position, _cam.transform.forward);

            if (Physics.Raycast(ray, out var hit, interactRange, interactLayer))
            {
                var interactable = hit.collider.GetComponent<LoreInteractable>();

                if (interactable && !interactable.IsUsed)
                {
                    if (_currentTarget != interactable)
                    {
                        _currentTarget?.Unhighlight();
                        _currentTarget = interactable;
                        _currentTarget.Highlight();
                        ShowPrompt(_currentTarget.InteractPrompt);
                    }

                    if (!Input.GetKeyDown(interactKey)) return;
                    _currentTarget.Interact();

                    // Jump to the lore beat in the narrative
                    if (narrativeManager &&
                        !string.IsNullOrEmpty(_currentTarget.TargetBeatID))
                    {
                        narrativeManager.PlayBeat(_currentTarget.TargetBeatID);
                    }

                    HidePrompt();
                }
                else
                {
                    ClearTarget();
                }
            }
            else
            {
                ClearTarget();
            }
        }

        private void ClearTarget()
        {
            if (!_currentTarget) return;
            _currentTarget.Unhighlight();
            _currentTarget = null;
            HidePrompt();
        }

        private void ShowPrompt(string text)
        {
            if (promptText)
                promptText.text = $"[{interactKey}] {text}";
            if (promptGroup)
                promptGroup.alpha = 1f;
        }

        private void HidePrompt()
        {
            if (promptGroup)
                promptGroup.alpha = 0f;
        }

        /// <summary>Disable interaction during dialogue/choices.</summary>
        // ReSharper disable once ParameterHidesMember
        public void SetEnabled(bool enabled)
        {
            _interactionEnabled = enabled;
            if (!enabled) ClearTarget();
        }
    }
}