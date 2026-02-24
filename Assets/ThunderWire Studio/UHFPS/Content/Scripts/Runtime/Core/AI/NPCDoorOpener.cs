using System.Collections.Generic;
using UnityEngine;
using UHFPS.Runtime;

namespace UHFPS.Runtime
{
    public class NPCDoorOpener : MonoBehaviour
    {
        public float detectionRadius = 3f;
        public LayerMask doorLayer = 1 << 9; // Interact layer
        public string[] doorTags = new string[] { "Door" }; // Tags to identify doors

        private List<DynamicObject> doorsInRange = new List<DynamicObject>();

        private void Update()
        {
            // Find all DynamicObject in radius
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, doorLayer);

            doorsInRange.Clear();
            foreach (Collider col in colliders)
            {
                DynamicObject dynamicObj = col.GetComponent<DynamicObject>();
                if (dynamicObj != null && 
                    dynamicObj.dynamicType == DynamicObject.DynamicType.Openable && 
                    !dynamicObj.IsOpened && 
                    HasAllowedTag(col.gameObject))
                {
                    doorsInRange.Add(dynamicObj);
                }
            }

            // Open doors
            foreach (DynamicObject door in doorsInRange)
            {
                door.SetOpenState();
            }
        }

        private bool HasAllowedTag(GameObject obj)
        {
            if (doorTags == null || doorTags.Length == 0)
                return true;

            foreach (string tag in doorTags)
            {
                if (!string.IsNullOrEmpty(tag) && obj.CompareTag(tag))
                    return true;
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}