using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SplitMeshByMaterials : EditorWindow
{
    private GameObject selectedObject;
    private Material[] materials;
    private bool[] selectedMaterials;
    private Vector2 scrollPos;

    [MenuItem("Tools/Split Selected Meshes by Materials")]
    static void OpenWindow()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length != 1)
        {
            EditorUtility.DisplayDialog("Error", "Please select exactly one GameObject.", "OK");
            return;
        }

        GameObject obj = selected[0];
        MeshFilter mf = obj.GetComponent<MeshFilter>();
        MeshRenderer mr = obj.GetComponent<MeshRenderer>();

        if (mf == null || mr == null)
        {
            EditorUtility.DisplayDialog("Error", "Selected object must have MeshFilter and MeshRenderer.", "OK");
            return;
        }

        Material[] mats = mr.sharedMaterials;
        if (mats.Length <= 1)
        {
            EditorUtility.DisplayDialog("Info", "Object has only one material. Nothing to split.", "OK");
            return;
        }

        SplitMeshByMaterials window = GetWindow<SplitMeshByMaterials>("Split by Materials");
        window.selectedObject = obj;
        window.materials = mats;
        window.selectedMaterials = new bool[mats.Length];
        window.minSize = new Vector2(300, 200);
    }

    void OnGUI()
    {
        if (selectedObject == null || materials == null)
        {
            EditorGUILayout.LabelField("No object selected.");
            return;
        }

        EditorGUILayout.LabelField("Select materials to split into separate objects:");
        EditorGUILayout.LabelField($"Object: {selectedObject.name}");

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < materials.Length; i++)
        {
            selectedMaterials[i] = EditorGUILayout.ToggleLeft(materials[i] != null ? materials[i].name : "Null Material", selectedMaterials[i]);
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Split Selected Materials"))
        {
            Split();
            Close();
        }
    }

    void Split()
    {
        MeshFilter meshFilter = selectedObject.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = selectedObject.GetComponent<MeshRenderer>();
        Mesh originalMesh = meshFilter.sharedMesh;

        List<int> splitIndices = new List<int>();
        for (int i = 0; i < selectedMaterials.Length; i++)
        {
            if (selectedMaterials[i]) splitIndices.Add(i);
        }

        if (splitIndices.Count == 0)
        {
            EditorUtility.DisplayDialog("Info", "No materials selected for splitting.", "OK");
            return;
        }

        // Create parent
        GameObject parent = new GameObject(selectedObject.name + "_Split");
        parent.transform.position = selectedObject.transform.position;
        parent.transform.rotation = selectedObject.transform.rotation;
        parent.transform.localScale = selectedObject.transform.localScale;
        parent.transform.SetParent(selectedObject.transform.parent);

        // Create objects for selected materials
        foreach (int i in splitIndices)
        {
            Mesh subMesh = CreateSubMesh(originalMesh, i);
            if (subMesh == null) continue;

            GameObject part = new GameObject(selectedObject.name + "_Part_" + materials[i].name);
            part.transform.SetParent(parent.transform);
            part.transform.localPosition = Vector3.zero;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = Vector3.one;

            MeshFilter partFilter = part.AddComponent<MeshFilter>();
            partFilter.sharedMesh = subMesh;

            MeshRenderer partRenderer = part.AddComponent<MeshRenderer>();
            partRenderer.sharedMaterial = materials[i];
        }

        // Update original mesh to remove split submeshes
        List<Material> remainingMaterials = new List<Material>();
        List<int[]> remainingTriangles = new List<int[]>();
        for (int i = 0; i < materials.Length; i++)
        {
            if (!selectedMaterials[i])
            {
                remainingMaterials.Add(materials[i]);
                remainingTriangles.Add(originalMesh.GetTriangles(i));
            }
        }

        if (remainingMaterials.Count > 0)
        {
            Mesh newMesh = Object.Instantiate(originalMesh);
            newMesh.name = originalMesh.name + "_Remaining";
            newMesh.subMeshCount = remainingMaterials.Count;
            for (int i = 0; i < remainingTriangles.Count; i++)
            {
                newMesh.SetTriangles(remainingTriangles[i], i);
            }
            meshFilter.sharedMesh = newMesh;
            meshRenderer.sharedMaterials = remainingMaterials.ToArray();
        }
        else
        {
            // If all materials split, disable original
            selectedObject.SetActive(false);
        }

        AssetDatabase.Refresh();
    }

    static Mesh CreateSubMesh(Mesh originalMesh, int submeshIndex)
    {
        if (submeshIndex >= originalMesh.subMeshCount)
            return null;

        int[] triangles = originalMesh.GetTriangles(submeshIndex);
        if (triangles.Length == 0)
            return null;

        Mesh subMesh = new Mesh();
        subMesh.name = originalMesh.name + "_Sub" + submeshIndex;
        subMesh.vertices = originalMesh.vertices;
        subMesh.normals = originalMesh.normals;
        subMesh.uv = originalMesh.uv;
        subMesh.uv2 = originalMesh.uv2;
        subMesh.tangents = originalMesh.tangents;
        subMesh.colors = originalMesh.colors;
        subMesh.subMeshCount = 1;
        subMesh.SetTriangles(triangles, 0);
        subMesh.RecalculateBounds();

        return subMesh;
    }
}