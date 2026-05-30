using UnityEngine;

namespace Shader
{
    /// <summary>
    /// Controls the void viewport shader parameters based on narrative events.
    /// Can also be used in Scene 2 with lower intensity for subtle unease.
    /// </summary>
    public class VoidViewportController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Renderer viewportRenderer;
        
        [Header("State")]
        [SerializeField, Range(0f, 1f)] private float unease;
        [SerializeField, Range(0f, 1f)] private float appetite;
        [SerializeField] private bool wakeVisible;
        
        [Header("Transition Speeds")]
        [SerializeField] private float uneaseRampSpeed = 0.1f;
        [SerializeField] private float appetiteRampSpeed = 0.05f;
        [SerializeField] private float wakeRevealSpeed = 0.3f;
        
        private Material material;
        private float targetUnease;
        private float targetAppetite;
        private float targetWake;
        
        // Shader property IDs — cached for performance
        private static readonly int PropUnease = UnityEngine.Shader.PropertyToID("_Unease");
        private static readonly int PropAppetite = UnityEngine.Shader.PropertyToID("_Appetite");
        private static readonly int PropWakeActive = UnityEngine.Shader.PropertyToID("_WakeActive");
        private static readonly int PropWakePosition = UnityEngine.Shader.PropertyToID("_WakePosition");
        private static readonly int PropDeepSpeed = UnityEngine.Shader.PropertyToID("_DeepSpeed");
        private static readonly int PropSurfaceSpeed = UnityEngine.Shader.PropertyToID("_SurfaceSpeed");
        private static readonly int PropPulseFrequency = UnityEngine.Shader.PropertyToID("_PulseFrequency");

        private void Awake()
        {
            if (!viewportRenderer)
                viewportRenderer = GetComponent<Renderer>();
                
            // Create material instance
            material = viewportRenderer.material;
        }

        private void Update()
        {
            // Smooth transitions
            unease = Mathf.MoveTowards(unease, targetUnease, uneaseRampSpeed * Time.deltaTime);
            appetite = Mathf.MoveTowards(appetite, targetAppetite, appetiteRampSpeed * Time.deltaTime);
            var wakeVal = material.GetFloat(PropWakeActive);
            wakeVal = Mathf.MoveTowards(wakeVal, targetWake, wakeRevealSpeed * Time.deltaTime);
            
            material.SetFloat(PropUnease, unease);
            material.SetFloat(PropAppetite, appetite);
            material.SetFloat(PropWakeActive, wakeVal);
        }

        // === PUBLIC API — Call from narrative/dialogue system ===
        
        /// <summary>
        /// Scene 2: Viewport shows subtle unease. The void is there but quiet.
        /// </summary>
        public void SetScene2Idle()
        {
            targetUnease = 0.1f;
            targetAppetite = 0f;
            targetWake = 0f;
        }

        /// <summary>
        /// Scene 3 entry: The medium is aware. Subtle activity.
        /// Call when the scene starts (14:17).
        /// </summary>
        public void OnBoundaryEntry()
        {
            targetUnease = 0.2f;
            targetAppetite = 0f;
        }

        /// <summary>
        /// Pressure readings go uniform. The medium is paying attention.
        /// Call at 14:32.
        /// </summary>
        public void OnPressureUniform()
        {
            targetUnease = 0.4f;
            targetAppetite = 0.1f;
            // Pulse frequency increases slightly
            material.SetFloat(PropPulseFrequency, 0.25f);
        }

        /// <summary>
        /// Shear current demonstration. The void shows its teeth briefly.
        /// Call when the shear current passes.
        /// </summary>
        public void OnShearDemonstration()
        {
            // Brief spike — unease jumps then settles
            StartCoroutine(ShearPulse());
        }

        private System.Collections.IEnumerator ShearPulse()
        {
            float originalUnease = targetUnease;
            targetUnease = 0.7f;
            uneaseRampSpeed = 0.8f; // fast spike
            
            yield return new WaitForSeconds(1.5f);
            
            targetUnease = originalUnease + 0.1f; // settles slightly higher than before
            uneaseRampSpeed = 0.1f; // slow settle
        }

        /// <summary>
        /// Thrust applied. The medium reacts before the engines fire.
        /// Call at the thrust moment.
        /// </summary>
        public void OnThrust()
        {
            targetUnease = 0.5f;
            targetAppetite = 0.2f;
            material.SetFloat(PropDeepSpeed, 0.02f);
            material.SetFloat(PropSurfaceSpeed, 0.08f);
        }

        /// <summary>
        /// Wake entity detected at 14:47. Something is coming up from below.
        /// Call when the deep-field registers the displacement.
        /// </summary>
        public void OnWakeDetected()
        {
            targetUnease = 0.7f;
            targetAppetite = 0.4f;
            targetWake = 1f;
            
            // Set wake entry position — comes from below
            material.SetVector(PropWakePosition, new Vector4(0.5f, 0.9f, 0f, 0f));
            
            material.SetFloat(PropPulseFrequency, 0.4f);
        }

        /// <summary>
        /// Multiple entities converging. 15:01.
        /// </summary>
        public void OnEntitiesConverge()
        {
            targetUnease = 0.85f;
            targetAppetite = 0.6f;
            
            material.SetFloat(PropDeepSpeed, 0.04f);
            material.SetFloat(PropSurfaceSpeed, 0.15f);
            material.SetFloat(PropPulseFrequency, 0.7f);
        }

        /// <summary>
        /// First hull breach. 15:02. The ship is being taken apart.
        /// </summary>
        public void OnFirstBreach()
        {
            targetUnease = 0.95f;
            targetAppetite = 0.8f;
        }

        /// <summary>
        /// Solenne's transmission. Full horror.
        /// </summary>
        public void OnTransmission()
        {
            targetUnease = 1f;
            targetAppetite = 1f;
            material.SetFloat(PropPulseFrequency, 1.2f);
            material.SetFloat(PropDeepSpeed, 0.06f);
            material.SetFloat(PropSurfaceSpeed, 0.2f);
        }

        /// <summary>
        /// Lights going out. The void is all that remains.
        /// Increase intensity so the viewport becomes the last visible thing.
        /// </summary>
        public void OnLightsOut()
        {
            // The void gets very slightly brighter as everything else dies
            // Making it the last thing the player sees
            StartCoroutine(FinalReveal());
        }

        private System.Collections.IEnumerator FinalReveal()
        {
            var elapsed = 0f;
            const float duration = 5f;
            var startIntensity = material.GetFloat(UnityEngine.Shader.PropertyToID("_Intensity"));
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;
                
                // Viewport gets slightly brighter as bridge lights die
                // Then fades to black with everything else
                var curve = Mathf.Sin(t * Mathf.PI); // rises then falls
                material.SetFloat(UnityEngine.Shader.PropertyToID("_Intensity"),
                    startIntensity + curve * 0.15f);
                
                yield return null;
            }
            
            // Final: everything dark
            material.SetFloat(UnityEngine.Shader.PropertyToID("_Intensity"), 0f);
        }

        /// <summary>
        /// Call during dialogue choice moments.
        /// The medium pays attention when the player is deciding.
        /// </summary>
        public void OnChoicePresented()
        {
            targetAppetite = Mathf.Min(targetAppetite + 0.15f, 1f);
        }

        /// <summary>
        /// Call when the player selects a choice.
        /// The medium's attention settles.
        /// </summary>
        public void OnChoiceMade()
        {
            targetAppetite = Mathf.Max(targetAppetite - 0.15f, 0f);
        }

        private void OnDestroy()
        {
            // Clean up instanced material
            if (material)
                Destroy(material);
        }
    }
}