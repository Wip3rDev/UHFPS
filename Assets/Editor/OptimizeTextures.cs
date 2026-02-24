using UnityEngine;
using UnityEditor;

public class OptimizeTextures : EditorWindow
{
    private int maxSize = 64;

    [MenuItem("Tools/Optimize Textures for Build Size")]
    private static void OpenWindow()
    {
        GetWindow<OptimizeTextures>("Optimize Textures");
    }

    private void OnGUI()
    {
        GUILayout.Label("Texture Optimization Settings", EditorStyles.boldLabel);
        maxSize = EditorGUILayout.IntField("Max Texture Size", maxSize);

        if (GUILayout.Button("Optimize Textures"))
        {
            Optimize();
        }
    }

    private void Optimize()
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });
        int count = 0;

        foreach (string guid in textureGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                bool changed = false;

                // Set max size
                if (importer.maxTextureSize != maxSize)
                {
                    importer.maxTextureSize = maxSize;
                    changed = true;
                }

                // Set compression to Crunch
                if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
                {
                    importer.textureCompression = TextureImporterCompression.CompressedHQ; // Crunch
                    changed = true;
                }

                // Enable mipmaps if not
                if (!importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = true;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    count++;
                }
            }
        }

        Debug.Log($"Optimized {count} textures to max size {maxSize}.");
    }
}