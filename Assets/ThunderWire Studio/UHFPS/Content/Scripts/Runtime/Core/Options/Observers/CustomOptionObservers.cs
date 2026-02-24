using System.Collections.Generic;
using UnityEngine;

namespace UHFPS.Runtime
{
    public class CustomOptionObservers : MonoBehaviour
    {
        [SerializeReference]
        public List<OptionObserverType> OptionObservers = new();

        private void Start()
        {
            foreach (var option in OptionObservers)
            {
                option.OnStart();
                // Only observe option if OptionsManager is available
                if (OptionsManager.HasReference)
                {
                    OptionsManager.ObserveOption(option.ObserveOptionName, option.OptionUpdate);
                }
            }
        }
    }
}