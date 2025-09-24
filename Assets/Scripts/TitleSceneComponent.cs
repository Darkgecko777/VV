using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace VirulentVentures
{
    public class TitleSceneComponent : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private EventBusSO eventBus;

        private void Awake()
        {
            if (!ValidateReferences()) return;

            VisualElement root = uiDocument.rootVisualElement;
            Button startButton = root.Q<Button>("start-button");
            Button continueButton = root.Q<Button>("continue-button");
            Button exitButton = root.Q<Button>("exit-button");

            startButton?.RegisterCallback<ClickEvent>(evt => OnStartNewClicked());
            continueButton?.RegisterCallback<ClickEvent>(evt => OnContinueClicked());
            exitButton?.RegisterCallback<ClickEvent>(evt => OnExitClicked());
        }

        private void OnStartNewClicked()
        {
            if (ExpeditionManager.Instance != null)
            {
                ExpeditionManager.Instance.EndExpedition(); // Resets data and transitions to TemplePlanningScene
            }
            else
            {
                Debug.LogError("TitleSceneComponent: ExpeditionManager.Instance is null, cannot start new expedition.");
                SceneManager.LoadScene("TemplePlanningScene"); // Fallback
            }
        }

        private void OnContinueClicked()
        {
            if (ExpeditionManager.Instance != null && SaveManager.Instance != null)
            {
                ExpeditionManager.Instance.OnContinueClicked();
                SaveManager.Instance.LoadProgress(
                    ExpeditionManager.Instance.GetExpedition(),
                    ExpeditionManager.Instance.GetExpedition().Party,
                    ExpeditionManager.Instance.GetPlayerProgress()
                );
                // Transition to last scene (assumed TemplePlanningScene for simplicity)
                ExpeditionManager.Instance.TransitionToTemplePlanningScene();
            }
            else
            {
                Debug.LogError("TitleSceneComponent: ExpeditionManager or SaveManager is null, cannot load progress.");
            }
        }

        private void OnExitClicked()
        {
            Application.Quit();
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null)
            {
                Debug.LogError("TitleSceneComponent: UIDocument is not assigned.");
                return false;
            }
            if (eventBus == null)
            {
                Debug.LogWarning("TitleSceneComponent: EventBusSO is not assigned, some events may not trigger.");
            }
            return true;
        }
    }
}