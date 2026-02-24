// ---------- SafeEventCopyTool.cs ----------
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class SafeEventCopyTool : EditorWindow
{
    private GameObject targetObject;
    private Vector2 scrollPos;
    private string eventData = "";

    [MenuItem("Tools/Safe Event Copy Tool")]
    public static void ShowWindow()
    {
        GetWindow<SafeEventCopyTool>("Safe Event Copy Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Копирование Runtime событий (Safe)", EditorStyles.boldLabel);
        targetObject = (GameObject)EditorGUILayout.ObjectField("Целевой объект", targetObject, typeof(GameObject), true);

        if (GUILayout.Button("Сканировать"))
        {
            if (targetObject)
                eventData = GetAllUnityEventsInfo(targetObject);
            else
                eventData = "Не выбран объект.";
        }

        if (!string.IsNullOrEmpty(eventData))
        {
            EditorGUILayout.Space();
            GUILayout.Label("Результат:", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.TextArea(eventData, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Скопировать в буфер"))
            {
                EditorGUIUtility.systemCopyBuffer = eventData;
                Debug.Log("Event data скопирован в буфер обмена!");
            }
        }
    }

    private string GetAllUnityEventsInfo(GameObject obj)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"=== События объекта: {obj.name} ===\n");

        var components = obj.GetComponents<Component>();
        var visited = new HashSet<object>();

        foreach (var comp in components)
        {
            if (comp == null) continue;

            bool hasAny = false;
            sb.AppendLine($"[{comp.GetType().Name}]");

            SafeFindUnityEvents(comp, comp.GetType(), sb, "  ", visited, ref hasAny);

            if (!hasAny)
                sb.AppendLine("  (Нет событий)");

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void SafeFindUnityEvents(object instance, System.Type type, StringBuilder sb, string indent, HashSet<object> visited, ref bool found)
    {
        if (instance == null) return;

        // Предотвращение рекурсии
        if (visited.Contains(instance)) return;
        visited.Add(instance);

        // Пропуск UnityEngine.Object и наследников (Transform, GameObject и т.д.)
        if (instance is UnityEngine.Object) return;

        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var fields = type.GetFields(flags);

        foreach (var field in fields)
        {
            var value = field.GetValue(instance);
            if (value == null) continue;

            // Если поле - UnityEventBase
            if (value is UnityEventBase unityEvent)
            {
                int count = unityEvent.GetPersistentEventCount();
                if (count > 0)
                {
                    found = true;
                    sb.AppendLine($"{indent}Поле: {field.Name} ({value.GetType().Name})");

                    for (int i = 0; i < count; i++)
                    {
                        var target = unityEvent.GetPersistentTarget(i);
                        var method = unityEvent.GetPersistentMethodName(i);
                        var state = unityEvent.GetPersistentListenerState(i);

                        sb.AppendLine($"{indent}  • Target: {target?.name ?? "None"}");
                        sb.AppendLine($"{indent}    Method: {method}");
                        sb.AppendLine($"{indent}    CallState: {state}");
                        sb.AppendLine();
                    }
                }
            }
            // Если не примитив и не enum и не строка
            else if (!field.FieldType.IsPrimitive && !field.FieldType.IsEnum && field.FieldType != typeof(string))
            {
                // Пропуск UnityEngine.Object
                if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                    continue;

                // Рекурсия для коллекций / массивов
                if (typeof(IEnumerable).IsAssignableFrom(field.FieldType))
                {
                    foreach (var item in (IEnumerable)value)
                    {
                        if (item != null)
                            SafeFindUnityEvents(item, item.GetType(), sb, indent + "  ", visited, ref found);
                    }
                }
                else
                {
                    SafeFindUnityEvents(value, field.FieldType, sb, indent + "  ", visited, ref found);
                }
            }
        }
    }
}
