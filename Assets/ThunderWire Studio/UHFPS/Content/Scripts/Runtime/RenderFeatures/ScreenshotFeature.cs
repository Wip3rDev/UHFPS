using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace UHFPS.Runtime.Rendering
{
    public class ScreenshotFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering;

        [SerializeField]
        private Vector2Int outputImageSize = new(640, 360);

        public static ScreenshotFeature Instance { get; private set; }
        public ScreenshotPass Pass => scriptablePass;

        private ScreenshotPass scriptablePass;
        private ScreenshotManager screenshotManager;

        public override void Create()
        {
            if (Instance != null)
                return;

            scriptablePass = new ScreenshotPass(renderPassEvent, outputImageSize);
            Instance = this;
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (scriptablePass == null)
                return;

            renderer.EnqueuePass(scriptablePass);
        }

        public void SaveScreenshotToDisk(string path)
        {
            // Lazy initialize manager only when needed
            if (screenshotManager == null)
            {
                GameObject managerGO = new GameObject("ScreenshotManager");
                screenshotManager = managerGO.AddComponent<ScreenshotManager>();
            }

            screenshotManager.QueueScreenshot(path, outputImageSize);
        }

        public class ScreenshotPass : ScriptableRenderPass
        {
            public ScreenshotPass(RenderPassEvent renderPassEvent, Vector2Int imageSize)
            {
                this.renderPassEvent = renderPassEvent;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                // No-op - manager handles screenshot
            }
        }
    }

    public class ScreenshotManager : MonoBehaviour
    {
        private string pendingPath;
        private Vector2Int pendingSize;
        private bool processing = false;

        public void QueueScreenshot(string path, Vector2Int size)
        {
            if (processing)
                return;

            pendingPath = path;
            pendingSize = size;
            StartCoroutine(CaptureScreenshot());
        }

        private IEnumerator CaptureScreenshot()
        {
            processing = true;

            // Find and disable all UI canvases
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            List<Canvas> overlayCanvases = new List<Canvas>();
            
            foreach (var canvas in canvases)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas.enabled = false;
                    overlayCanvases.Add(canvas);
                }
            }

            // Wait for UI to update
            yield return new WaitForEndOfFrame();

            // Capture with ScreenCapture (now UI is hidden)
            ScreenCapture.CaptureScreenshot(pendingPath);
            Debug.Log($"[ScreenshotManager] Screenshot saved: {pendingPath}");

            // Wait a frame for capture to complete
            yield return new WaitForEndOfFrame();

            // Re-enable UI
            foreach (var canvas in overlayCanvases)
            {
                if (canvas != null)
                    canvas.enabled = true;
            }

            pendingPath = null;
            processing = false;

            // Self-destruct if no longer needed
            Destroy(gameObject);
        }
    }
}
