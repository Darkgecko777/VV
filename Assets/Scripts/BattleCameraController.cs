using UnityEngine;

namespace VirulentVentures
{
    public class BattleCameraController : MonoBehaviour
    {
        [SerializeField] private Camera battleCamera; // Assign in Inspector

        void Awake()
        {
            if (battleCamera == null)
            {
                battleCamera = GetComponent<Camera>();
                if (battleCamera == null)
                {
                    Debug.LogError("BattleCameraController: No Camera found on this GameObject!");
                    return;
                }
            }

            // Ensure this camera is tagged as MainCamera
            battleCamera.tag = "MainCamera";

            // Disable all other cameras
            var allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in allCameras)
            {
                if (cam != battleCamera)
                {
                    cam.enabled = false;
                }
            }
        }

        void OnDestroy()
        {
            // Re-enable other cameras
            var allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in allCameras)
            {
                if (cam != battleCamera)
                {
                    cam.enabled = true;
                }
            }
        }
    }
}