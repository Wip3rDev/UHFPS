using UnityEditor;
using UnityEngine;
using System.Text;
using System.Reflection;
using UnityEngine.Events;
using System.Collections;

public class ComponentInspectorToText : EditorWindow
{
    private GameObject targetObject;
    private Component targetComponent;
    private Vector2 scrollPos;
    private string outputText = "";

    [MenuItem("Tools/🧩 Component To Text Viewer")]
    public static void OpenWindow()
    {
        GetWindow<ComponentInspectorToText>("Component To Text");
    }

    private void OnGUI()
    {
        GUILayout.Label("Выберите объект и компонент", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetObject = (GameObject)EditorGUILayout.ObjectField("Объект", targetObject, typeof(GameObject), true);

        if (targetObject != null)
        {
            var components = targetObject.GetComponents<Component>();
            string[] componentNames = new string[components.Length];
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    componentNames[i] = "⚠️ (Missing Script)";
                }
                else if (components[i] is MonoBehaviour mb)
                {
                    var script = MonoScript.FromMonoBehaviour(mb);
                    componentNames[i] = script ? $"{script.name}.cs" : components[i].GetType().Name;
                }
                else
                {
                    componentNames[i] = components[i].GetType().Name;
                }
            }

            int currentIndex = targetComponent ? System.Array.IndexOf(components, targetComponent) : -1;
            int newIndex = EditorGUILayout.Popup("Компонент", currentIndex, componentNames);

            if (newIndex >= 0 && newIndex < components.Length)
                targetComponent = components[newIndex];

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            if (targetComponent != null && GUILayout.Button("📄 Сгенерировать выбранный компонент"))
            {
                outputText = GenerateComponentDescription(targetComponent);
            }

            if (GUILayout.Button("📦 Показать все компоненты объекта"))
            {
                outputText = GenerateAllComponentsDescription(targetObject);
            }
            GUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        if (!string.IsNullOrEmpty(outputText))
        {
            GUILayout.Label("Результат:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.TextArea(outputText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("📋 Скопировать в буфер"))
            {
                EditorGUIUtility.systemCopyBuffer = outputText;
                Debug.Log("✅ Содержимое скопировано в буфер обмена!");
            }
        }
    }

    // =======================================================================
    private string GenerateComponentDescription(Component component)
    {
        var sb = new StringBuilder();
        string compName = component.GetType().Name;

        // Если это MonoBehaviour — выводим как скрипт
        if (component is MonoBehaviour mb)
        {
            var script = MonoScript.FromMonoBehaviour(mb);
            if (script != null)
                compName = $"{script.name}.cs";
        }

        sb.AppendLine($"=== COMPONENT: {compName} ===");
        sb.AppendLine($"GameObject: {component.gameObject.name}");
        sb.AppendLine();

        var so = new SerializedObject(component);
        var sp = so.GetIterator();

        sb.AppendLine("🎛 ПОЛЯ (как в инспекторе):");

        bool enterChildren = true;
        while (sp.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (sp.name == "m_Script") continue;
            sb.AppendLine($" - {sp.displayName} ({sp.propertyType}) = {GetSerializedPropertyValue(sp, component)}");
        }

        return sb.ToString();
    }

    private string GenerateAllComponentsDescription(GameObject obj)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== ОБЪЕКТ: {obj.name} ===");
        sb.AppendLine();

        var components = obj.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null)
            {
                sb.AppendLine("⚠️ Missing Script");
                continue;
            }

