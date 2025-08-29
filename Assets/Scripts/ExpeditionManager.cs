using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VirulentVentures
{
    [Serializable]
    public class SaveDataWrapper
    {
        public string version;
        public ExpeditionData expeditionData;
        public PartyData partyData;

        public SaveDataWrapper(string version, ExpeditionData expeditionData, PartyData partyData)
        {
            this.version = version;
            this.expeditionData = expeditionData;
            this.partyData = partyData;
        }
    }

    public class ExpeditionManager : MonoBehaviour
    {
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private PartyData partyData;
        [SerializeField] private List<string> fallbackHeroIds = new List<string> { "Fighter", "Healer", "Scout", "TreasureHunter" };
        [SerializeField] private CharacterPositions defaultPositions;
        [SerializeField] private EncounterData combatEncounterData;
        private bool isTransitioning = false;
        private static ExpeditionManager instance;
        private const string CURRENT_VERSION = "1.0";

        public static ExpeditionManager Instance => instance;
        public bool IsTransitioning => isTransitioning;
        public event Action<List<NodeData>, int> OnNodeUpdated;
        public event Action OnExpeditionGenerated;
        public event Action OnCombatStarted;
        public event Action<List<NodeData>, int> OnSceneTransitionCompleted;

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
            if (!ValidateReferences()) return;

            string expeditionSaveData = PlayerPrefs.GetString("ExpeditionSave", "");
            if (!string.IsNullOrEmpty(expeditionSaveData))
            {
                var wrapper = JsonUtility.FromJson<SaveDataWrapper>(expeditionSaveData);
                if (wrapper != null && wrapper.version == CURRENT_VERSION && wrapper.expeditionData != null)
                {
                    expeditionData.SetNodes(wrapper.expeditionData.NodeData);
                    expeditionData.CurrentNodeIndex = wrapper.expeditionData.CurrentNodeIndex;
                    expeditionData.SetParty(wrapper.expeditionData.Party);
                }
                else
                {
                    PlayerPrefs.DeleteKey("ExpeditionSave");
                }
            }

            string partySaveData = PlayerPrefs.GetString("PartySave", "");
            if (!string.IsNullOrEmpty(partySaveData) && partyData != null && (partyData.HeroStats == null || partyData.HeroStats.Count == 0))
            {
                var wrapper = JsonUtility.FromJson<SaveDataWrapper>(partySaveData);
                if (wrapper != null && wrapper.version == CURRENT_VERSION && wrapper.partyData != null)
                {
                    partyData.HeroStats = wrapper.partyData.HeroStats;
                    partyData.PartyID = wrapper.partyData.PartyID;
                }
            }
        }

        void OnDestroy()
        {
            OnNodeUpdated = null;
            OnExpeditionGenerated = null;
            OnCombatStarted = null;
            OnSceneTransitionCompleted = null;
            Debug.Log("ExpeditionManager: Cleared all event subscriptions on destroy");
        }

        public void SetExpedition(List<NodeData> nodes, PartyData party)
        {
            if (nodes == null || nodes.Count == 0 || party == null)
            {
                Debug.LogError("ExpeditionManager: Cannot set expedition with invalid nodes or party!");
                return;
            }

            expeditionData.SetNodes(nodes);
            expeditionData.SetParty(party);
            Debug.Log("ExpeditionManager: Expedition set successfully!");
            OnExpeditionGenerated?.Invoke();
        }

        public void TransitionToExpeditionScene()
        {
            if (isTransitioning)
            {
                return;
            }
            if (!expeditionData.IsValid())
            {
                Debug.LogError("ExpeditionManager: Cannot transition to ExpeditionScene, invalid expedition!");
                return;
            }
            isTransitioning = true;
            SceneManager.LoadSceneAsync("ExpeditionScene", LoadSceneMode.Single).completed += _ =>
            {
                isTransitioning = false;
                OnSceneTransitionCompleted?.Invoke(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
            };
        }

        public void TransitionToBattleScene()
        {
            if (isTransitioning)
            {
                return;
            }
            if (!expeditionData.IsValid() || expeditionData.CurrentNodeIndex >= expeditionData.NodeData.Count)
            {
                Debug.LogError("ExpeditionManager: Cannot transition to BattleScene, invalid expedition or node index!");
                return;
            }
            isTransitioning = true;
            SceneManager.LoadSceneAsync("BattleScene", LoadSceneMode.Single).completed += _ =>
            {
                OnCombatStarted?.Invoke();
                isTransitioning = false;
                OnSceneTransitionCompleted?.Invoke(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
            };
        }

        public void ProcessCurrentNode()
        {
            if (isTransitioning || expeditionData.CurrentNodeIndex >= expeditionData.NodeData.Count)
            {
                EndExpedition();
                return;
            }
            NodeData node = expeditionData.NodeData[expeditionData.CurrentNodeIndex];
            OnNodeUpdated?.Invoke(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
            if (node.IsCombat)
            {
                TransitionToBattleScene();
            }
        }

        public void OnContinueClicked()
        {
            if (isTransitioning) return;
            expeditionData.CurrentNodeIndex++;
            SaveProgress();
            ProcessCurrentNode();
        }

        public void EndExpedition()
        {
            expeditionData.Reset();
            partyData.Reset();
            PlayerPrefs.DeleteKey("ExpeditionSave");
            PlayerPrefs.DeleteKey("PartySave");
            TransitionToTemplePlanningScene();
        }

        public void SaveProgress()
        {
            var expeditionWrapper = new SaveDataWrapper(CURRENT_VERSION, expeditionData, null);
            string expeditionJson = JsonUtility.ToJson(expeditionWrapper);
            PlayerPrefs.SetString("ExpeditionSave", expeditionJson);
            var partyWrapper = new SaveDataWrapper(CURRENT_VERSION, null, partyData);
            string partyJson = JsonUtility.ToJson(partyWrapper);
            PlayerPrefs.SetString("PartySave", partyJson);
            PlayerPrefs.Save();
        }

        public void SetTransitioning(bool state)
        {
            isTransitioning = state;
        }

        public ExpeditionData GetExpedition()
        {
            return expeditionData;
        }

        private bool ValidateReferences()
        {
            if (expeditionData == null || partyData == null || defaultPositions == null || combatEncounterData == null)
            {
                Debug.LogError($"ExpeditionManager: Missing references! ExpeditionData: {expeditionData != null}, PartyData: {partyData != null}, DefaultPositions: {defaultPositions != null}, CombatEncounterData: {combatEncounterData != null}");
                return false;
            }
            return true;
        }

        private void TransitionToTemplePlanningScene()
        {
            if (isTransitioning)
            {
                return;
            }
            isTransitioning = true;
            SceneManager.LoadSceneAsync("TemplePlanning", LoadSceneMode.Single).completed += _ =>
            {
                isTransitioning = false;
                OnSceneTransitionCompleted?.Invoke(null, 0);
            };
        }
    }
}