using System.IO;
using System.Text;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ScriptCopierTool : EditorWindow
{
    private string combinedScriptText = "";
    private Vector2 scrollPos;
    private string separator = "\n// ===========================================\n";

    [MenuItem("Tools/Script Copier")]
    public static void ShowWindow()
    {
        GetWindow<ScriptCopierTool>("Script Copier");
    }

    private void OnGUI()
    {
        GUILayout.Label("📋 Скопировать код выбранных C# скриптов", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Выдели один или несколько .cs файлов или папку в Project, затем выбери действие.", MessageType.Info);

        separator = EditorGUILayout.TextField("Разделитель:", separator);

        GUILayout.Space(10);
        if (GUILayout.Button("📄 Показать код в окне", GUILayout.Height(25)))
            ShowSelectedScriptsInWindow();

        if (GUILayout.Button("📋 Скопировать код в буфер обмена", GUILayout.Height(25)))
            CopySelectedScriptsToClipboard();

        if (GUILayout.Button("💾 Сохранить код в файл", GUILayout.Height(25)))
            SaveSelectedScriptsToFile();

        GUILayout.Space(5);

        if (GUILayout.Button("🗂️ Копировать только имена скриптов", GUILayout.Height(25)))
            CopySelectedScriptNames();

        if (GUILayout.Button("🧹 Очистить", GUILayout.Height(25)))
            combinedScriptText = "";

        EditorGUILayout.Space(10);

        if (!string.IsNullOrEmpty(combinedScriptText))
        {
            GUILayout.Label("Результат:", EditorStyles.boldLabel);
            GUIStyle style = new GUIStyle(EditorStyles.textArea)
            {
                font = EditorStyles.miniFont,
                wordWrap = false
            };

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.TextArea(combinedScriptText, style, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }

    // ------------------------------------------------------
    // === Копирование кода выбранных скриптов ===
    // ------------------------------------------------------

    private void CopySelectedScriptsToClipboard()
    {
        string text = GetSelectedScriptsText();
        if (string.IsNullOrEmpty(text))
        {
            EditorUtility.DisplayDialog("Script Copier", "Не выбрано ни одного C# скрипта!", "OK");
            return;
        }

        EditorGUIUtility.systemCopyBuffer = text;
        EditorUtility.DisplayDialog("Script Copier", "✅ Код выбранных скриптов скопирован в буфер обмена!", "OK");
    }

    private void ShowSelectedScriptsInWindow()
    {
        string text = GetSelectedScriptsText();
        if (string.IsNullOrEmpty(text))
        {
            EditorUtility.DisplayDialog("Script Copier", "Не выбрано ни одного C# скрипта!", "OK");
            return;
        }
        combinedScriptText = text;
    }

    private void SaveSelectedScriptsToFile()
    {
        string text = GetSelectedScriptsText();
        if (string.IsNullOrEmpty(text))
        {
            EditorUtility.DisplayDialog("Script Copier", "Нет скриптов для сохранения!", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Сохранить код как...", "", "CombinedScripts.txt", "txt");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, text, Encoding.UTF8);
            EditorUtility.RevealInFinder(path);
        }
    }

    // ------------------------------------------------------
    // === Новая функция: копирование только имен ===
    // ------------------------------------------------------

    private void CopySelectedScriptNames()
    {
        var selectedObjects = Selection.objects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Script Copier", "Не выбрано ни одного скрипта или папки!", "OK");
            return;
        }

        var fileNames = selectedObjects
            .SelectMany(obj =>
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (Directory.Exists(path))
                    return Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
                else if (path.EndsWith(".cs"))
                    return new string[] { path };
                return new string[0];
            })
            .Select(p => Path.GetFileNameWithoutExtension(p))
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        if (fileNames.Count == 0)
        {
            EditorUtility.DisplayDialog("Script Copier", "Не найдено ни одного C# файла!", "OK");
            return;
        }

        string result = string.Join("\n", fileNames);
        EditorGUIUtility.systemCopyBuffer = result;

        EditorUtility.DisplayDialog("Script Copier", $"✅ Скопировано имён: {fileNames.Count}", "OK");
    }

    // ------------------------------------------------------
    // === Вспомогательные методы ===
    // ------------------------------------------------------

    private string GetSelectedScriptsText()
    {
        var selectedObjects = Selection.objects;
        if (selectedObjects == null || selectedObjects.Length == 0)
            return "";

        StringBuilder builder = new StringBuilder();
        int index = 0;

        foreach (var obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (Directory.Exists(path))
            {
                string[] csFiles = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
                foreach (string csPath in csFiles)
                    AddFileToBuilder(csPath, builder, ref index);
            }
            else if (path.EndsWith(".cs"))
            {
                AddFileToBuilder(path, builder, ref index);
            }
        }

        return builder.ToString();
    }

    private void AddFileToBuilder(string path, StringBuilder builder, ref int index)
    {
        try
        {
            string code = File.ReadAllText(path, Encoding.UTF8);
            builder.AppendLine($"// ===== [{++index}] {Path.GetFileName(path)} =====");
            builder.AppendLine(code);
            builder.AppendLine(separator);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка при чтении файла {path}: {ex.Message}");
        }
    }
}
