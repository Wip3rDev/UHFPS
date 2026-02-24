using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OpenURLButton : MonoBehaviour
{
    [Header("URL для открытия при нажатии")]
    [Tooltip("Пример: https://itch.io/username или https://t.me/yourchannel")]
    public string url;

    private Button button;
    private bool isUrlOpened = false;

    void Awake()
    {
        // Получаем компонент Button
        button = GetComponent<Button>();
    }

    void Start()
    {
        if (button != null)
        {
            // Удаляем старые listeners перед добавлением нового
            button.onClick.RemoveAllListeners();
            // Добавляем метод OpenURL в onClick
            button.onClick.AddListener(OpenURL);
        }
        else
        {
            Debug.LogWarning("Не найден компонент Button! Скрипт OpenURLButton не работает.");
        }
    }

    void OnDestroy()
    {
        // Очищаем listeners при уничтожении объекта
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }

    public void OpenURL()
    {
        // Защита от повторных вызовов
        if (isUrlOpened) return;

        if (!string.IsNullOrEmpty(url))
        {
            isUrlOpened = true;
            Application.OpenURL(url);
            Debug.Log("Открыт адрес: " + url);
            
            // Сбрасываем флаг через небольшую задержку
            Invoke(nameof(ResetFlag), 0.5f);
        }
        else
        {
            Debug.LogWarning("URL не задан для кнопки: " + gameObject.name);
        }
    }

    private void ResetFlag()
    {
        isUrlOpened = false;
    }
}
