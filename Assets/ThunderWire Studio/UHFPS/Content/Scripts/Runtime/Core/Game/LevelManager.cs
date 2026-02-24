using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UHFPS.Input;
using UHFPS.Tools;
using UHFPS.Scriptable;
using TMText = TMPro.TMP_Text;
using static UHFPS.Runtime.SaveGameManager;

namespace UHFPS.Runtime
{
    public class LevelManager : MonoBehaviour
    {
        [Serializable]
        public struct LevelInfo
        {
            public string SceneName;
            public GString Title;
            public GString Description;
            public Sprite Background;
        }

        public LevelInfo[] LevelInfos;

        public TMText Title;
        public TMText Description;
        public Image Background;
        public BackgroundFader FadingBackground;

        /// <summary>
        /// AudioSource for menu music - will be stopped when level is loaded.
        /// </summary>
        public AudioSource MenuMusicAudioSource;

        /// <summary>
        /// Priority of background loading thread. <br><see href="https://docs.unity3d.com/ScriptReference/Application-backgroundLoadingPriority.html"></see></br>
        /// </summary>
        public ThreadPriority LoadPriority = ThreadPriority.High;

        public float FadeSpeed;
        public bool SwitchManually;
        public bool FadeBackground;
        public bool Debugging;

        public bool SwitchPanels;
        public float SwitchFadeSpeed;
        public CanvasGroup CurrentPanel;
        public CanvasGroup NewPanel;

        public UnityEvent<float> OnProgressUpdate;
        public UnityEvent OnLoadingDone;

        private void Start()
        {
            Time.timeScale = 1f;
            Application.backgroundLoadingPriority = LoadPriority;

            // Ensure GameLocalization exists in this scene
            if (!GameLocalization.HasReference)
            {
                // Load LocalizationTable from Resources
                var localizationTable = Resources.Load<GameLocaizationTable>("Game/Localization/GameLocalizationTable");
                
                if (localizationTable != null)
                {
                    // Create GameLocalization object with the table
                    GameObject glocObj = new GameObject("GameLocalization");
                    GameLocalization gloc = glocObj.AddComponent<GameLocalization>();
                    gloc.LocalizationTable = localizationTable;
                    // The Awake() will load language from PlayerPrefs automatically
                }
            }

            string sceneName = LoadSceneName;
            if (!string.IsNullOrEmpty(sceneName))
            {
                foreach (var info in LevelInfos)
                {
                    if(info.SceneName == sceneName)
                    {
                        Background.sprite = info.Background;

                        // Try to subscribe to localization, falling back to key if not available
                        if (GameLocalization.HasReference)
                        {
                            info.Title.SubscribeGloc(text => Title.text = text);
                            info.Description.SubscribeGloc(text => Description.text = text);
                        }
                        else
                        {
                            // Localization not available, use key as fallback
                            Title.text = info.Title.GlocText;
                            Description.text = info.Description.GlocText;
                        }
                        break;
                    }
                }

                StartCoroutine(LoadLevelAsync(sceneName));
            }
        }

        private IEnumerator LoadLevelAsync(string sceneName)
        {
            yield return FadingBackground.StartBackgroundFade(true, fadeSpeed: FadeSpeed);
            yield return new WaitForEndOfFrame();

            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;

            while (!asyncOp.isDone)
            {
                float progress = asyncOp.progress / 0.9f;
                OnProgressUpdate?.Invoke(progress);

                if (progress >= 1f) break;
                yield return null;
            }

            yield return DeserializeSavedGame();

            if (SwitchManually)
            {
                OnLoadingDone?.Invoke();

                if (SwitchPanels)
                {
                    yield return CanvasGroupFader.StartFade(CurrentPanel, false, SwitchFadeSpeed);
                    yield return CanvasGroupFader.StartFade(NewPanel, true, SwitchFadeSpeed);
                }

                yield return new WaitUntil(() => InputManager.AnyKeyPressed());

                if (FadeBackground)
                {
                    yield return FadingBackground.StartBackgroundFade(false, fadeSpeed: FadeSpeed);
                    yield return new WaitForEndOfFrame();
                }
            }

            asyncOp.allowSceneActivation = true;
            yield return new WaitForEndOfFrame();

            // Destroy LevelManager after level is loaded to avoid conflicts
            // Stop menu music if it exists
            if (MenuMusicAudioSource != null)
            {
                StartCoroutine(FadeOutAndStopMusic(MenuMusicAudioSource));
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator FadeOutAndStopMusic(AudioSource audioSource)
        {
            float startVolume = audioSource.volume;
            float fadeDuration = 1f;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            audioSource.volume = 0f;
            audioSource.Stop();
            Destroy(gameObject);
        }

        private IEnumerator DeserializeSavedGame()
        {
            if (GameLoadType == LoadType.Normal || string.IsNullOrEmpty(LoadFolderName))
                yield return null;

            // define loading states
            bool loadGameState = GameLoadType == LoadType.LoadGameState;
            bool loadWorldState = GameLoadType == LoadType.LoadWorldState;
            bool loadLastWorld = GameLoadType == LoadType.LoadWorldLast;

            bool loadWorldType = loadWorldState || loadLastWorld;
            bool canLoadWorld = loadWorldType && SaveGameManager.SerializationAsset.PreviousScenePersistency;
            string saveFolder = string.Empty;

            if(loadGameState)
            {
                saveFolder = LoadFolderName;
            }
            else if (canLoadWorld)
            {
                if (LastSceneSaves == null)
                {
                    if (Debugging) Debug.Log("[LevelManager] LastSceneSaves are empty. Trying to load the last scene saves.");
                    {
                        Task getLastScenesTask = LoadLastSceneSaves();
                        yield return new WaitToTaskComplete(getLastScenesTask);
                    }
                    if (Debugging) Debug.Log("[LevelManager] The last scene saves was successfully loaded.");
                }

                LastSceneSaves.TryGetValue(LoadSceneName, out saveFolder);
            }

            if (!string.IsNullOrEmpty(saveFolder))
            {
                if (Debugging) Debug.Log($"[LevelManager] Trying to deserialize a save with the name '{saveFolder}'.");
                {
                    Task deserializeTask = TryDeserializeGameStateAsync(saveFolder);
                    yield return new WaitToTaskComplete(deserializeTask);
                }
                if (Debugging) Debug.Log($"[LevelManager] The save was successfully deserialized. ");
            }
        }
    }
}
