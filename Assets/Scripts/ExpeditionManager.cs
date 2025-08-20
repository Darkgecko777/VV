using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

namespace VirulentVentures
{
    public class ExpeditionManager : MonoBehaviour
    {
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private PartyData partyData;
        [SerializeField] private MonsterSO ghoulSO;
        [SerializeField] private MonsterSO wraithSO;
        private bool isTransitioning = false;
        private static ExpeditionManager instance;

        public static ExpeditionManager Instance => instance;
        public bool IsTransitioning => isTransitioning;
        public event Action<List<NodeData>, int> OnNodeUpdated;
        public event Action OnExpeditionGenerated;
        public event Action OnCombatStarted; // Changed to System.Action

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
            if (expeditionData == null || partyData == null || ghoulSO == null || wraithSO == null)
            {
                Debug.LogError($"ExpeditionManager: Missing references! ExpeditionData: {expeditionData != null}, PartyData: {partyData != null}, GhoulSO: {ghoulSO != null}, WraithSO: {wraithSO != null}");
                return;
            }

            string saveData = PlayerPrefs.GetString("ExpeditionSave", "");
            if (!string.IsNullOrEmpty(saveData))
            {
                var tempData = ScriptableObject.CreateInstance<ExpeditionData>();
                JsonUtility.FromJsonOverwrite(saveData, tempData);
                expeditionData.SetNodes(tempData.NodeData);
                expeditionData.CurrentNodeIndex = tempData.CurrentNodeIndex;
                expeditionData.SetParty(tempData.Party);
            }

            string partySaveData = PlayerPrefs.GetString("PartySave", "");
            if (!string.IsNullOrEmpty(partySaveData))
            {
                JsonUtility.FromJsonOverwrite(partySaveData, partyData);
            }

            OnNodeUpdated?.Invoke(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
            ProcessCurrentNode();
        }

        public void GenerateExpedition()
        {
            expeditionData.Reset();
            partyData.Reset();

            List<NodeData> nodes = new List<NodeData>
            {
                new NodeData(
                    monsters: new List<MonsterStats>(),
                    nodeType: "NonCombat",
                    biome: "Swamp",
                    isCombat: false,
                    flavourText: "A foggy camp with eerie whispers.",
                    seededViruses: new List<VirusData>()
                ),
                new NodeData(
                    monsters: GenerateRandomMonsters(),
                    nodeType: "Combat",
                    biome: "Swamp",
                    isCombat: true,
                    flavourText: "A ghoul-infested ruin.",
                    seededViruses: new List<VirusData>()
                )
            };

            expeditionData.SetNodes(nodes);
            expeditionData.SetParty(partyData);

            partyData.InitializeParty();
            SaveProgress();
            OnExpeditionGenerated?.Invoke();
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
                isTransitioning = true;
                OnCombatStarted?.Invoke();
                SceneManager.LoadScene("BattleScene", LoadSceneMode.Additive);
            }
        }

        public void OnContinueClicked()
        {
            if (isTransitioning) return;
            expeditionData.CurrentNodeIndex++;
            SaveProgress();
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
            isTransitioning = true;
            SceneManager.LoadScene("TemplePlanningScene");
        }

        private List<MonsterStats> GenerateRandomMonsters()
        {
            List<MonsterStats> monsters = new List<MonsterStats>();
            int count = UnityEngine.Random.Range(1, 5);
            for (int i = 0; i < count; i++)
            {
                MonsterSO monsterSO = UnityEngine.Random.value > 0.5f ? ghoulSO : wraithSO;
                MonsterStats stats = new MonsterStats(monsterSO, Vector3.zero);
                monsterSO.ApplyStats(stats);
                monsters.Add(stats);
            }
            return monsters;
        }

        public void SetTransitioning(bool state)
        {
            isTransitioning = state;
        }

        public ExpeditionData GetExpedition()
        {
            return expeditionData;
        }
    }
}