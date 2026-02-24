using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using System.Text;
using System.Collections.Generic;

public class InputActionViewer : EditorWindow
{
    private InputActionAsset actionsAsset;
    private Vector2 scroll;
    private string displayText = "";
    private Dictionary<string, bool> mapFoldouts = new Dictionary<string, bool>();

    [MenuItem("Tools/Input Action Viewer")]
    public static void ShowWindow()
    {
        GetWindow<InputActionViewer>("Input Action Viewer");
    }

    private void OnGUI()
    {
        GUILayout.Label("📄 Input Actions Inspector", EditorStyles.boldLabel);

        actionsAsset = (InputActionAsset)EditorGUILayout.ObjectField("Input Actions Asset", actionsAsset, typeof(InputActionAsset), false);

        if (actionsAsset == null)
        {
            EditorGUILayout.HelpBox("Перетащи сюда .inputactions (например UIActions)", MessageType.Info);
            return;
        }

        if (GUILayout.Button("📋 Обновить"))
        {
            // Refresh foldouts
            mapFoldouts.Clear();
            foreach (var map in actionsAsset.actionMaps)
            {
                if (!mapFoldouts.ContainsKey(map.name))
                    mapFoldouts[map.name] = true;
            }
        }

        EditorGUILayout.Space(10);

        if (actionsAsset != null)
        {
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            DrawActionMaps();
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawActionMaps()
    {
        foreach (var map in actionsAsset.actionMaps)
        {
            if (!mapFoldouts.ContainsKey(map.name))
                mapFoldouts[map.name] = true;

            mapFoldouts[map.name] = EditorGUILayout.Foldout(mapFoldouts[map.name], $"Action Map: {map.name}", true);

            if (mapFoldouts[map.name])
            {
                EditorGUI.indentLevel++;
                foreach (var action in map.actions)
                {
                    EditorGUILayout.LabelField($"• Action: {action.name}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"  Type: {action.type}");
                    EditorGUILayout.LabelField($"  Expected Control: {action.expectedControlType}");

                    if (action.bindings.Count > 0)
                    {
                        EditorGUILayout.LabelField("  Bindings:");
                        EditorGUI.indentLevel++;
                        foreach (var binding in action.bindings)
                        {
                            if (binding.isComposite)
                                EditorGUILayout.LabelField($"[Composite: {binding.name}]");
                            else if (binding.isPartOfComposite)
                                EditorGUILayout.LabelField($"+ {binding.name} → {binding.effectivePath}");
                            else
                                EditorGUILayout.LabelField($"{binding.effectivePath}");
                        }
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Space();
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}
