using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VirulentVentures
{
    [Serializable]
    public class SaveDataWrapper
    {
        public string version;
        public ExpeditionData expeditionData;
        public PartyData partyData;
        public PlayerProgress playerProgress;

        public SaveDataWrapper(string version, ExpeditionData expeditionData, PartyData partyData, PlayerProgress playerProgress)
        {
            this.version = version;
            this.expeditionData = expeditionData;
            this.partyData = partyData;
            this.playerProgress = playerProgress;
        }
    }

    public class SaveManager : MonoBehaviour
    {
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private PartyData partyData;
        [SerializeField] private PlayerProgress playerProgress;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private bool clearDataOnStart = true;

        private static SaveManager instance;
        private const string CURRENT_VERSION = "1.0";

        public static SaveManager Instance => instance;

        public PlayerProgress GetPlayerProgress()
        {
            return playerProgress;
        }

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

            if (clearDataOnStart)
            {
                PlayerPrefs.DeleteKey("ExpeditionSave");
                PlayerPrefs.DeleteKey("PartySave");
                PlayerPrefs.DeleteKey("PlayerProgressSave");
                PlayerPrefs.Save();
                expeditionData?.Reset();
                partyData?.Reset();
                playerProgress?.Reset();
                Debug.Log("SaveManager: Cleared all save data for new session");
            }
        }

        void Start()
        {
            if (!ValidateReferences()) return;

            // Load saved data
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

            string progressSaveData = PlayerPrefs.GetString("PlayerProgressSave", "");
            if (!string.IsNullOrEmpty(progressSaveData))
            {
                var wrapper = JsonUtility.FromJson<SaveDataWrapper>(progressSaveData);
                if (wrapper != null && wrapper.version == CURRENT_VERSION && wrapper.playerProgress != null)
                {
                    playerProgress = wrapper.playerProgress;
                }
            }

            PostLoad();  // Restore post-load tweaks

            // Subscribe to save triggers
            eventBus.OnNodeUpdated += HandleSaveTrigger;
            eventBus.OnPartyUpdated += HandleSaveTrigger;
            eventBus.OnExpeditionEnded += HandleExpeditionEnded;
        }

        void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.OnNodeUpdated -= HandleSaveTrigger;
                eventBus.OnPartyUpdated -= HandleSaveTrigger;
                eventBus.OnExpeditionEnded -= HandleExpeditionEnded;
            }
        }

        private void HandleSaveTrigger(EventBusSO.NodeUpdateData data) => SaveProgress();
        private void HandleSaveTrigger(PartyData data) => SaveProgress();

        private void HandleExpeditionEnded()
        {
            expeditionData?.Reset();
            if (!clearDataOnStart)
            {
                playerProgress?.Reset();
            }
            PlayerPrefs.DeleteKey("ExpeditionSave");
            PlayerPrefs.DeleteKey("PartySave");
            PlayerPrefs.DeleteKey("PlayerProgressSave");
            PlayerPrefs.Save();
            eventBus.RaiseExpeditionEnded();  // If needed for downstream
        }

        public void SaveProgress()
        {
            if (partyData?.HeroStats != null)
            {
                partyData.HeroStats = partyData.HeroStats.OrderBy(h => CharacterLibrary.GetHeroData(h.Id).PartyPosition).ToList();
            }

            var expeditionWrapper = new SaveDataWrapper(CURRENT_VERSION, expeditionData, null, null);
            string expeditionJson = JsonUtility.ToJson(expeditionWrapper);
            PlayerPrefs.SetString("ExpeditionSave", expeditionJson);

            var partyWrapper = new SaveDataWrapper(CURRENT_VERSION, null, partyData, null);
            string partyJson = JsonUtility.ToJson(partyWrapper);
            PlayerPrefs.SetString("PartySave", partyJson);

            var progressWrapper = new SaveDataWrapper(CURRENT_VERSION, null, null, playerProgress);
            string progressJson = JsonUtility.ToJson(progressWrapper);
            PlayerPrefs.SetString("PlayerProgressSave", progressJson);

            PlayerPrefs.Save();
            Debug.Log("SaveManager: Progress saved");
        }

        public void PostLoad()
        {
            if (partyData == null) return;

            if (partyData.HeroStats != null)
            {
                partyData.HeroStats = partyData.HeroStats
                    .Where(h => h != null)  // Filter nulls post-load
                    .OrderBy(h => {
                        if (h == null) return 0;
                        var data = CharacterLibrary.GetHeroData(h.Id ?? "");  // Guard Id
                return data.PartyPosition;
                    }).ToList();
            }

            if (partyData.HeroStats != null)
            {
                foreach (var hero in partyData.HeroStats)
                {
                    if (hero == null) continue;
                    var data = CharacterLibrary.GetHeroData(hero.Id ?? "");
                    if (data.Id != null) hero.Speed = data.Speed;  // Valid int == int comparison
                                                                   // Add: hero.Health = data.Health; if syncing more
                }
            }

            if (expeditionData?.NodeData != null)
            {
                foreach (var node in expeditionData.NodeData)
                {
                    if (node?.Monsters == null) continue;
                    foreach (var monster in node.Monsters)
                    {
                        if (monster?.Id == null) continue;  // Guard Id string == null
                        var data = CharacterLibrary.GetMonsterData(monster.Id);
                        // e.g., monster.Speed = data.Speed; if syncing
                    }
                }
            }

            Debug.Log("SaveManager: PostLoad completed");
        }

        private bool ValidateReferences()
        {
            if (expeditionData == null || partyData == null || playerProgress == null || eventBus == null)
            {
                Debug.LogError($"SaveManager: Missing references! ExpeditionData: {expeditionData != null}, PartyData: {partyData != null}, PlayerProgress: {playerProgress != null}, EventBus: {eventBus != null}");
                return false;
            }
            return true;
        }
    }
}