            sb.AppendLine(GenerateComponentDescription(comp));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // =======================================================================
    private string GetSerializedPropertyValue(SerializedProperty property, Component component)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer: return property.intValue.ToString();
            case SerializedPropertyType.Boolean: return property.boolValue.ToString();
            case SerializedPropertyType.Float: return property.floatValue.ToString("F3");
            case SerializedPropertyType.String: return $"\"{property.stringValue}\"";
            case SerializedPropertyType.ObjectReference:
                return property.objectReferenceValue ? property.objectReferenceValue.name : "null";
            case SerializedPropertyType.Enum:
                return property.enumValueIndex >= 0 && property.enumValueIndex < property.enumDisplayNames.Length
                    ? property.enumDisplayNames[property.enumValueIndex]
                    : property.enumValueIndex.ToString();
            case SerializedPropertyType.Vector2: return property.vector2Value.ToString();
            case SerializedPropertyType.Vector3: return property.vector3Value.ToString();
            case SerializedPropertyType.Vector4: return property.vector4Value.ToString();
            case SerializedPropertyType.Color: return property.colorValue.ToString();
            case SerializedPropertyType.Quaternion: return property.quaternionValue.eulerAngles.ToString();
            case SerializedPropertyType.Rect: return property.rectValue.ToString();
            case SerializedPropertyType.Bounds: return property.boundsValue.ToString();
            case SerializedPropertyType.AnimationCurve: return $"Curve ({property.animationCurveValue.length} keys)";
            case SerializedPropertyType.ArraySize:
                return $"Array ({property.intValue} элементов)";
            case SerializedPropertyType.Generic:
                return HandleGenericProperty(property, component);
            default: return "(тип не поддерживается)";
        }
    }

    // ----------------------------------------------------------------------
    private string HandleGenericProperty(SerializedProperty property, Component component)
    {
        // Попробуем обработать массивы, списки и UnityEvent
        FieldInfo field = component.GetType().GetField(property.name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (field == null)
            return "(Generic, поле не найдено)";

        object val = field.GetValue(component);
        if (val == null)
            return "null";

        // UnityEvent
        if (val is UnityEventBase unityEvent)
            return UnityEventToStringFull(unityEvent);

        // Списки и массивы
        if (val is IEnumerable enumerable && !(val is string))
        {
            var sb = new StringBuilder();
            sb.AppendLine();

            int index = 0;
            foreach (var item in enumerable)
            {
                string itemStr = item != null ? item.ToString() : "null";
                sb.AppendLine($"    [{index}] = {itemStr}");
                index++;
            }

            if (index == 0)
                return "(Пустая коллекция)";
            return sb.ToString();
        }

        // Вложенные объекты (структуры / классы)
        var nestedType = val.GetType();
        if (!nestedType.IsPrimitive && nestedType != typeof(string))
        {
            var fields = nestedType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var sb = new StringBuilder();
            sb.AppendLine();
            foreach (var f in fields)
            {
                try
                {
                    object fv = f.GetValue(val);
                    sb.AppendLine($"    {f.Name} = {fv}");
                }
                catch { sb.AppendLine($"    {f.Name} = (недоступно)"); }
            }
            return sb.ToString();
        }

        return val.ToString();
    }

    // ----------------------------------------------------------------------
    private string UnityEventToStringFull(UnityEventBase unityEvent)
    {
        var sb = new StringBuilder();
        int count = unityEvent.GetPersistentEventCount();
        if (count == 0)
            return "(UnityEvent пуст)";

        sb.AppendLine();

        var baseType = typeof(UnityEventBase);
        var persistentCallsField = baseType.GetField("m_PersistentCalls", BindingFlags.NonPublic | BindingFlags.Instance);
        var persistentCalls = persistentCallsField?.GetValue(unityEvent);

        var persistentCallType = persistentCalls?.GetType();
        var callsProp = persistentCallType?.GetProperty("List", BindingFlags.Public | BindingFlags.Instance);
        var callList = callsProp?.GetValue(persistentCalls) as System.Collections.IList;

        for (int i = 0; i < count; i++)
        {
            var target = unityEvent.GetPersistentTarget(i);
            var method = unityEvent.GetPersistentMethodName(i);

            string mode = "RuntimeOnly";
            if (callList != null && i < callList.Count)
            {
                var call = callList[i];
                var callModeField = call.GetType().GetField("m_CallState", BindingFlags.NonPublic | BindingFlags.Instance);
                if (callModeField != null)
                    mode = callModeField.GetValue(call).ToString();
            }

            string typeName = target != null ? target.GetType().Name : "null";
            string goName = "(none)";
            if (target is Component comp) goName = comp.gameObject.name;
            else if (target is GameObject go) goName = go.name;

            sb.AppendLine($"    {mode} → ({typeName}) ({goName}) -> {method}()");
        }

        return sb.ToString();
    }
}
