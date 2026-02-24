using UnityEngine;
using Newtonsoft.Json.Linq;

namespace UHFPS.Runtime
{
    /// <summary>
    /// Saves and loads the enabled state of a Collider.
    /// Automatically tracks changes to the enabled state.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SaveableCollider : MonoBehaviour, IRuntimeSaveable
    {
        [field: SerializeField]
        public UniqueID UniqueID { get; set; }

        [Header("Settings")]
        public bool SaveEnabled = true;

        private Collider collider;
        private bool savedEnabledState;

        private void Awake()
        {
            collider = GetComponent<Collider>();
            savedEnabledState = collider.enabled;
        }

        private void Update()
        {
            // Track changes to enabled state
            if (collider.enabled != savedEnabledState)
            {
                savedEnabledState = collider.enabled;
            }
        }

        public StorableCollection OnSave()
        {
            StorableCollection data = new();

            if (SaveEnabled)
            {
                data.Add("enabled", savedEnabledState);
            }

            return data;
        }

        public void OnLoad(JToken data)
        {
            if (data == null) return;

            if (SaveEnabled)
            {
                bool enabled = (bool)data["enabled"];
                collider.enabled = enabled;
                savedEnabledState = enabled;
            }
        }
    }
}
