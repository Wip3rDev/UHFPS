using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimatorAnalyzerTool : EditorWindow
{
    private AnimatorController controller;
    private Vector2 scroll;
    private string reportText = "";
    private bool includeTransitions = true;
    private bool includeParameters = true;

    [MenuItem("Tools/Animator Analyzer")]
    public static void Open()
    {
        GetWindow<AnimatorAnalyzerTool>("Animator Analyzer");
    }

    private void OnGUI()
    {
        GUILayout.Label("🔍 Анализатор AnimatorController", EditorStyles.boldLabel);
        controller = (AnimatorController)EditorGUILayout.ObjectField("Animator Controller", controller, typeof(AnimatorController), false);

        includeParameters = EditorGUILayout.Toggle("Включать параметры", includeParameters);
        includeTransitions = EditorGUILayout.Toggle("Включать переходы", includeTransitions);

        if (controller == null)
        {
            EditorGUILayout.HelpBox("Выбери AnimatorController для анализа.", MessageType.Info);
            return;
        }

        if (GUILayout.Button("Сгенерировать отчёт"))
        {
            reportText = GenerateReport(controller);
        }

        if (!string.IsNullOrEmpty(reportText))
        {
            GUILayout.Space(10);
            if (GUILayout.Button("📋 Копировать текст"))
            {
                EditorGUIUtility.systemCopyBuffer = reportText;
                EditorUtility.DisplayDialog("Скопировано", "Текст отчёта скопирован в буфер обмена!", "OK");
            }

            GUILayout.Space(10);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(reportText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }

    private string GenerateReport(AnimatorController controller)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== Animator Controller: {controller.name} ===");
        sb.AppendLine();

        if (includeParameters)
        {
            sb.AppendLine("🎛 ПАРАМЕТРЫ:");
            foreach (var p in controller.parameters)
            {
                string defaultVal = p.type switch
                {
                    AnimatorControllerParameterType.Float => p.defaultFloat.ToString(),
                    AnimatorControllerParameterType.Int => p.defaultInt.ToString(),
                    AnimatorControllerParameterType.Bool => p.defaultBool.ToString(),
                    AnimatorControllerParameterType.Trigger => "Trigger",
                    _ => "Unknown"
                };
                sb.AppendLine($" - {p.name} ({p.type}) Default: {defaultVal}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("🎭 СЛОИ:");
        foreach (var layer in controller.layers)
        {
            sb.AppendLine($" - Layer: {layer.name}, Weight: {layer.defaultWeight}, Blending: {layer.blendingMode}");
        }
        sb.AppendLine();

        sb.AppendLine("🎬 СОСТОЯНИЯ:");
        foreach (var layer in controller.layers)
        {
            sb.AppendLine($"\n🧩 Layer: {layer.name}");
            AnalyzeStateMachine(layer.stateMachine, sb, "  ");
        }

        return sb.ToString();
    }

    private void AnalyzeStateMachine(AnimatorStateMachine sm, StringBuilder sb, string indent)
    {
        // Состояния
        foreach (var state in sm.states)
        {
            var animState = state.state;
            sb.AppendLine($"{indent}- State: {animState.name}");

            if (animState.motion != null)
                sb.AppendLine($"{indent}  • Motion: {animState.motion.name}");

            sb.AppendLine($"{indent}  • Speed: {animState.speed}");
            sb.AppendLine($"{indent}  • WriteDefaultValues: {animState.writeDefaultValues}");

            // Поведение (StateMachineBehaviour)
            var behaviours = animState.behaviours;
            if (behaviours != null && behaviours.Length > 0)
            {
                sb.AppendLine($"{indent}  • Behaviours:");
                foreach (var b in behaviours)
                    sb.AppendLine($"{indent}     - {b.GetType().Name}");
            }

            if (includeTransitions)
            {
                var transitions = animState.transitions;
                if (transitions != null && transitions.Length > 0)
                {
                    sb.AppendLine($"{indent}  • Transitions:");
                    foreach (var t in transitions)
                    {
                        sb.AppendLine($"{indent}     -> {t.destinationState?.name ?? "(exit)"}");
                        sb.AppendLine($"{indent}        hasExitTime: {t.hasExitTime}");
                        sb.AppendLine($"{indent}        exitTime: {t.exitTime}");
                        sb.AppendLine($"{indent}        duration: {t.duration}");

                        if (t.conditions.Length > 0)
                        {
                            sb.AppendLine($"{indent}        Conditions:");
                            foreach (var c in t.conditions)
                                sb.AppendLine($"{indent}           - {c.parameter} {c.mode} {c.threshold}");
                        }
                    }
                }
            }
        }

        // Подмашины (если есть)
        foreach (var child in sm.stateMachines)
        {
            sb.AppendLine($"\n{indent}📦 Sub-StateMachine: {child.stateMachine.name}");
            AnalyzeStateMachine(child.stateMachine, sb, indent + "  ");
        }
    }
}
