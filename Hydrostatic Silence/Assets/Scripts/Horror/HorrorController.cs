using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
// ReSharper disable InconsistentNaming

namespace Horror
{
    /// <summary>
    /// Controls the atmospheric horror systems: the hum, scene lighting, and post-processing.
    /// One instance per scene. Listens to NarrativeManager beat events to trigger changes.
    /// </summary>
    public class HorrorController : MonoBehaviour
    {
        [Header("The Hum")]
        [SerializeField] private AudioSource humLayer1SubBass;
        [SerializeField] private AudioSource humLayer2Drone;
        [SerializeField] private AudioSource humLayer3Harmonic;
        [SerializeField, Range(0f, 1f)] private float humBaseVolume = 0.05f;
        [SerializeField] private float humRampSpeed = 0.1f;

        [Header("Hull Sound (Scene 3 only)")]
        [SerializeField] private AudioSource hullResonance;
        [SerializeField] private AudioSource structuralStress;

        [Header("Scene Lighting")]
        [SerializeField] private Light[] overheadLights;
        [SerializeField] private Light[] consoleLights;
        [SerializeField] private float lightFadeSpeed = 0.5f;

        [Header("Post-Processing")]
        [SerializeField] private Volume postProcessVolume;
        
        private Vignette _vignette;
        private ChromaticAberration _chromatic;
        private FilmGrain _filmGrain;
        private ColorAdjustments _colorAdjust;

        private float _targetHumVolume;
        private float _targetVignette;
        private float _targetChromatic;
        private float _baseExposure;

        private void Awake()
        {
            _targetHumVolume = humBaseVolume;

            if (postProcessVolume != null && postProcessVolume.profile != null)
            {
                postProcessVolume.profile.TryGet(out _vignette);
                postProcessVolume.profile.TryGet(out _chromatic);
                postProcessVolume.profile.TryGet(out _filmGrain);
                postProcessVolume.profile.TryGet(out _colorAdjust);

                if (_colorAdjust != null)
                    _baseExposure = _colorAdjust.postExposure.value;
            }
        }

        private void Update()
        {
            // Smooth hum volume transitions
            if (humLayer1SubBass != null)
                humLayer1SubBass.volume = Mathf.MoveTowards(
                    humLayer1SubBass.volume, _targetHumVolume, humRampSpeed * Time.deltaTime);

            if (humLayer2Drone != null)
                humLayer2Drone.volume = Mathf.MoveTowards(
                    humLayer2Drone.volume, _targetHumVolume * 0.7f, humRampSpeed * Time.deltaTime);

            if (humLayer3Harmonic != null)
                humLayer3Harmonic.volume = Mathf.MoveTowards(
                    humLayer3Harmonic.volume, _targetHumVolume * 0.4f, humRampSpeed * Time.deltaTime);

            // Smooth post-processing
            if (_vignette != null)
                _vignette.intensity.value = Mathf.MoveTowards(
                    _vignette.intensity.value, _targetVignette, 0.3f * Time.deltaTime);

            if (_chromatic != null)
                _chromatic.intensity.value = Mathf.MoveTowards(
                    _chromatic.intensity.value, _targetChromatic, 0.1f * Time.deltaTime);
        }

        // === PUBLIC API — Call from NarrativeBeat.onShow UnityEvents ===

        /// <summary>Scene 1: Barely audible. Player shouldn't notice.</summary>
        public void SetScene1Atmosphere()
        {
            _targetHumVolume = 0.03f;
            _targetVignette = 0.3f;
            _targetChromatic = 0f;
        }

        /// <summary>Scene 1: V-11 dies. Main monitor goes dark, room gets colder.</summary>
        public void OnV11Destroyed()
        {
            _targetHumVolume = 0.06f;
            _targetVignette = 0.35f;

            // Kill the main monitor light
            if (overheadLights.Length > 0)
                StartCoroutine(FadeLight(overheadLights[0], 0f, 1f));
        }

        /// <summary>
        /// Scene 1: Player examined deep-field data. Hum gets permanently louder.
        /// The player opened a door by looking.
        /// </summary>
        public void OnDeepfieldExamined()
        {
            _targetHumVolume += 0.03f;
        }

        /// <summary>Scene 2: Clinical. Sterile. Too bright.</summary>
        public void SetScene2Atmosphere()
        {
            _targetHumVolume = 0.08f;
            _targetVignette = 0.35f;
            _targetChromatic = 0f;
        }

        /// <summary>Scene 3 entry: The medium is present.</summary>
        public void SetScene3Entry()
        {
            _targetHumVolume = 0.15f;
            _targetVignette = 0.4f;
            _targetChromatic = 0f;

            if (hullResonance != null)
            {
                hullResonance.volume = 0.1f;
                hullResonance.Play();
            }
        }

        /// <summary>Pressure goes uniform. 14:32. Medium is paying attention.</summary>
        public void OnPressureUniform()
        {
            _targetHumVolume = 0.25f;
            _targetVignette = 0.45f;
            _targetChromatic = 0.05f;
        }

        /// <summary>Shear current passes. Brief spike in everything.</summary>
        public void OnShearDemonstration()
        {
            StartCoroutine(ShearPulse());
        }

