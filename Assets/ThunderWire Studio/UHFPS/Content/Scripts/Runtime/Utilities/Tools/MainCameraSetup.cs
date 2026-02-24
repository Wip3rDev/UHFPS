using UnityEngine;

namespace UHFPS.Runtime
{
    public class MainCameraSetup : MonoBehaviour
    {
        private void Awake()
        {
            Camera camera = GetComponent<Camera>();
            if (camera != null)
            {
                // Настройки для основной камеры
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.depth = 0;
                camera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI")); // Исключить UI слой
            }
        }
    }
}