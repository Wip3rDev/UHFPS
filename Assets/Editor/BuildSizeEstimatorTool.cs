using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class BuildSizeEstimatorTool : EditorWindow
{
    private Vector2 sceneScrollPos;
    private Vector2 resultScrollPos;

    private List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
    private List<EditorBuildSettingsScene> selectedScenes = new List<EditorBuildSettingsScene>();

    private long estimatedSizeBytes;
    private bool calculating;

    private Dictionary<string, long> assetSizes = new Dictionary<string, long>();
    private Dictionary<string, List<(string path, long size)>> categorizedAssets = new Dictionary<string, List<(string, long)>>();

    private Dictionary<string, bool> categoryFoldouts = new Dictionary<string, bool>();

    private string[] categoryOrder = { "Textures", "Audio", "Models", "Materials", "Prefabs", "Animations", "Shaders", "Other" };
    private bool showSummary = true;

    [MenuItem("Tools/Build Size Estimator", priority = 2001)]
    private static void OpenWindow()
    {
        GetWindow<BuildSizeEstimatorTool>("Build Size Estimator");
    }

    private void OnEnable()
    {
        scenes = EditorBuildSettings.scenes.ToList();
        selectedScenes = scenes.Where(s => s.enabled).ToList();

        foreach (var cat in categoryOrder)
        {
            if (!categoryFoldouts.ContainsKey(cat))
                categoryFoldouts[cat] = true;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("📦 Оценка примерного веса билда (Pro)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (scenes.Count == 0)
        {
            EditorGUILayout.HelpBox("В Build Settings нет сцен.", MessageType.Warning);
            if (GUILayout.Button("Открыть Build Settings"))
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            return;
        }

        EditorGUILayout.LabelField("Выберите сцены для анализа:");
        EditorGUILayout.Space();

        sceneScrollPos = EditorGUILayout.BeginScrollView(sceneScrollPos, GUILayout.Height(150));
        foreach (var scene in scenes)
        {
            bool wasSelected = selectedScenes.Contains(scene);
            bool selected = EditorGUILayout.ToggleLeft(Path.GetFileNameWithoutExtension(scene.path), wasSelected);
            if (selected && !wasSelected)
                selectedScenes.Add(scene);
            else if (!selected && wasSelected)
                selectedScenes.Remove(scene);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (GUILayout.Button("🔍 Рассчитать примерный вес", GUILayout.Height(30)))
            EstimateBuildSize();

        if (calculating)
        {
            EditorGUILayout.HelpBox("Выполняется анализ... Пожалуйста, подождите.", MessageType.Info);
        }
        else if (estimatedSizeBytes > 0)
        {
            float mb = estimatedSizeBytes / (1024f * 1024f);
            EditorGUILayout.HelpBox($"Примерный вес билда: {mb:F2} MB", MessageType.Info);

            showSummary = EditorGUILayout.Foldout(showSummary, "📊 Подробности по категориям", true);
            if (showSummary)
            {
                resultScrollPos = EditorGUILayout.BeginScrollView(resultScrollPos, GUILayout.Height(350));
                DrawCategoryBreakdown();
                EditorGUILayout.EndScrollView();
            }
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Очистить результаты"))
        {
            estimatedSizeBytes = 0;
            calculating = false;
            categorizedAssets.Clear();
        }
    }

    private void DrawCategoryBreakdown()
    {
        foreach (var cat in categoryOrder)
        {
            if (!categorizedAssets.ContainsKey(cat))
                continue;

            var list = categorizedAssets[cat];
            long total = list.Sum(l => l.size);
            float percent = (float)total / estimatedSizeBytes * 100f;

            EditorGUILayout.BeginVertical("box");
            categoryFoldouts[cat] = EditorGUILayout.Foldout(categoryFoldouts[cat], $"{cat} — {total / (1024f * 1024f):F2} MB ({percent:F1}%)", true);

            if (categoryFoldouts[cat])
            {
                foreach (var (path, size) in list.OrderByDescending(l => l.size))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {Path.GetFileName(path)}", GUILayout.Width(220));
                    EditorGUILayout.LabelField($"{(size / 1024f):F1} KB", GUILayout.Width(80));

                    if (GUILayout.Button("🔎", GUILayout.Width(30)))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                        if (asset != null)
                        {
                            EditorGUIUtility.PingObject(asset);
                            Selection.activeObject = asset;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void EstimateBuildSize()
    {
        if (selectedScenes.Count == 0)
        {
            EditorUtility.DisplayDialog("Ошибка", "Выберите хотя бы одну сцену для анализа.", "OK");
            return;
        }

        calculating = true;
        estimatedSizeBytes = 0;
        assetSizes.Clear();
        categorizedAssets.Clear();

        try
        {
            HashSet<string> allAssetPaths = new HashSet<string>();
            foreach (var scene in selectedScenes)
            {
                string[] dependencies = AssetDatabase.GetDependencies(scene.path, true);
                foreach (var dep in dependencies)
                {
                    if (!dep.EndsWith(".cs") && !dep.EndsWith(".dll"))
                        allAssetPaths.Add(dep);
                }
            }

            int count = 0;
            foreach (var path in allAssetPaths)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Анализ ассетов", path, (float)count / allAssetPaths.Count))
                    break;

                FileInfo fi = new FileInfo(path);
                if (fi.Exists)
                {
                    long size = fi.Length;
                    assetSizes[path] = size;

                    string cat = CategorizeAsset(path);
                    if (!categorizedAssets.ContainsKey(cat))
                        categorizedAssets[cat] = new List<(string, long)>();
                    categorizedAssets[cat].Add((path, size));
                }

                count++;
            }

            EditorUtility.ClearProgressBar();

            float systemOverheadMultiplier = 1.15f;
            estimatedSizeBytes = (long)(assetSizes.Values.Sum() * systemOverheadMultiplier);

            calculating = false;
            Debug.Log($"[Build Size Estimator] Найдено {assetSizes.Count} ассетов. Примерный вес: {(estimatedSizeBytes / (1024f * 1024f)):F2} MB");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            calculating = false;
            Repaint();
        }
    }

    private string CategorizeAsset(string path)
    {
        string ext = Path.GetExtension(path).ToLower();

        if (ext == ".png" || ext == ".jpg" || ext == ".tga" || ext == ".psd" || ext == ".exr")
            return "Textures";
        if (ext == ".wav" || ext == ".mp3" || ext == ".ogg")
            return "Audio";
        if (ext == ".fbx" || ext == ".obj" || ext == ".blend")
            return "Models";
        if (ext == ".mat")
            return "Materials";
        if (ext == ".prefab")
            return "Prefabs";
        if (ext == ".anim" || ext == ".controller")
            return "Animations";
        if (ext == ".shader" || ext == ".shadergraph")
            return "Shaders";

        return "Other";
    }
}
