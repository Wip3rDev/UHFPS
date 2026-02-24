using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public class FindMissingReferencesTool : EditorWindow
{
    private Vector2 scrollPos;
    private List<GameObject> missingScriptObjects = new();
    private List<(GameObject obj, Component comp, string field, string guid)> missingReferences = new();

    private static readonly string[] ignoreFieldKeywords =
    {
        "asset", "template", "prefab", "profile", "library", "default", "editor", "runtime"
    };

    [MenuItem("Tools/Find Missing References (Smart)", priority = 2001)]
    private static void OpenWindow()
    {
        GetWindow<FindMissingReferencesTool>("Find Missing References");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("🔍 Поиск сломанных ссылок и скриптов", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("🧩 Проверить текущую сцену", GUILayout.Height(28)))
            FindMissingInScene();

        if (GUILayout.Button("📦 Проверить все префабы", GUILayout.Height(28)))
            FindMissingInPrefabs();

        if (GUILayout.Button("🧠 Проверить в симуляции (Play Mode Simulation)", GUILayout.Height(28)))
            SimulateRuntimeCheck();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Результаты:", EditorStyles.boldLabel);

        if (missingScriptObjects.Count == 0 && missingReferences.Count == 0)
        {
            EditorGUILayout.HelpBox("Ничего не найдено.", MessageType.Info);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ==== MISSING SCRIPTS ====
        if (missingScriptObjects.Count > 0)
        {
            EditorGUILayout.LabelField("❌ Объекты с Missing Script:", EditorStyles.boldLabel);
            foreach (var go in missingScriptObjects)
            {
                string guid = GetObjectGUID(go);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(go, typeof(GameObject), true);
                EditorGUILayout.LabelField($"GUID: {guid}", GUILayout.Width(280));

                if (GUILayout.Button("📋", GUILayout.Width(25)))
                    GUIUtility.systemCopyBuffer = guid;

                if (GUILayout.Button("👁", GUILayout.Width(25)))
                    PingAssetByGUID(guid);

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();
        }

        // ==== MISSING REFERENCES ====
        if (missingReferences.Count > 0)
        {
            EditorGUILayout.LabelField("⚠️ Объекты с Missing Reference:", EditorStyles.boldLabel);
            foreach (var (obj, comp, field, guid) in missingReferences)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                GUILayout.Label($"→ {comp.GetType().Name}.{field}");
                GUILayout.FlexibleSpace();
                GUILayout.Label($"GUID: {guid}", GUILayout.Width(280));

                if (GUILayout.Button("📋", GUILayout.Width(25)))
                    GUIUtility.systemCopyBuffer = guid;

                if (GUILayout.Button("👁", GUILayout.Width(25)))
                    PingAssetByGUID(guid);

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (GUILayout.Button("🗑 Удалить все Missing Scripts", GUILayout.Height(25)))
            RemoveAllMissingScripts();

        if (GUILayout.Button("Очистить список", GUILayout.Height(25)))
        {
            missingScriptObjects.Clear();
            missingReferences.Clear();
        }
    }

    private void FindMissingInScene()
    {
        missingScriptObjects.Clear();
        missingReferences.Clear();

        foreach (var go in FindObjectsOfType<GameObject>(true))
            CheckObject(go, smartFilter: true);

        Debug.Log($"✅ Проверка завершена: {missingScriptObjects.Count} Missing Script, {missingReferences.Count} Missing Reference (фильтрованный).");
    }

    private void FindMissingInPrefabs()
    {
        missingScriptObjects.Clear();
        missingReferences.Clear();

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int count = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            CheckObject(prefab, smartFilter: true);
            count++;
        }

        Debug.Log($"📦 Проверено {count} префабов. Найдено {missingScriptObjects.Count} Missing Script и {missingReferences.Count} Missing Reference (фильтрованный).");
    }

    private void SimulateRuntimeCheck()
    {
        missingScriptObjects.Clear();
        missingReferences.Clear();

        var allObjects = FindObjectsOfType<GameObject>(true);
        foreach (var go in allObjects)
            CheckObject(go, smartFilter: false, simulateRuntime: true);

        Debug.Log($"🧠 Симуляция завершена. Найдено {missingScriptObjects.Count} Missing Script и {missingReferences.Count} Missing Reference (реальные).");
    }

    private void CheckObject(GameObject go, bool smartFilter = false, bool simulateRuntime = false)
    {
        Component[] components = go.GetComponents<Component>();
        string goGuid = GetObjectGUID(go);

        foreach (Component comp in components)
        {
            if (comp == null)
            {
                missingScriptObjects.Add(go);
                continue;
            }

            SerializedObject so = new SerializedObject(comp);
            SerializedProperty prop = so.GetIterator();

            while (prop.NextVisible(true))
            {
                if (prop.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                    {
                        string fieldName = prop.displayName.ToLower();

                        if (smartFilter)
                        {
                            bool skip = false;
                            foreach (string keyword in ignoreFieldKeywords)
                                if (fieldName.Contains(keyword))
                                    skip = true;
                            if (skip) continue;

                            var field = comp.GetType().GetField(prop.name,
                                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                            if (field != null)
                            {
                                if (field.GetCustomAttribute<HideInInspector>() != null ||
                                    field.GetCustomAttribute<System.NonSerializedAttribute>() != null)
                                    continue;
                            }
                        }

                        if (simulateRuntime)
                        {
                            if (Application.isPlaying) continue;
                        }

                        missingReferences.Add((go, comp, prop.displayName, goGuid));
                    }
                }
            }
        }
    }

    private void RemoveAllMissingScripts()
    {
        int removed = 0;
        foreach (GameObject go in missingScriptObjects)
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

        Debug.Log($"🗑 Удалено {removed} сломанных скриптов.");
        missingScriptObjects.Clear();
    }

    private string GetObjectGUID(Object obj)
    {
        string path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path))
            return "Scene Object (no asset)";
        return AssetDatabase.AssetPathToGUID(path);
    }

    private void PingAssetByGUID(string guid)
    {
        if (string.IsNullOrEmpty(guid) || guid == "Scene Object (no asset)")
        {
            EditorUtility.DisplayDialog("Информация", "Этот объект не связан с ассетом в проекте.", "OK");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
        {
            EditorUtility.DisplayDialog("Ошибка", "Не удалось найти ассет по GUID: " + guid, "OK");
            return;
        }

        Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (asset != null)
        {
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }
    }
}
