using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

namespace UHFPS.Runtime
{
    /// <summary>
    /// Простой скрипт для сцены-интро с Video Player.
    /// После окончания видео автоматически загружает главное меню.
    /// </summary>
    public class IntroVideoManager : MonoBehaviour
    {
        [Header("Video Settings")]
        [Tooltip("VideoPlayer компонент на этом объекте или его дочернем объекте")]
        [SerializeField] private VideoPlayer videoPlayer;

        [Tooltip("Имя сцены главного меню для загрузки после видео")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Tooltip("Задержка перед загрузкой меню (в секундах)")]
        [SerializeField] private float delayBeforeLoad = 0f;

        private void Start()
        {
            // Если videoPlayer не назначен, ищем его на этом объекте
            if (videoPlayer == null)
            {
                videoPlayer = GetComponent<VideoPlayer>();
                
                // Если не нашли, ищем в дочерних объектах
                if (videoPlayer == null)
                {
                    videoPlayer = GetComponentInChildren<VideoPlayer>();
                }
            }

            if (videoPlayer != null)
            {
                // Подписываемся на событие окончания видео
                videoPlayer.loopPointReached += OnVideoFinished;
                
                Debug.Log("[IntroVideoManager] Video Player найден и готов к воспроизведению.");
            }
            else
            {
                Debug.LogError("[IntroVideoManager] Video Player не найден! Назначьте его вручную или добавьте компонент Video Player.");
            }
        }

        private void OnDestroy()
        {
            // Отписываемся от события при уничтожении объекта
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= OnVideoFinished;
            }
        }

        private void OnVideoFinished(VideoPlayer vp)
        {
            Debug.Log("[IntroVideoManager] Видео завершено. Загрузка меню: " + mainMenuSceneName);
            
            if (delayBeforeLoad > 0f)
            {
                StartCoroutine(LoadMenuWithDelay());
            }
            else
            {
                LoadMainMenu();
            }
        }

        private System.Collections.IEnumerator LoadMenuWithDelay()
        {
            yield return new WaitForSeconds(delayBeforeLoad);
            LoadMainMenu();
        }

        private void LoadMainMenu()
        {
            // Используем SceneManager для загрузки сцены меню
            SceneManager.LoadScene(mainMenuSceneName);
        }

        /// <summary>
        /// Вызывается из UI кнопки "Пропустить" для пропуска видео
        /// </summary>
        public void SkipIntro()
        {
            Debug.Log("[IntroVideoManager] Пропуск вступления.");
            LoadMainMenu();
        }
    }
}
