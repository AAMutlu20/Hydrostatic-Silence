using UnityEngine;
using UnityEngine.Serialization;
// ReSharper disable InconsistentNaming

namespace Core
{
    /// <summary>
    /// Persistent game state tracking all narrative variables.
    /// Lives as a ScriptableObject so it survives scene loads.
    /// Reset it at game start via ResetAll().
    /// </summary>
    [CreateAssetMenu(fileName = "GameState", menuName = "Hydrostatic Silence/Game State")]
    public class GameState : ScriptableObject
    {
        [FormerlySerializedAs("Framework")]
        [Header("Scene 1 — Observatory")]
        [Tooltip("Which interpretation Vera chose: alive / scientific / engineering / neutral")]
        public string framework = "neutral";

        [Tooltip("Vera-Maren relationship tone: warm / professional / withdrawn / neutral")]
        public string veraMarenTone = "neutral";

        [Header("Scene 2 — Meridian")]
        [Tooltip("How Cavall approached Vera: direct / resigned / honest / neutral")]
        public string cavallApproach = "neutral";

        [Tooltip("Vera's parting admission: fear / entropy / quiet / neutral")]
        public string veraAdmission = "neutral";

        [Header("Scene 3 — Contact")]
        [Tooltip("Drev's reaction style: write / look / check / neutral")]
        public string drevResponse = "neutral";

        [Tooltip("What Drev does with the wake data: write / cavall / solenne / neutral")]
        public string drevWake = "neutral";

        [Header("Lore Flags")]
        public bool loreNotebook;
        public bool lorePhoto;
        public bool loreDeepfield1;
        public bool loreManifest;
        public bool loreCharter;
        public bool loreDrevLog;
        public bool loreDeepfield2;

        /// <summary>
        /// True if the player found deep-field data in BOTH Scene 1 and Scene 3.
        /// Gates the hidden ending content.
        /// </summary>
        public bool HiddenEndingUnlocked => loreDeepfield1 && loreDeepfield2;

        /// <summary>
        /// Call at game start to wipe all state for a fresh playthrough.
        /// </summary>
        public void ResetAll()
        {
            framework = "neutral";
            veraMarenTone = "neutral";
            cavallApproach = "neutral";
            veraAdmission = "neutral";
            drevResponse = "neutral";
            drevWake = "neutral";

            loreNotebook = false;
            lorePhoto = false;
            loreDeepfield1 = false;
            loreManifest = false;
            loreCharter = false;
            loreDrevLog = false;
            loreDeepfield2 = false;
        }
    }
}