        private IEnumerator ShearPulse()
        {
            float prevHum = _targetHumVolume;
            float prevVig = _targetVignette;

            _targetHumVolume = 0.5f;
            _targetVignette = 0.55f;
            humRampSpeed = 0.8f;

            if (_filmGrain != null)
                _filmGrain.intensity.value = 0.4f;

            yield return new WaitForSeconds(1.5f);

            _targetHumVolume = prevHum + 0.05f;
            _targetVignette = prevVig + 0.03f;
            humRampSpeed = 0.1f;

            if (_filmGrain != null)
                _filmGrain.intensity.value = 0.1f;
        }

        /// <summary>Thrust applied. Medium reacts before engines fire.</summary>
        public void OnThrust()
        {
            _targetHumVolume = 0.35f;
            _targetVignette = 0.5f;
            _targetChromatic = 0.1f;
        }

        /// <summary>14:47. Wake entity detected. The deep field moves.</summary>
        public void OnWakeDetected()
        {
            _targetHumVolume = 0.5f;
            _targetVignette = 0.55f;
            _targetChromatic = 0.15f;

            // Drop ambient light
            // ReSharper disable once LocalVariableHidesMember
            foreach (var light in overheadLights)
                StartCoroutine(FadeLight(light, light.intensity * 0.7f, 10f));

            // Hull sound shifts
            if (!hullResonance) return;
            hullResonance.pitch = 0.7f;
            hullResonance.volume = 0.3f;
        }

        /// <summary>15:01. More entities converging.</summary>
        public void OnEntitiesConverge()
        {
            _targetHumVolume = 0.65f;
            _targetVignette = 0.6f;
            _targetChromatic = 0.2f;

            if (!structuralStress) return;
            structuralStress.volume = 0.2f;
            structuralStress.Play();
        }

        /// <summary>15:02. First hull breach. Kill an overhead light.</summary>
        public void OnFirstBreach()
        {
            _targetHumVolume = 0.75f;
            _targetChromatic = 0.25f;

            if (overheadLights.Length > 0)
                StartCoroutine(FadeLight(overheadLights[0], 0f, 2f));

            // Brief grain spike
            StartCoroutine(GrainSpike());

            if (structuralStress)
                structuralStress.volume = 0.5f;
        }

        /// <summary>Second breach. Kill another light.</summary>
        public void OnSecondBreach()
        {
            if (overheadLights.Length > 1)
                StartCoroutine(FadeLight(overheadLights[1], 0f, 1.5f));

            StartCoroutine(GrainSpike());
        }

        /// <summary>Solenne takes the comms. Full horror state.</summary>
        public void OnTransmission()
        {
            _targetHumVolume = 0.85f;
            _targetVignette = 0.65f;
            _targetChromatic = 0.3f;

            // Kill remaining overheads
            for (var i = 2; i < overheadLights.Length; i++)
                StartCoroutine(FadeLight(overheadLights[i], 0f, 3f + i * 0.5f));
        }

        /// <summary>
        /// Final sequence. Lights out one by one. Then only the hum.
        /// </summary>
        public void OnLightsOut()
        {
            StartCoroutine(LightsOutSequence());
        }

        private IEnumerator LightsOutSequence()
        {
            _targetHumVolume = 1f;
            _targetVignette = 0.75f;

            // Console lights go out one at a time with different timing
            for (var i = 0; i < consoleLights.Length; i++)
            {
                // Each light has slightly different death
                var fadeDuration = 0.3f + (i * 0.15f);
                var waitBefore = 0.5f + (i * 0.4f);

                yield return new WaitForSeconds(waitBefore);
                StartCoroutine(FadeLight(consoleLights[i], 0f, fadeDuration));
            }

            // After last console light, brief pause, then everything
            yield return new WaitForSeconds(1f);

            // Kill hull sound, structural sound — ONLY hum remains
            if (hullResonance)
                StartCoroutine(FadeAudio(hullResonance, 0f, 1f));
            if (structuralStress)
                StartCoroutine(FadeAudio(structuralStress, 0f, 1f));

            _targetVignette = 1f;

            // Drop exposure to black
            if (!_colorAdjust) yield break;
            var elapsed = 0f;
            while (elapsed < 3f)
            {
                elapsed += Time.deltaTime;
                _colorAdjust.postExposure.value = Mathf.Lerp(_baseExposure, -10f, elapsed / 3f);
                yield return null;
            }
        }

        /// <summary>
        /// Call during choice moments. Hum responds to player deciding.
        /// </summary>
        public void OnChoicePresented()
        {
            _targetHumVolume = Mathf.Min(_targetHumVolume + 0.08f, 1f);
        }

        public void OnChoiceMade()
        {
            _targetHumVolume = Mathf.Max(_targetHumVolume - 0.08f, humBaseVolume);
        }

        // === UTILITIES ===

        private IEnumerator FadeLight(Light target, float endIntensity, float duration)
        {
            if (!target) yield break;
            var start = target.intensity;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                target.intensity = Mathf.Lerp(start, endIntensity, elapsed / duration);
                yield return null;
            }

            target.intensity = endIntensity;
        }

        private IEnumerator FadeAudio(AudioSource source, float endVolume, float duration)
        {
            if (!source) yield break;
            var start = source.volume;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(start, endVolume, elapsed / duration);
                yield return null;
            }

            source.volume = endVolume;
        }

        private IEnumerator GrainSpike()
        {
            if (!_filmGrain) yield break;
            _filmGrain.intensity.value = 0.5f;
            yield return new WaitForSeconds(0.3f);
            _filmGrain.intensity.value = 0.1f;
        }
    }
}