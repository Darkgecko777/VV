using UnityEngine;
using System;
using System.Linq;

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
        [SerializeField] private bool clearDataOnStart = true;
        public bool ClearDataOnStart => clearDataOnStart;
        private static SaveManager instance;
        private const string CURRENT_VERSION = "1.0";

        public static SaveManager Instance => instance;

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
            }
        }

        public void ClearProgressOnStart(ExpeditionData expeditionData, PartyData partyData, PlayerProgress playerProgress)
        {
            if (clearDataOnStart)
            {
                ClearProgress();
                expeditionData.Reset();
                partyData.Reset();
                playerProgress.Reset();
                Debug.Log("SaveManager: Cleared all save data for new session");
            }
        }

        public void SaveProgress(ExpeditionData expeditionData, PartyData partyData, PlayerProgress playerProgress)
        {
            if (partyData != null && partyData.HeroStats != null)
            {
                partyData.HeroStats = partyData.HeroStats.OrderBy(h => CharacterLibrary.GetHeroData(h.Id).PartyPosition).ToList();
            }
            var expeditionWrapper = new SaveDataWrapper(CURRENT_VERSION, expeditionData, null, null);
            PlayerPrefs.SetString("ExpeditionSave", JsonUtility.ToJson(expeditionWrapper));
            var partyWrapper = new SaveDataWrapper(CURRENT_VERSION, null, partyData, null);
            PlayerPrefs.SetString("PartySave", JsonUtility.ToJson(partyWrapper));
            var progressWrapper = new SaveDataWrapper(CURRENT_VERSION, null, null, playerProgress);
            PlayerPrefs.SetString("PlayerProgressSave", JsonUtility.ToJson(progressWrapper));
            PlayerPrefs.Save();
        }

        public void LoadProgress(ExpeditionData expeditionData, PartyData partyData, PlayerProgress playerProgress)
        {
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
                    partyData.HeroStats = wrapper.partyData.HeroStats;
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
        }

        public void ClearProgress()
        {
            PlayerPrefs.DeleteKey("ExpeditionSave");
            PlayerPrefs.DeleteKey("PartySave");
            PlayerPrefs.DeleteKey("PlayerProgressSave");
            PlayerPrefs.Save();
        }
    }
}