using System;
using System.Reflection;
using UnityEngine;

namespace UHFPS.Runtime
{
    [Serializable]
    public class OptionVolumeActive : OptionObserverType
    {
        public VolumeComponentReferecne volumeComponent = new();

        public override string Name => "Volume Active";

        public override void OptionUpdate(object value)
        {
            if (value == null)
                return;

            bool enabled = (bool)value;

            // Set Volume Component Active
            if (volumeComponent.Volume != null)
            {
                volumeComponent.SetVolumeComponentActive(enabled);
            }

            // UHFPS Integration: Also control VHSProRendererFeature if this is VHSPro
            TryControlVHSProRendererFeature(enabled);
        }

        private void TryControlVHSProRendererFeature(bool enabled)
        {
            // Check if this volume component is VHSProVolumeComponent
            if (volumeComponent.TryGetVolumeComponent(out UnityEngine.Rendering.VolumeComponent component))
            {
                // Check if component type name contains "VHSPro"
                string typeName = component.GetType().Name;
                if (typeName.Contains("VHSPro") || typeName.Contains("VHSProVolumeComponent"))
                {
                    // Find VHSProRendererFeature and set enabled
                    try
                    {
                        var features = Resources.FindObjectsOfTypeAll<UnityEngine.ScriptableObject>();
                        foreach (var feature in features)
                        {
                            if (feature.name == "VHSPro" && feature.GetType().Name.Contains("RendererFeature"))
                            {
                                // Use reflection to call SetEnabled
                                var method = feature.GetType().GetMethod("SetEnabled", BindingFlags.Public | BindingFlags.Instance);
                                method?.Invoke(feature, new object[] { enabled });
                            }
                        }
                    }
                    catch { /* Ignore errors */ }
                }
            }
        }
    }
}
