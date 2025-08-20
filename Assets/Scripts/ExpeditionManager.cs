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
        public event Action OnCombatStarted;
        public event Action OnSceneTransitionCompleted; // New event for transition completion

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
            if (isTransitioning) return;
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

        public void TransitionToTemplePlanningScene()
        {
            if (isTransitioning)
            {
                Debug.LogWarning("ExpeditionManager: Already transitioning!");
                return;
            }
            isTransitioning = true;
            SceneManager.LoadSceneAsync("TemplePlanningScene").completed += _ =>
            {
                isTransitioning = false;
                OnSceneTransitionCompleted?.Invoke();
            };
        }

        public void TransitionToExpeditionScene()
        {
            if (isTransitioning)
            {
                Debug.LogWarning("ExpeditionManager: Already transitioning!");
                return;
            }
            if (!expeditionData.IsValid())
            {
                Debug.LogWarning("ExpeditionManager: Cannot transition to ExpeditionScene, invalid expedition data!");
                return;
            }
            isTransitioning = true;
            SceneManager.LoadSceneAsync("ExpeditionScene").completed += _ =>
            {
                isTransitioning = false;
                OnSceneTransitionCompleted?.Invoke();
            };
        }

        public void TransitionToBattleScene()
        {
            if (isTransitioning)
            {
                Debug.LogWarning("ExpeditionManager: Already transitioning!");
                return;
            }
            if (!expeditionData.IsValid() || expeditionData.CurrentNodeIndex >= expeditionData.NodeData.Count)
            {
                Debug.LogWarning("ExpeditionManager: Cannot transition to BattleScene, invalid state!");
                return;
            }
            isTransitioning = true;
            SceneManager.LoadSceneAsync("BattleScene", LoadSceneMode.Additive).completed += _ =>
            {
                OnCombatStarted?.Invoke();
                isTransitioning = false;
                OnSceneTransitionCompleted?.Invoke();
            };
        }

        public void UnloadBattleScene()
        {
            if (isTransitioning)
            {
                Debug.LogWarning("ExpeditionManager: Already transitioning!");
                return;
            }
            isTransitioning = true;
            SceneManager.UnloadSceneAsync("BattleScene").completed += _ =>
            {
                isTransitioning = false;
                OnSceneTransitionCompleted?.Invoke();
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
            else
            {
                // Non-combat node logic (e.g., apply ambient virus effects)
                // For now, auto-advance to next node
                OnContinueClicked();
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

        public void SaveProgress()
        {
            string json = JsonUtility.ToJson(expeditionData);
            PlayerPrefs.SetString("ExpeditionSave", json);
            string partyJson = JsonUtility.ToJson(partyData);
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
    }
}