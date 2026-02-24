using UnityEngine;
using UnityEditor;

public class OptimizeAudio : EditorWindow
{
    [MenuItem("Tools/Optimize Audio for Build Size")]
    private static void Optimize()
    {
        string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
        int count = 0;

        foreach (string guid in audioGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;

            if (importer != null)
            {
                bool changed = false;

                // Set compression format to Vorbis
                if (importer.defaultSampleSettings.compressionFormat != AudioCompressionFormat.Vorbis)
                {
                    var settings = importer.defaultSampleSettings;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.1f; // Extremely low quality
                    importer.defaultSampleSettings = settings;
                    changed = true;
                }

                // Set load type to Compressed In Memory if not
                if (importer.defaultSampleSettings.loadType != AudioClipLoadType.CompressedInMemory)
                {
                    var settings = importer.defaultSampleSettings;
                    settings.loadType = AudioClipLoadType.CompressedInMemory;
                    importer.defaultSampleSettings = settings;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                    count++;
                }
            }
        }

        Debug.Log($"Optimized {count} audio clips.");
    }
}