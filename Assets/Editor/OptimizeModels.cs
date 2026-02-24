using UnityEngine;
using UnityEditor;

public class OptimizeModels : EditorWindow
{
    [MenuItem("Tools/Optimize Models for Build Size")]
    private static void Optimize()
    {
        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets" });
        int count = 0;

        foreach (string guid in modelGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

            if (importer != null)
            {
                bool changed = false;

                // Enable mesh compression
                if (importer.meshCompression != ModelImporterMeshCompression.Medium)
                {
                    importer.meshCompression = ModelImporterMeshCompression.Medium;
                    changed = true;
                }

                // Disable read/write if not needed
                if (importer.isReadable)
                {
                    importer.isReadable = false;
                    changed = true;
                }

                // Optimize for
                if (importer.optimizeMeshPolygons)
                {
                    importer.optimizeMeshPolygons = true;
                    changed = true;
                }

                if (importer.optimizeMeshVertices)
                {
                    importer.optimizeMeshVertices = true;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    count++;
                }
            }
        }

        Debug.Log($"Optimized {count} models.");
    }
}