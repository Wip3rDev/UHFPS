using UnityEngine;
using Newtonsoft.Json.Linq;

namespace UHFPS.Runtime
{
    /// <summary>
    /// Saves and loads the enabled state and playing state of an AudioSource.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SaveableAudio : MonoBehaviour, IRuntimeSaveable
    {
        [field: SerializeField]
        public UniqueID UniqueID { get; set; }

        [Header("Settings")]
        public bool SaveEnabled = true;
        public bool SaveIsPlaying = true;

        private AudioSource audioSource;
        private bool wasPlaying;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            // Track if audio is playing
            if (audioSource.isPlaying)
            {
                wasPlaying = true;
            }
        }

        public StorableCollection OnSave()
        {
            StorableCollection data = new();

            if (SaveEnabled)
            {
                data.Add("enabled", audioSource.enabled);
            }

            if (SaveIsPlaying && wasPlaying)
            {
                data.Add("isPlaying", true);
            }

            return data;
        }

        public void OnLoad(JToken data)
        {
            if (data == null) return;

            if (SaveEnabled)
            {
                JToken enabledToken = data["enabled"];
                if (enabledToken != null && enabledToken.Type == JTokenType.Boolean)
                {
                    audioSource.enabled = (bool)enabledToken;
                }
            }

            if (SaveIsPlaying)
            {
                JToken isPlayingToken = data["isPlaying"];
                if (isPlayingToken != null && isPlayingToken.Type == JTokenType.Boolean)
                {
                    bool isPlaying = (bool)isPlayingToken;
                    if (isPlaying)
                    {
                        // Start playing if it was playing before
                        audioSource.Play();
                        wasPlaying = false; // Reset, Update will track it again
                    }
                }
            }
        }
    }
}
