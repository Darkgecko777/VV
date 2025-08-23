using UnityEngine;
using UnityEngine.UIElements;

namespace VirulentVentures
{
    public class ExpeditionSceneController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private VisualConfig visualConfig;
        [SerializeField] private UIConfig uiConfig;

        void Awake()
        {
            if (!ValidateReferences()) return;
        }

        void Start()
        {
            // Commented out to prevent auto-progression for debugging
            // ExpeditionManager.Instance.ProcessCurrentNode();
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || expeditionData == null || visualConfig == null || uiConfig == null)
            {
                Debug.LogError($"ExpeditionSceneController: Missing references! UIDocument: {uiDocument != null}, ExpeditionData: {expeditionData != null}, VisualConfig: {visualConfig != null}, UIConfig: {uiConfig != null}");
                return false;
            }
            return true;
        }
    }
}