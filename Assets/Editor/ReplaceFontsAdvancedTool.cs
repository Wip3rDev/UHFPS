using UnityEditor;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.IO;

public class ReplaceFontsAdvancedTool : EditorWindow
{
    private TMP_FontAsset targetFont;
    private bool processScene = true;
    private bool processSelection = false;
    private bool processPrefabs = false;

    // История для отката
    private static Dictionary<Object, TMP_FontAsset> previousFonts = new();

    [MenuItem("Tools/Замена шрифтов (расширенная)")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceFontsAdvancedTool>("Замена шрифтов");
    }

    private void OnGUI()
    {
        GUILayout.Label("🔤 Расширенная замена шрифтов", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Новый шрифт", targetFont, typeof(TMP_FontAsset), false);

        GUILayout.Label("Область применения:", EditorStyles.boldLabel);
        processSelection = EditorGUILayout.ToggleLeft("Только выбранные объекты", processSelection);
        processScene = EditorGUILayout.ToggleLeft("Вся сцена", processScene);
        processPrefabs = EditorGUILayout.ToggleLeft("Префабы (в проекте)", processPrefabs);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("🔁 Заменить шрифты", GUILayout.Height(35)))
        {
            if (targetFont == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "Выберите шрифт (TMP_FontAsset)!", "OK");
                return;
            }

            ReplaceFonts();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("↩️ Откатить последние изменения", GUILayout.Height(30)))
        {
            UndoFonts();
        }
    }

    private void ReplaceFonts()
    {
        int changedCount = 0;
        previousFonts.Clear();

        if (processSelection)
            changedCount += ReplaceFontsInSelection();

        if (processScene && !processSelection)
            changedCount += ReplaceFontsInScene();

        if (processPrefabs)
            changedCount += ReplaceFontsInPrefabs();

        EditorUtility.DisplayDialog("Готово", $"Заменено шрифтов: {changedCount}", "OK");
        Debug.Log($"✅ Заменено шрифтов: {changedCount}");
    }

    private void UndoFonts()
    {
        int reverted = 0;

        foreach (var kv in previousFonts)
        {
            if (kv.Key == null) continue;
            Undo.RecordObject(kv.Key, "Undo Font Change");

            if (kv.Key is TMP_Text tmp)
            {
                tmp.font = kv.Value;
                EditorUtility.SetDirty(tmp);
                reverted++;
            }
        }

        previousFonts.Clear();
        EditorUtility.DisplayDialog("Откат выполнен", $"Восстановлено шрифтов: {reverted}", "OK");
        Debug.Log($"↩️ Откат выполнен, восстановлено {reverted} элементов");
    }

    private int ReplaceFontsInScene()
    {
        int count = 0;

        var uiTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (var t in uiTexts)
            if (ReplaceFont(t)) count++;

        var worldTexts = FindObjectsOfType<TextMeshPro>(true);
        foreach (var t in worldTexts)
            if (ReplaceFont(t)) count++;

        return count;
    }

    private int ReplaceFontsInSelection()
    {
        int count = 0;
        foreach (var obj in Selection.gameObjects)
        {
            foreach (var tmp in obj.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (ReplaceFont(tmp)) count++;

            foreach (var tmp in obj.GetComponentsInChildren<TextMeshPro>(true))
                if (ReplaceFont(tmp)) count++;
        }
        return count;
    }

    private int ReplaceFontsInPrefabs()
    {
        int count = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) continue;

            bool changed = false;

            foreach (var tmp in prefab.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (ReplaceFont(tmp)) { count++; changed = true; }

            foreach (var tmp in prefab.GetComponentsInChildren<TextMeshPro>(true))
                if (ReplaceFont(tmp)) { count++; changed = true; }

            if (changed)
                AssetDatabase.SaveAssets();
        }

        return count;
    }

    private bool ReplaceFont(TMP_Text tmp)
    {
        if (tmp == null || tmp.font == targetFont) return false;

        if (!previousFonts.ContainsKey(tmp))
            previousFonts[tmp] = tmp.font;

        Undo.RecordObject(tmp, "Replace Font");
        tmp.font = targetFont;
        EditorUtility.SetDirty(tmp);

        return true;
    }
}
