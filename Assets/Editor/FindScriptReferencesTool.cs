using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class FindScriptReferencesTool : EditorWindow
{
    private MonoScript targetScript;
    private Vector2 scrollPos;

    private List<(GameObject obj, string source, string path, string component)> foundObjects = new List<(GameObject, string, string, string)>();

    // Фильтры
    private bool searchInScene = true;
    private bool searchInPrefabs = true;
    private bool includeChildren = true;

    [MenuItem("Tools/Find Script References", priority = 2000)]
    private static void OpenWindow()
    {
        GetWindow<FindScriptReferencesTool>("Find Script References");
    }

    // --- Контекстное меню на скрипте ---
    [MenuItem("Assets/Find Script References", true)]
    private static bool ValidateFindScriptMenu()
    {
        return Selection.activeObject is MonoScript;
    }

    [MenuItem("Assets/Find Script References")]
    private static void FindScriptFromMenu()
    {
        var window = GetWindow<FindScriptReferencesTool>();
        window.targetScript = (MonoScript)Selection.activeObject;
        window.Focus();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("🔍 Поиск объектов и префабов со скриптом", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetScript = (MonoScript)EditorGUILayout.ObjectField("Скрипт:", targetScript, typeof(MonoScript), false);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Фильтры:", EditorStyles.boldLabel);
        searchInScene = EditorGUILayout.Toggle("Искать в сцене", searchInScene);
        searchInPrefabs = EditorGUILayout.Toggle("Искать в префабах", searchInPrefabs);
        includeChildren = EditorGUILayout.Toggle("Искать в дочерних объектах", includeChildren);
        EditorGUILayout.Space(10);

        if (targetScript != null)
        {
            Type scriptClass = targetScript.GetClass();

            if (scriptClass == null)
            {
                EditorGUILayout.HelpBox("Выбранный файл не содержит класс или не компилируется.", MessageType.Warning);
                return;
            }

            if (!typeof(MonoBehaviour).IsAssignableFrom(scriptClass))
            {
                EditorGUILayout.HelpBox("Этот скрипт не является MonoBehaviour и не может висеть на объектах.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("🔎 Найти все ссылки", GUILayout.Height(30)))
            {
                FindAllObjectsWithScript(scriptClass);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Результаты:", EditorStyles.boldLabel);

        if (foundObjects.Count == 0)
        {
            EditorGUILayout.HelpBox("Ничего не найдено.", MessageType.Info);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var (obj, source, path, component) in foundObjects)
        {
            if (obj == null) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
            GUILayout.Label(source, GUILayout.Width(70));
            GUILayout.Label(component, GUILayout.Width(130));
            GUILayout.Label(path, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("👁", GUILayout.Width(28)))
                Selection.activeObject = obj;
            if (GUILayout.Button("📂", GUILayout.Width(28)))
                EditorGUIUtility.PingObject(obj);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(5);
        if (GUILayout.Button("Очистить результаты"))
            foundObjects.Clear();
    }

    private void FindAllObjectsWithScript(Type scriptType)
    {
        foundObjects.Clear();

        try
        {
            EditorUtility.DisplayProgressBar("Поиск скрипта", "Сканирование...", 0f);

            // --- Поиск в активной сцене ---
            if (searchInScene)
            {
                var allSceneObjects = FindObjectsOfType<GameObject>(true);
                int total = allSceneObjects.Length;
                for (int i = 0; i < total; i++)
                {
                    var go = allSceneObjects[i];
                    if (HasComponent(go, scriptType))
                    {
                        foundObjects.Add((go, "Scene", GetFullPath(go.transform), scriptType.Name));
                    }

                    if (i % 100 == 0)
                        EditorUtility.DisplayProgressBar("Поиск в сцене", $"Проверено {i}/{total}", (float)i / total);
                }
            }

            // --- Поиск во всех префабах проекта ---
            if (searchInPrefabs)
            {
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                int total = prefabGuids.Length;
                for (int i = 0; i < total; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;

                    if (HasComponent(prefab, scriptType))
                    {
                        foundObjects.Add((prefab, "Prefab", path, scriptType.Name));
                    }

                    if (i % 100 == 0)
                        EditorUtility.DisplayProgressBar("Поиск в префабах", $"Проверено {i}/{total}", (float)i / total);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"Найдено {foundObjects.Count} объектов со скриптом {scriptType.Name}.");
    }

    private bool HasComponent(GameObject go, Type scriptType)
    {
        if (includeChildren)
            return go.GetComponentInChildren(scriptType, true) != null;
        else
            return go.GetComponent(scriptType) != null;
    }

    private string GetFullPath(Transform t)
    {
        return t == null ? "null" : (t.parent == null ? t.name : $"{GetFullPath(t.parent)}/{t.name}");
    }
}
