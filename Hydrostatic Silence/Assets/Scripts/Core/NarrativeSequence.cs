using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// A sequence of narrative beats for one scene.
    /// Create one per scene: Observatory, MeridianBay, MeridianBridge, Closing.
    /// </summary>
    [CreateAssetMenu(fileName = "NarrativeSequence", menuName = "Hydrostatic Silence/Narrative Sequence")]
    public abstract class NarrativeSequence : ScriptableObject
    {
        [Tooltip("The first beat ID to play when this sequence starts.")]
        public string startBeatID;

        [Tooltip("All beats in this sequence.")]
        public List<NarrativeBeat> beats = new();

        // ReSharper disable once InconsistentNaming
        private Dictionary<string, NarrativeBeat> _lookup;

        /// <summary>
        /// Get a beat by its ID. Builds lookup on first call.
        /// </summary>
        public NarrativeBeat GetBeat(string beatID)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<string, NarrativeBeat>();
                foreach (var beat in beats)
                {
                    if (!string.IsNullOrEmpty(beat.beatID))
                        _lookup[beat.beatID] = beat;
                }
            }

            return _lookup.TryGetValue(beatID, out var found) ? found : null;
        }

        /// <summary>
        /// Force rebuild of the lookup dictionary.
        /// Call if beats are modified at runtime.
        /// </summary>
        public void RebuildLookup() => _lookup = null;
    }
}