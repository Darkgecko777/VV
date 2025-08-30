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
            }
            string partySaveData = PlayerPrefs.GetString("PartySave", "");
            if (!string.IsNullOrEmpty(partySaveData))
            {
                var wrapper = JsonUtility.FromJson<SaveDataWrapper>(partySaveData);
                if (wrapper != null && wrapper.version == CURRENT_VERSION && wrapper.partyData != null)
                {
                    partyData = wrapper.partyData;
                }
            }
            PostLoad();
        }

        public void OnContinueClicked()
        {
            var expedition = GetExpedition();
            if (expedition.CurrentNodeIndex < expedition.NodeData.Count - 1)
            {
                expedition.CurrentNodeIndex++;
                OnNodeUpdated?.Invoke(expedition.NodeData, expedition.CurrentNodeIndex);
            }
            else
            {
                EndExpedition();
            }
        }

        public void TransitionToExpeditionScene()
        {
            if (isTransitioning) return;
            isTransitioning = true;
            SceneManager.LoadSceneAsync("ExpeditionScene", LoadSceneMode.Single).completed += _ =>
            {
                isTransitioning = false;
                OnSceneTransitionCompleted?.Invoke(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
            };
        }

        public void TransitionToCombatScene()
        {
            if (isTransitioning) return;
            isTransitioning = true;
            SceneManager.LoadSceneAsync("Combat", LoadSceneMode.Single).completed += _ =>
            {
                isTransitioning = false;
                OnCombatStarted?.Invoke();
                OnSceneTransitionCompleted?.Invoke(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
            };
        }

        public void EndExpedition()
        {
            expeditionData.Reset();
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

        public void PostLoad()
        {
            if (partyData == null) return;
            foreach (var hero in partyData.HeroStats)
            {
                var data = CharacterLibrary.GetHeroData(hero.Id);
                hero.AbilityId = data.AbilityIds.Count > 0 ? data.AbilityIds[0] : "BasicAttack";
                hero.Speed = data.Speed;
            }
            foreach (var node in expeditionData.NodeData)
            {
                foreach (var monster in node.Monsters)
                {
                    var data = CharacterLibrary.GetMonsterData(monster.Id);
                    monster.AbilityId = data.AbilityIds.Count > 0 ? data.AbilityIds[0] : "BasicAttack";
                }
            }
        }

        private bool ValidateReferences()
        {
            if (expeditionData == null || partyData == null)
            {
                Debug.LogError($"ExpeditionManager: Missing references! ExpeditionData: {expeditionData != null}, PartyData: {partyData != null}");
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