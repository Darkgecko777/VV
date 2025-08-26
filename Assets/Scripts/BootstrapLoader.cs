using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirulentVentures
{
    public class BootstrapLoader : MonoBehaviour
    {
        void Awake()
        {
            // Ensure ExpeditionManager is initialized (singleton handles this)
            if (ExpeditionManager.Instance == null)
            {
                Debug.LogError("BootstrapLoader: ExpeditionManager.Instance is null after bootstrap!");
            }
            else
            {
                Debug.Log("BootstrapLoader: ExpeditionManager initialized successfully.");
            }

            // Load the initial scene (e.g., TemplePlanningScene) single or additive
            SceneManager.LoadSceneAsync("TemplePlanningScene", LoadSceneMode.Single);
        }
    }
}