using UnityEngine;
using UnityEngine.UIElements;

namespace VirulentVentures
{
    public class ExpeditionSceneController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private VisualConfig visualConfig;

        // Validate references for scene setup
        void Awake()
        {
            if (!ValidateReferences()) return;
        }

        // Initialize scene and continue node progression
        void Start()
        {
            // Trigger node processing to continue expedition flow
            ExpeditionManager.Instance.ProcessCurrentNode();
        }

        private bool ValidateReferences()
        {
            if (uiDocument == null || expeditionData == null || visualConfig == null)
            {
                Debug.LogError($"ExpeditionSceneController: Missing references! UIDocument: {uiDocument != null}, ExpeditionData: {expeditionData != null}, VisualConfig: {visualConfig != null}");
                return false;
            }
            return true;
        }
    }
}