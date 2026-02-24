using UnityEngine;
using UnityEngine.Events;
using ThunderWire.Attributes;

namespace UHFPS.Runtime
{
    [InspectorHeader("Options Escape Events")]
    public class OptionsEscapeEvents : MonoBehaviour
    {
        public bool DiscardChanges;
        [Space]
        public UnityEvent EscapeEvents;

        private OptionsManager optionsManager;

        private void Awake()
        {
            // Only get OptionsManager reference if it's available
            if (OptionsManager.HasReference)
            {
                optionsManager = OptionsManager.Instance;
            }
            GameManager.SubscribePauseEvent(esc =>
            {
                if (!esc) OnEscape();
            });
        }

        public void OnEscape()
        {
            EscapeEvents?.Invoke();
            if (DiscardChanges)
                optionsManager.DiscardChanges();
        }
    }
}