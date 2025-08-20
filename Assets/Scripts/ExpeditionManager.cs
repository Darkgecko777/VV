using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirulentVentures
{
    public class ExpeditionManager : MonoBehaviour
    {
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private PartyData partyData;
        [SerializeField] private ExpeditionUIManager uiManager;
        private static ExpeditionManager instance;
        private bool isTransitioning = false;

        public static ExpeditionManager Instance => instance;

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        void Start()
        {
            if (expeditionData == null || partyData == null || uiManager == null)
            {
                Debug.LogError($"ExpeditionManager: Missing references! ExpeditionData: {expeditionData != null}, PartyData: {partyData != null}, UIManager: {uiManager != null}");
                return;
            }

            // Load saved data or start fresh
            string saveData = PlayerPrefs.GetString("ExpeditionSave", "");
            if (!string.IsNullOrEmpty(saveData))
            {
                JsonUtility.FromJsonOverwrite(saveData, expeditionData);
            }

            string partySaveData = PlayerPrefs.GetString("PartySave", "");
            if (!string.IsNullOrEmpty(partySaveData))
            {
                JsonUtility.FromJsonOverwrite(partySaveData, partyData);
            }

            uiManager.UpdateNodeUI(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
            ProcessCurrentNode();
        }

        public void ProcessCurrentNode()
        {
            if (expeditionData.CurrentNodeIndex >= expeditionData.NodeData.Count)
            {
                EndExpedition();
                return;
            }

            NodeData node = expeditionData.NodeData[expeditionData.CurrentNodeIndex];
            if (node.IsCombat)
            {
                uiManager.FadeToCombat(() => SceneManager.LoadScene("Combat"));
            }
            else
            {
                uiManager.ShowNonCombatPopout(node.FlavourText, OnContinueClicked);
            }
        }

        private void OnContinueClicked()
        {
            if (isTransitioning) return;
            expeditionData.CurrentNodeIndex++;
            SaveProgress();
            uiManager.UpdateNodeUI(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
            ProcessCurrentNode();
        }

        public void SaveProgress()
        {
            string json = JsonUtility.ToJson(expeditionData);
            PlayerPrefs.SetString("ExpeditionSave", json);
            string partyJson = JsonUtility.ToJson(partyData);
            PlayerPrefs.SetString("PartySave", partyJson);
            PlayerPrefs.Save();
        }

        private void EndExpedition()
        {
            expeditionData.Reset();
            partyData.Reset();
            PlayerPrefs.DeleteKey("ExpeditionSave");
            PlayerPrefs.DeleteKey("PartySave");
            SceneManager.LoadScene("Temple");
        }

        public void SetTransitioning(bool state)
        {
            isTransitioning = state;
        }
    }
}