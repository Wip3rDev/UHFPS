using UnityEngine;
using UnityEditor;

public class OptimizePlayerSettings : EditorWindow
{
    [MenuItem("Tools/Optimize Player Settings for Build Size")]
    private static void Optimize()
    {
        // Set managed stripping to High
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Standalone, ManagedStrippingLevel.High);
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.stripUnusedMeshComponents = true;

        // Note: Compression is set in Build Settings, not here. Use LZ4 in Build Settings.

        // Disable unused modules if possible
        // Note: This might require manual adjustment based on project needs

        Debug.Log("Player Settings optimized for smaller build size. Set compression to LZ4 in Build Settings.");
    }
}