using UnityEngine;
using System.Collections;

namespace UHFPS.Runtime
{
    /// <summary>
    /// Triggers autosave when a specific objective is completed.
    /// Use this instead of calling SaveGameManager.SaveGame() from UnityEvents.
    /// </summary>
    [AddComponentMenu("UHFPS/Triggers/Autosave On Objective")]
    public class AutosaveOnObjective : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("The key of the objective that will trigger the autosave.")]
        public string ObjectiveKey;

        [Tooltip("Delay in seconds before saving.")]
        [Range(0.1f, 2f)]
        public float Delay = 0.5f;

        [Tooltip("Only save once. Prevents multiple saves if objective is completed again.")]
        public bool OnlyOnce = true;

        private bool hasSaved;

        private void OnEnable()
        {
            SubscribeToObjectiveEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromObjectiveEvents();
        }

        private void SubscribeToObjectiveEvents()
        {
            if (!ObjectiveManager.HasReference) return;

            var events = FindObjectsByType<ObjectiveEvent>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var evt in events)
            {
                if (evt.Objective.IsValid && evt.Objective.CompareObj(ObjectiveKey))
                {
                    evt.OnObjectiveCompleted.AddListener(OnObjectiveCompletedHandler);
                }
            }
        }

        private void UnsubscribeFromObjectiveEvents()
        {
            if (!ObjectiveManager.HasReference) return;

            var events = FindObjectsByType<ObjectiveEvent>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var evt in events)
            {
                if (evt.Objective.IsValid && evt.Objective.CompareObj(ObjectiveKey))
                {
                    evt.OnObjectiveCompleted.RemoveListener(OnObjectiveCompletedHandler);
                }
            }
        }

        private void OnObjectiveCompletedHandler()
        {
            if (!hasSaved)
            {
                if (OnlyOnce) hasSaved = true;
                StartCoroutine(DelayedSave());
            }
        }

        private IEnumerator DelayedSave()
        {
            // Wait for all UnityEvents to complete (2 frames)
            yield return null;
            yield return null;

            // Small additional delay for safety
            yield return new WaitForSeconds(Delay);

            // Check if game is not currently loading
            if (!SaveGameManager.GameWillLoad)
            {
                SaveGameManager.SaveGame(true);
            }
        }
    }
}
