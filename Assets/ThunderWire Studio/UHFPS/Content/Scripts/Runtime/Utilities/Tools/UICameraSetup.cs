using UnityEngine;

namespace UHFPS.Runtime
{
    public class UICameraSetup : MonoBehaviour
    {
        private void Awake()
        {
            Camera camera = GetComponent<Camera>();
            if (camera != null)
            {
                // Настройки для UI камеры
                camera.clearFlags = CameraClearFlags.Nothing;
                camera.depth = 1;
                camera.cullingMask = LayerMask.GetMask("UI");
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 1000f;
                camera.orthographic = false;
                camera.fieldOfView = 60f;
            }
        }
    }
}