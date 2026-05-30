using UnityEngine;

// ReSharper disable InconsistentNaming

namespace Interaction
{
    /// <summary>
    /// First-person look only — no walking. The player is rooted in place
    /// and can look around within constrained angles.
    /// Attach to the player GameObject. Camera should be a child.
    /// </summary>
    public class FirstPersonLook : MonoBehaviour
    {
        [Header("Sensitivity")]
        [SerializeField] private float mouseSensitivity = 2f;

        [Header("Constraints")]
        [SerializeField] private float maxYaw = 90f;
        [SerializeField] private float maxPitch = 60f;
        [SerializeField] private float minPitch = -40f;

        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        private float _yaw;
        private float _pitch;
        private bool _enabled = true;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (cameraTransform == null)
                cameraTransform = GetComponentInChildren<Camera>()?.transform;
        }

        private void Update()
        {
            if (!_enabled) return;

            var mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            var mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            _yaw = Mathf.Clamp(_yaw + mouseX, -maxYaw, maxYaw);
            _pitch = Mathf.Clamp(_pitch - mouseY, minPitch, maxPitch);

            transform.localRotation = Quaternion.Euler(0f, _yaw, 0f);

            if (cameraTransform)
                cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        /// <summary>Disable look during transitions or menus.</summary>
        // ReSharper disable once ParameterHidesMember
        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !enabled;
        }

        /// <summary>Snap to a specific direction (for scene starts).</summary>
        public void SetLookDirection(float yaw, float pitch)
        {
            _yaw = yaw;
            _pitch = pitch;
        }
    }
}