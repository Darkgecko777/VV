using UnityEngine;

namespace VirulentVentures
{
    public class AudioListenerController : MonoBehaviour
    {
        private AudioListener audioListener;

        void Awake()
        {
            audioListener = GetComponent<AudioListener>();
            if (audioListener == null)
            {
                Debug.LogError("AudioListenerManager: No AudioListener found on this GameObject!");
                return;
            }

            // Disable all other AudioListeners in the scene
            var allListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (var listener in allListeners)
            {
                if (listener != audioListener)
                {
                    listener.enabled = false;
                }
            }
        }

        void OnDestroy()
        {
            // Re-enable other AudioListeners when this scene is unloaded
            var allListeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (var listener in allListeners)
            {
                if (listener != audioListener)
                {
                    listener.enabled = true;
                }
            }
        }
    }
}