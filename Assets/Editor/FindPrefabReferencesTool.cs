using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class FindPrefabReferencesTool : EditorWindow
{
    private GameObject targetPrefab;
    private Vector2 scrollPos;
    private List<(string path, Object context)> foundReferences = new List<(string, Object)>();

    [MenuItem("Tools/Find Prefab References", priority = 2002)]
    private static void OpenWindow()
    {
        GetWindow<FindPrefabReferencesTool>("Find Prefab References");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("🔍 Поиск ссылок на префаб", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetPrefab = (GameObject)EditorGUILayout.ObjectField("Префаб:", targetPrefab, typeof(GameObject), false);

        if (targetPrefab != null)
        {
            if (GUILayout.Button("Найти все ссылки", GUILayout.Height(30)))
                FindReferences(targetPrefab);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Результаты:", EditorStyles.boldLabel);

        if (foundReferences.Count == 0)
        {
            EditorGUILayout.HelpBox("Ничего не найдено.", MessageType.Info);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var (path, context) in foundReferences)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(context, typeof(Object), true);
            GUILayout.Label(path, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Очистить результаты"))
            foundReferences.Clear();
    }

    private void FindReferences(GameObject prefab)
    {
        foundReferences.Clear();

        string prefabPath = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            EditorUtility.DisplayDialog("Ошибка", "Выбранный объект не является префабом в проекте.", "OK");
            return;
        }

        string[] assetGuids = AssetDatabase.FindAssets("t:Object");
        int count = 0;

        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Пропускаем сам префаб
            if (path == prefabPath)
                continue;

            // Проверяем только сцены, префабы и ScriptableObject
            if (!path.EndsWith(".prefab") && !path.EndsWith(".unity") && !path.EndsWith(".asset"))
                continue;

            // Загружаем как текст и проверяем по GUID
            string fileText = File.ReadAllText(path);
            if (fileText.Contains(guidOf(prefab)))
            {
                Object obj = AssetDatabase.LoadMainAssetAtPath(path);
                foundReferences.Add((path, obj));
                count++;
            }
        }

        Debug.Log($"🔎 Найдено {count} файлов, содержащих ссылки на {prefab.name}.");
    }

    private string guidOf(Object obj)
    {
        string path = AssetDatabase.GetAssetPath(obj);
        return AssetDatabase.AssetPathToGUID(path);
    }
}
