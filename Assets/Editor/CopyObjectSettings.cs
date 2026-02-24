using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class CopyObjectSettings : EditorWindow
{
    private GameObject referenceObject;
    private GameObject[] targetObjects;
    private bool copyLayer = true;
    private bool copyTag = true;
    private bool copyTransform = false;
    private bool copyComponents = true;
    private bool copyColliders = true;
    private bool copyRenderer = true;

    [MenuItem("Tools/Copy Object Settings")]
    private static void ShowWindow()
    {
        GetWindow<CopyObjectSettings>("Copy Object Settings");
    }

    [MenuItem("GameObject/Copy Settings From Reference", false, 0)]
    private static void CopyFromReferenceMenu(UnityEditor.MenuCommand command)
    {
        GameObject target = command.context as GameObject;
        if (target != null)
        {
            CopyObjectSettings window = GetWindow<CopyObjectSettings>();
            window.targetObjects = new GameObject[] { target };
            window.Show();
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Copy Object Settings", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        // Reference object
        EditorGUILayout.LabelField("Reference Object (source of settings):", EditorStyles.boldLabel);
        referenceObject = (GameObject)EditorGUILayout.ObjectField(referenceObject, typeof(GameObject), true);

        EditorGUILayout.Space(10);

        // Target objects
        EditorGUILayout.LabelField("Target Objects (to apply settings):", EditorStyles.boldLabel);
        if (targetObjects != null && targetObjects.Length > 0)
        {
            for (int i = 0; i < targetObjects.Length; i++)
            {
                targetObjects[i] = (GameObject)EditorGUILayout.ObjectField($"Target {i + 1}:", targetObjects[i], typeof(GameObject), true);
            }
        }

        if (GUILayout.Button("Select Target Objects"))
        {
            GameObject[] selected = Selection.gameObjects;
            if (selected.Length > 0)
            {
                targetObjects = selected;
            }
        }

        EditorGUILayout.Space(10);

        // Copy options
        EditorGUILayout.LabelField("Copy Options:", EditorStyles.boldLabel);
        copyLayer = EditorGUILayout.Toggle("Copy Layer", copyLayer);
        copyTag = EditorGUILayout.Toggle("Copy Tag", copyTag);
        copyTransform = EditorGUILayout.Toggle("Copy Transform", copyTransform);
        copyComponents = EditorGUILayout.Toggle("Copy Components", copyComponents);
        copyColliders = EditorGUILayout.Toggle("Copy Colliders", copyColliders);
        copyRenderer = EditorGUILayout.Toggle("Copy Renderer", copyRenderer);

        EditorGUILayout.Space(15);

        // Apply button
        if (referenceObject == null)
        {
            EditorGUILayout.HelpBox("Please select a reference object first.", MessageType.Warning);
        }
        else if (targetObjects == null || targetObjects.Length == 0 || targetObjects.All(t => t == null))
        {
            EditorGUILayout.HelpBox("Please select at least one target object.", MessageType.Warning);
        }
        else
        {
            if (GUILayout.Button("Apply Settings", GUILayout.Height(30)))
            {
                ApplySettings();
            }
        }

        EditorGUILayout.Space(10);

        // Quick copy buttons
        EditorGUILayout.LabelField("Quick Copy:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Copy All to Selection"))
        {
            GameObject[] selected = Selection.gameObjects;
            if (referenceObject != null && selected.Length > 0)
            {
                targetObjects = selected;
                ApplySettings();
            }
        }
        if (GUILayout.Button("Swap Reference <-> Target"))
        {
            GameObject temp = referenceObject;
            referenceObject = targetObjects != null && targetObjects.Length > 0 ? targetObjects[0] : null;
            if (temp != null)
            {
                targetObjects = new GameObject[] { temp };
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ApplySettings()
    {
        if (referenceObject == null || targetObjects == null)
            return;

        foreach (GameObject target in targetObjects)
        {
            if (target == null || target == referenceObject)
                continue;

            Undo.RecordObject(target, "Copy Object Settings");

            // Copy layer
            if (copyLayer)
            {
                target.layer = referenceObject.layer;
            }

            // Copy tag
            if (copyTag)
            {
                target.tag = referenceObject.tag;
            }

            // Copy transform
            if (copyTransform)
            {
                target.transform.SetPositionAndRotation(
                    referenceObject.transform.position,
                    referenceObject.transform.rotation
                );
                target.transform.localScale = referenceObject.transform.localScale;
            }

            // Copy components
            if (copyComponents)
            {
                CopyComponents(referenceObject, target);
            }

            // Copy colliders separately (more detailed settings)
            if (copyColliders)
            {
                CopyColliders(referenceObject, target);
            }

            // Copy renderer
            if (copyRenderer)
            {
                CopyRenderer(referenceObject, target);
            }

            // Mark scene as dirty
            EditorUtility.SetDirty(target);
        }

        // Force rebuild of target object tree for hierarchy refresh
        UnityEditor.EditorApplication.RepaintHierarchyWindow();

        Debug.Log($"Applied settings from '{referenceObject.name}' to {targetObjects.Length} object(s).");
    }

    private void CopyComponents(GameObject source, GameObject target)
    {
        Component[] sourceComponents = source.GetComponents<Component>();
        
        foreach (Component sourceComponent in sourceComponents)
        {
            if (sourceComponent == null)
                continue;

            // Skip special components that shouldn't be copied
            System.Type componentType = sourceComponent.GetType();
            if (componentType == typeof(Transform) || 
                componentType == typeof(MeshFilter) || 
                componentType == typeof(MeshRenderer) ||
                componentType == typeof(SkinnedMeshRenderer) ||
                componentType == typeof(Collider) ||
                componentType == typeof(Collider2D) ||
                componentType == typeof(Rigidbody) ||
                componentType == typeof(Rigidbody2D))
                continue;

            // Check if target already has this component
            Component targetComponent = target.GetComponent(componentType);
            
            if (targetComponent == null)
            {
                // Add new component
                targetComponent = target.AddComponent(componentType);
            }

            // Copy component values
            CopyComponentValues(sourceComponent, targetComponent);
        }
    }

    private void CopyComponentValues(Component source, Component target)
    {
        if (source == null || target == null)
            return;

        System.Type type = source.GetType();
        BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        // Copy all serializable fields
        foreach (FieldInfo field in type.GetFields(flags))
        {
            if (!field.IsNotSerialized)
            {
                try
                {
                    field.SetValue(target, field.GetValue(source));
                }
                catch { }
            }
        }

        // Copy all serializable properties
        foreach (PropertyInfo prop in type.GetProperties(flags))
        {
            if (prop.CanWrite && prop.CanRead && !prop.IsDefined(typeof(HideInInspector), true))
            {
                try
                {
                    prop.SetValue(target, prop.GetValue(source, null), null);
                }
                catch { }
            }
        }
    }

    private void CopyColliders(GameObject source, GameObject target)
    {
        Collider[] sourceColliders = source.GetComponents<Collider>();
        Collider[] targetColliders = target.GetComponents<Collider>();

        // Remove colliders that don't exist in source
        foreach (Collider targetCollider in targetColliders)
        {
            bool found = false;
            System.Type targetType = targetCollider.GetType();

            foreach (Collider sourceCollider in sourceColliders)
            {
                if (sourceCollider.GetType() == targetType)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                DestroyImmediate(targetCollider);
            }
        }

        // Copy/add colliders
        foreach (Collider sourceCollider in sourceColliders)
        {
            Collider matchingCollider = null;
            
            // Try to find matching collider
            foreach (Collider targetCollider in target.GetComponents<Collider>())
            {
                if (targetCollider.GetType() == sourceCollider.GetType())
                {
                    matchingCollider = targetCollider;
                    break;
                }
            }

            // Add if not found
            if (matchingCollider == null)
            {
                matchingCollider = target.AddComponent(sourceCollider.GetType()) as Collider;
            }

            // Copy collider values
            CopyColliderValues(sourceCollider, matchingCollider);
        }
    }

    private void CopyColliderValues(Collider source, Collider target)
    {
        if (source == null || target == null)
            return;

        Undo.RecordObject(target, "Copy Collider Settings");

        // Copy common properties
        target.isTrigger = source.isTrigger;
        target.enabled = source.enabled;

        // BoxCollider
        if (source is BoxCollider boxSource && target is BoxCollider boxTarget)
        {
            boxTarget.center = boxSource.center;
            boxTarget.size = boxSource.size;
        }
        // SphereCollider
        else if (source is SphereCollider sphereSource && target is SphereCollider sphereTarget)
        {
            sphereTarget.center = sphereSource.center;
            sphereTarget.radius = sphereSource.radius;
        }
        // CapsuleCollider
        else if (source is CapsuleCollider capsuleSource && target is CapsuleCollider capsuleTarget)
        {
            capsuleTarget.center = capsuleSource.center;
            capsuleTarget.radius = capsuleSource.radius;
            capsuleTarget.height = capsuleSource.height;
            capsuleTarget.direction = capsuleSource.direction;
        }
        // MeshCollider
        else if (source is MeshCollider meshSource && target is MeshCollider meshTarget)
        {
            meshTarget.sharedMesh = meshSource.sharedMesh;
            meshTarget.convex = meshSource.convex;
            meshTarget.isTrigger = meshSource.isTrigger;
        }
        // WheelCollider
        else if (source is WheelCollider wheelSource && target is WheelCollider wheelTarget)
        {
            wheelTarget.center = wheelSource.center;
            wheelTarget.radius = wheelSource.radius;
            wheelTarget.suspensionDistance = wheelSource.suspensionDistance;
            wheelTarget.suspensionSpring = wheelSource.suspensionSpring;
            wheelTarget.motorTorque = wheelSource.motorTorque;
            wheelTarget.brakeTorque = wheelSource.brakeTorque;
            wheelTarget.steerAngle = wheelSource.steerAngle;
        }
    }

    private void CopyRenderer(GameObject source, GameObject target)
    {
        Renderer sourceRenderer = source.GetComponent<Renderer>();
        Renderer targetRenderer = target.GetComponent<Renderer>();

        if (sourceRenderer == null || targetRenderer == null)
            return;

        Undo.RecordObject(targetRenderer, "Copy Renderer Settings");

        // Copy materials
        Material[] sourceMaterials = new Material[sourceRenderer.sharedMaterials.Length];
        for (int i = 0; i < sourceRenderer.sharedMaterials.Length; i++)
        {
            sourceMaterials[i] = sourceRenderer.sharedMaterials[i];
        }
        targetRenderer.sharedMaterials = sourceMaterials;

        // Copy mesh renderer specific
        if (sourceRenderer is MeshRenderer sourceMesh && targetRenderer is MeshRenderer targetMesh)
        {
            // Copy additional GIMode if available
        }

        // Copy skinned mesh renderer specific
        if (sourceRenderer is SkinnedMeshRenderer sourceSkinned && targetRenderer is SkinnedMeshRenderer targetSkinned)
        {
            targetSkinned.sharedMesh = sourceSkinned.sharedMesh;
            targetSkinned.rootBone = sourceSkinned.rootBone;
            targetSkinned.bones = sourceSkinned.bones;
            targetSkinned.quality = sourceSkinned.quality;
            targetSkinned.updateWhenOffscreen = sourceSkinned.updateWhenOffscreen;
        }
    }
}
