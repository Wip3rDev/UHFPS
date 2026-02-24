using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace UHFPS.Runtime
{
    /// <summary>
    /// Система рандомизации предметов.
    /// При активации задания активирует случайные предметы из списка.
    /// </summary>
    public class ItemSpawner : MonoBehaviour, ISaveable
    {
        [Header("Spawn Settings")]
        [Tooltip("Ключ задания, при активации которого предметы станут активными")]
        public string ObjectiveKey;

        [Tooltip("Количество предметов для активации")]
        public int ItemsToActivate = 3;

        [Header("Items")]
        [Tooltip("Все предметы на сцене (уже расставленные)")]
        public List<GameObject> AllItems;

        [Header("Settings")]
        [Tooltip("Отключать коллайдеры у неактивных предметов")]
        public bool DisableColliders = true;

        [Tooltip("Отключать InteractableItem у неактивных предметов")]
        public bool DisableInteractable = true;

        [Header("Debug")]
        [Tooltip("Показывать в консоли какие предметы активировались")]
        public bool DebugLog = true;

        private List<GameObject> activatedItems = new();
        private bool isInitialized = false;

        private void Awake()
        {
            // Деактивируем все предметы при старте
            DeactivateAllItems();
        }

        private void OnEnable()
        {
            SubscribeToObjectiveEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromObjectiveEvents();
        }

        private void SubscribeToObjectiveEvents()
        {
            // Check if ObjectiveManager is available
            if (!ObjectiveManager.HasReference) return;

            var events = FindObjectsByType<ObjectiveEvent>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var evt in events)
            {
                if (evt.Objective.IsValid && evt.Objective.CompareObj(ObjectiveKey))
                {
                    evt.OnObjectiveAdded.AddListener(OnObjectiveAdded);
                }
            }
        }

        private void UnsubscribeFromObjectiveEvents()
        {
            // Check if ObjectiveManager is available
            if (!ObjectiveManager.HasReference) return;

            var events = FindObjectsByType<ObjectiveEvent>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var evt in events)
            {
                if (evt.Objective.IsValid && evt.Objective.CompareObj(ObjectiveKey))
                {
                    evt.OnObjectiveAdded.RemoveListener(OnObjectiveAdded);
                }
            }
        }

        private void OnObjectiveAdded()
        {
            ActivateRandomItems();
        }

        /// <summary>
        /// Активирует случайные предметы
        /// </summary>
        public void ActivateRandomItems()
        {
            if (AllItems.Count == 0)
            {
                Debug.LogWarning($"[ItemSpawner] Нет предметов в списке на {gameObject.name}");
                return;
            }

            if (isInitialized) return;
            isInitialized = true;

            // Перемешиваем список
            var shuffledItems = AllItems.OrderBy(x => UnityEngine.Random.value).ToList();
            int activateCount = Mathf.Min(ItemsToActivate, shuffledItems.Count);

            for (int i = 0; i < activateCount; i++)
            {
                GameObject item = shuffledItems[i];
                SetItemActive(item, true);
                activatedItems.Add(item);

                if (DebugLog)
                {
                    Debug.Log($"[ItemSpawner] Активирован предмет: {item.name} на позиции {item.transform.position}");
                }
            }

            // Деактивируем остальные
            for (int i = activateCount; i < shuffledItems.Count; i++)
            {
                SetItemActive(shuffledItems[i], false);
            }
        }

        /// <summary>
        /// Включает или выключает предмет
        /// </summary>
        private void SetItemActive(GameObject item, bool active)
        {
            // Включаем/выключаем GameObject
            item.SetActive(active);

            // Опционально отключаем коллайдеры
            if (DisableColliders)
            {
                var colliders = item.GetComponentsInChildren<Collider>();
                foreach (var col in colliders)
                {
                    col.enabled = active;
                }
            }

            // Опционально отключаем InteractableItem
            if (DisableInteractable)
            {
                var interactable = item.GetComponentInChildren<InteractableItem>();
                if (interactable != null)
                {
                    interactable.enabled = active;
                }
            }
        }

        /// <summary>
        /// Деактивирует все предметы
        /// </summary>
        private void DeactivateAllItems()
        {
            foreach (var item in AllItems)
            {
                if (item != null)
                {
                    SetItemActive(item, false);
                }
            }
        }

        /// <summary>
        /// Активирует все предметы (для отладки)
        /// </summary>
        [ContextMenu("Activate All Items")]
        public void DebugActivateAll()
        {
            foreach (var item in AllItems)
            {
                if (item != null)
                {
                    SetItemActive(item, true);
                }
            }
        }

        /// <summary>
        /// Деактивирует все предметы (для отладки)
        /// </summary>
        [ContextMenu("Deactivate All Items")]
        public void DebugDeactivateAll()
        {
            DeactivateAllItems();
        }

        // ========== Система сохранения/загрузки ==========

        public StorableCollection OnSave()
        {
            StorableCollection saveData = new StorableCollection
            {
                { "isInitialized", isInitialized }
            };

            // Сохраняем какие предметы были активированы
            var activatedIndices = new List<int>();
            for (int i = 0; i < AllItems.Count; i++)
            {
                if (AllItems[i] != null && activatedItems.Contains(AllItems[i]))
                {
                    activatedIndices.Add(i);
                }
            }
            saveData.Add("activatedIndices", activatedIndices);

            return saveData;
        }

        public void OnLoad(JToken data)
        {
            bool wasInitialized = (bool)data["isInitialized"];

            if (wasInitialized)
            {
                var activatedIndices = data["activatedIndices"].ToObject<List<int>>();

                // Активируем сохраненные предметы
                foreach (int index in activatedIndices)
                {
                    if (index >= 0 && index < AllItems.Count && AllItems[index] != null)
                    {
                        SetItemActive(AllItems[index], true);
                        activatedItems.Add(AllItems[index]);
                    }
                }

                // Деактивируем остальные
                for (int i = 0; i < AllItems.Count; i++)
                {
                    if (!activatedIndices.Contains(i) && AllItems[i] != null)
                    {
                        SetItemActive(AllItems[i], false);
                    }
                }

                isInitialized = true;
            }
        }
    }
}
