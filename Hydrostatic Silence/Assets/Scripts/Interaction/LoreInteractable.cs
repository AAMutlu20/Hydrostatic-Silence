using UnityEngine;
using UnityEngine.Events;

// ReSharper disable InconsistentNaming

namespace Interaction
{
    /// <summary>
    /// Attach to any object the player can examine (notebooks, monitors, manifests, etc.).
    /// The PlayerInteraction script raycasts and calls Interact() on whatever it hits.
    /// </summary>
    public class LoreInteractable : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private string interactPrompt = "Examine";

        [Header("Narrative")]
        [Tooltip("Beat ID to jump to when examined. NarrativeManager will play this beat.")]
        [SerializeField] private string targetBeatID;

        [Tooltip("Beat ID to return to after this lore is viewed.")]
        [SerializeField] private string returnBeatID;

        [Header("State")]
        [Tooltip("Can only be examined once per playthrough.")]
        [SerializeField] private bool singleUse = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onInteract;
        [SerializeField] private UnityEvent onHighlight;
        [SerializeField] private UnityEvent onUnhighlight;

        private bool _used;
        private Renderer _renderer;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private Color _originalEmission;
        private bool _hasEmission;

        public string InteractPrompt => interactPrompt;
        public bool IsUsed => _used;
        public string TargetBeatID => targetBeatID;
        public string ReturnBeatID => returnBeatID;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            if (!_renderer || !_renderer.material.HasProperty(EmissionColor)) return;
            _hasEmission = true;
            _originalEmission = _renderer.material.GetColor(EmissionColor);
        }

        /// <summary>
        /// Called by PlayerInteraction when the player presses the interact key.
        /// </summary>
        public void Interact()
        {
            if (singleUse && _used) return;

            _used = true;
            onInteract?.Invoke();
        }

        /// <summary>
        /// Called when the player's raycast starts hitting this object.
        /// Adds a subtle highlight.
        /// </summary>
        public void Highlight()
        {
            onHighlight?.Invoke();

            if (_hasEmission && _renderer)
            {
                _renderer.material.SetColor(EmissionColor,
                    _originalEmission + new Color(0.05f, 0.05f, 0.08f));
            }
        }

        /// <summary>
        /// Called when the player's raycast stops hitting this object.
        /// </summary>
        public void Unhighlight()
        {
            onUnhighlight?.Invoke();

            if (_hasEmission && _renderer)
            {
                _renderer.material.SetColor(EmissionColor, _originalEmission);
            }
        }
    }
}