// ===== [DeepObjectInspectorTool.cs] =====
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

public class DeepObjectInspectorTool : EditorWindow
{
    private GameObject targetObject;
    private Vector2 scrollPos;

    private enum ScanMode { Full, HierarchyOnly }
    private ScanMode scanMode = ScanMode.Full;

    private bool excludeColliders = true;
    private bool excludeRigidbodies = true;
    private bool excludeMaterials = true;
    private bool excludeTransforms = true;

    private List<string> foundComponents = new List<string>();

    [MenuItem("Tools/Deep Object Inspector")]
    private static void ShowWindow()
    {
        GetWindow<DeepObjectInspectorTool>("Deep Object Inspector");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Целевой объект:", EditorStyles.boldLabel);
        targetObject = (GameObject)EditorGUILayout.ObjectField(targetObject, typeof(GameObject), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Режим сканирования:", EditorStyles.boldLabel);
        scanMode = (ScanMode)EditorGUILayout.EnumPopup("Режим", scanMode);

        EditorGUILayout.Space();

        if (scanMode == ScanMode.Full)
        {
            EditorGUILayout.LabelField("Исключить компоненты:", EditorStyles.boldLabel);
            excludeColliders = EditorGUILayout.Toggle("Исключить Colliders", excludeColliders);
            excludeRigidbodies = EditorGUILayout.Toggle("Исключить Rigidbodies", excludeRigidbodies);
            excludeMaterials = EditorGUILayout.Toggle("Исключить Materials", excludeMaterials);
            excludeTransforms = EditorGUILayout.Toggle("Исключить Transforms", excludeTransforms);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Сканировать объект", GUILayout.Height(30)))
        {
            ScanObject();
        }

        EditorGUILayout.Space();

        if (foundComponents.Count > 0)
        {
            EditorGUILayout.LabelField($"Найдено компонентов: {foundComponents.Count}", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));

            foreach (var comp in foundComponents)
                EditorGUILayout.LabelField(comp);

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Скопировать в буфер", GUILayout.Height(25)))
                CopyToClipboard();
        }
        else
        {
            EditorGUILayout.HelpBox("Нет данных для отображения. Нажми 'Сканировать объект'.", MessageType.Info);
        }
    }

    private void ScanObject()
    {
        if (targetObject == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Не выбран объект!", "OK");
            return;
        }

        foundComponents.Clear();

        List<GameObject> allObjects = new List<GameObject>();
        GetAllChildren(targetObject.transform, allObjects);

        foreach (GameObject go in allObjects)
        {
            string path = GetObjectPath(go.transform);

            if (scanMode == ScanMode.HierarchyOnly)
            {
                foundComponents.Add(path);
                continue;
            }

            Component[] comps = go.GetComponents<Component>();

            foreach (var comp in comps)
            {
                if (comp == null) continue;
                Type type = comp.GetType();

                // Исключения
                if (excludeTransforms && type == typeof(Transform)) continue;
                if (excludeRigidbodies && type == typeof(Rigidbody)) continue;
                if (excludeColliders && typeof(Collider).IsAssignableFrom(type)) continue;
                if (excludeMaterials && type == typeof(MeshRenderer)) continue;

                string compName = type.Name;

                // Если это MonoBehaviour, показать скрипт
                if (typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    MonoScript script = MonoScript.FromMonoBehaviour(comp as MonoBehaviour);
                    if (script != null)
                        compName = $"{script.name}.cs";
                }

                foundComponents.Add($"{path} -> {compName}");
            }
        }

        Repaint();
    }

    private void GetAllChildren(Transform parent, List<GameObject> list)
    {
        list.Add(parent.gameObject);
        foreach (Transform child in parent)
            GetAllChildren(child, list);
    }

    private string GetObjectPath(Transform obj)
    {
        string path = obj.name;
        while (obj.parent != null)
        {
            obj = obj.parent;
            path = obj.name + "/" + path;
        }
        return path;
    }

    private void CopyToClipboard()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var comp in foundComponents)
            sb.AppendLine(comp);
        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        EditorUtility.DisplayDialog("Скопировано", "Данные скопированы в буфер обмена!", "OK");
    }
}
