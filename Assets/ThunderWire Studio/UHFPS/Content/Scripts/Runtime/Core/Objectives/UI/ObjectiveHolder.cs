using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using ThunderWire.Attributes;
using TMPro;
using UHFPS.Tools;
using UnityEngine;
using static UHFPS.Runtime.ObjectiveManager;

namespace UHFPS.Runtime
{
    [InspectorHeader("Objective Holder")]
    public class ObjectiveHolder : MonoBehaviour
    {
        [Header("UI Elements")]
        public Transform SubObjectives;
        public TMP_Text ObjectiveTitle;

        private ObjectiveManager manager;

        private readonly CompositeDisposable disposables = new();
        private readonly Dictionary<string, GameObject> subObjectives = new();
        private readonly Dictionary<string, CompositeDisposable> subDisposables = new();

        // Храним последний локализованный шаблон текста для каждой подзадачи
        // (нужно чтобы при изменении count мы могли подставить число в актуальный локализованный шаблон)
        private readonly Dictionary<string, string> lastSubLocalizedText = new();

        private void OnDestroy()
        {
            disposables.Dispose();
            foreach (var d in subDisposables.Values)
                d.Dispose();

            subDisposables.Clear();
            subObjectives.Clear();
            lastSubLocalizedText.Clear();
        }

        /// <summary>
        /// Настройка UI для конкретной цели
        /// </summary>
        public void SetObjective(ObjectiveManager manager, ObjectiveData objectiveData)
        {
            this.manager = manager;

            // 🔹 Основная цель: сразу устанавливаем локализованный текст и подписываемся на изменения
            if (ObjectiveTitle != null && objectiveData.Objective.ObjectiveTitle != null)
            {
                void UpdateObjectiveTitle(string text) => ObjectiveTitle.text = FormatCountVariants(text, 0); // если нужен count в заголовке — подставим 0 по умолчанию

                // Сразу ставим (возможный "key" или уже нормальный текст)
                UpdateObjectiveTitle(objectiveData.Objective.ObjectiveTitle.Value);

                // Подписка на обновления через GString (ObserveText обновит при смене локали)
                objectiveData.Objective.ObjectiveTitle.ObserveText(UpdateObjectiveTitle).AddTo(disposables);

                // Подписка на расширенные локализованные строки с [action] и т.п.
                objectiveData.Objective.ObjectiveTitle.SubscribeGlocMany(UpdateObjectiveTitle);
            }

            // 🔹 Подписка на завершение цели
            objectiveData.IsCompleted.Subscribe(completed =>
            {
                if (completed)
                {
                    disposables.Dispose();
                    Destroy(gameObject);
                }
            }).AddTo(disposables);

            // 🔹 Добавление новых подзадач
            objectiveData.AddSubObjective.Subscribe(data => CreateSubObjective(data)).AddTo(disposables);

            // 🔹 Удаление подзадач
            objectiveData.RemoveSubObjective.Subscribe(RemoveSubObjective).AddTo(disposables);

            // 🔹 Инициализация стартовых подзадач
            foreach (var sub in objectiveData.SubObjectives.Values)
                CreateSubObjective(sub);
        }

        /// <summary>
        /// Создание UI для подзадачи
        /// </summary>
        private void CreateSubObjective(SubObjectiveData data)
        {
            if (manager.SubObjectivePrefab == null || SubObjectives == null)
                return;

            GameObject subObj = Instantiate(manager.SubObjectivePrefab, Vector3.zero, Quaternion.identity, SubObjectives);
            TMP_Text subTitle = subObj.GetComponentInChildren<TMP_Text>();
            if (subTitle == null)
            {
                Debug.LogWarning("TMP_Text не найден в SubObjectivePrefab!");
                Destroy(subObj);
                return;
            }

            data.SubObjectiveObject = subObj;
            CompositeDisposable subDisp = new();
            subDisposables.Add(data.SubObjective.SubObjectiveKey, subDisp);

            // 🔹 Метод обновления текста с локализацией и прогрессом
            void UpdateText(string localizedText)
            {
                // Сохраняем последний локализованный шаблон
                lastSubLocalizedText[data.SubObjective.SubObjectiveKey] = localizedText ?? string.Empty;

                // Подставляем текущий count в шаблон
                subTitle.text = FormatCountVariants(localizedText, data.CompleteCount.Value);
            }

            // 🔹 Подписка на локализацию через GString
            if (data.SubObjective.ObjectiveText != null)
            {
                // Немедленное обновление (может быть ключ или уже норм.текст)
                UpdateText(data.SubObjective.ObjectiveText.Value);

                // Подписка на обновления текста (ObserveText будет вызывать при смене локали)
                data.SubObjective.ObjectiveText.ObserveText(UpdateText).AddTo(subDisp);

                // Подписка на SubscribeGlocMany чтобы GString сам обновлял текст при смене языка / биндов
                data.SubObjective.ObjectiveText.SubscribeGlocMany(UpdateText);
            }

            // 🔹 Подписка на изменение CompleteCount — используем последний локализованный шаблон (lastSubLocalizedText)
            data.CompleteCount.Subscribe(_ =>
            {
                string last = lastSubLocalizedText.TryGetValue(data.SubObjective.SubObjectiveKey, out var v) ? v : data.SubObjective.ObjectiveText?.Value;
                subTitle.text = FormatCountVariants(last, data.CompleteCount.Value);
            }).AddTo(subDisp);

            // 🔹 Подписка на завершение подзадачи
            data.IsCompleted.Subscribe(completed =>
            {
                if (completed)
                {
                    subDisp.Dispose();
                    lastSubLocalizedText.Remove(data.SubObjective.SubObjectiveKey);
                    Destroy(subObj);
                }
            }).AddTo(subDisp);

            subObjectives.Add(data.SubObjective.SubObjectiveKey, subObj);
        }

        /// <summary>
        /// Удаление подзадачи из UI
        /// </summary>
        private void RemoveSubObjective(string key)
        {
            if (subObjectives.TryGetValue(key, out GameObject obj))
            {
                Destroy(obj);
                if (subDisposables.TryGetValue(key, out var disp))
                {
                    disp.Dispose();
                    subDisposables.Remove(key);
                }
                subObjectives.Remove(key);
                lastSubLocalizedText.Remove(key);
            }
        }

        /// <summary>
        /// Форматирует строку, подставляя count во всех популярных форматах плейсхолдера.
        /// Поддерживает: [count], {count}, %count%
        /// </summary>
        private static string FormatCountVariants(string template, ushort count)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;

            // Подставляем в несколько вариантов - чтобы ассеты могли использовать любую нотацию
            string result = template.Replace("{count}", count.ToString());
            result = result.Replace("[count]", count.ToString());
            result = result.Replace("%count%", count.ToString());

            return result;
        }
    }
}
