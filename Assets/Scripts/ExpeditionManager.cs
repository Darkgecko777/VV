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
        [SerializeField] private MonsterSO ghoulSO;
        [SerializeField] private MonsterSO wraithSO;
        [SerializeField] private List<HeroSO> fallbackHeroes;
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

            if (partyData == null || partyData.HeroStats == null)
            {
                Debug.LogError($"ExpeditionManager.Awake: partyData or HeroStats is null");
            }
            else
            {
                Debug.Log($"ExpeditionManager.Awake: partyData.HeroSOs count: {partyData.HeroSOs.Count}");
            }
        }

        void Start()
        {
            if (expeditionData == null || partyData == null || ghoulSO == null || wraithSO == null)
            {
                Debug.LogError($"ExpeditionManager: Missing references! ExpeditionData: {expeditionData != null}, PartyData: {partyData != null}, GhoulSO: {ghoulSO != null}, WraithSO: {wraithSO != null}");
                return;
            }

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
                    Debug.LogWarning("ExpeditionManager: Invalid or outdated ExpeditionSave version, resetting.");
                    PlayerPrefs.DeleteKey("ExpeditionSave");
                }
            }

            string partySaveData = PlayerPrefs.GetString("PartySave", "");
            if (!string.IsNullOrEmpty(partySaveData) && partyData != null && (partyData.HeroStats == null || partyData.HeroStats.Count == 0))
            {
                var wrapper = JsonUtility.FromJson<SaveDataWrapper>(partySaveData);
                if (wrapper != null && wrapper.version == CURRENT_VERSION && wrapper.partyData != null)
                {
                    partyData.HeroSOs = wrapper.partyData.HeroSOs ?? new List<HeroSO>();
                    partyData.AllowCultist = wrapper.partyData.AllowCultist;
                    partyData.GenerateHeroStats(CharacterPositions.Default().heroPositions);
                }
                else
                {
                    Debug.LogWarning("ExpeditionManager: Invalid or outdated PartySave version, resetting.");
                    PlayerPrefs.DeleteKey("PartySave");
                }
            }

            OnNodeUpdated?.Invoke(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
            ProcessCurrentNode();
        }

        public void GenerateExpedition()
        {
            if (isTransitioning) return;
            expeditionData.Reset();
            partyData.Reset();

            HeroSO[] heroPool = Resources.LoadAll<HeroSO>("SO's/Heroes");
            Debug.Log($"ExpeditionManager.GenerateExpedition: Loaded {heroPool.Length} HeroSOs from Resources/SOs/Heroes: {string.Join(", ", heroPool.Select(h => h.name + " (partyPosition: " + h.PartyPosition + ")"))}");

            List<HeroSO> selectedHeroes;
            if (heroPool.Length < 4)
            {
                Debug.LogError($"ExpeditionManager.GenerateExpedition: Insufficient HeroSOs in Resources/SOs/Heroes, found {heroPool.Length}, need 4. Using fallbackHeroes.");
                selectedHeroes = fallbackHeroes != null && fallbackHeroes.Count >= 4
                    ? fallbackHeroes.Take(4).ToList()
                    : new List<HeroSO>();
                if (selectedHeroes.Count < 4)
                {
                    Debug.LogError($"ExpeditionManager.GenerateExpedition: Fallback heroes insufficient, found {selectedHeroes.Count}, need 4");
                    return;
                }
            }
            else
            {
                selectedHeroes = heroPool.OrderBy(_ => UnityEngine.Random.value).Take(4).ToList();
            }

            partyData.HeroSOs = selectedHeroes;
            expeditionData.SetNodes(new List<NodeData>
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
            });
            expeditionData.SetParty(partyData);
            partyData.GenerateHeroStats(CharacterPositions.Default().heroPositions);
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
            Debug.Log("ExpeditionManager: Transitioning to TemplePlanningScene");
            SceneManager.LoadSceneAsync("TemplePlanningScene").completed += _ =>
            {
                isTransitioning = false;
                OnSceneTransitionCompleted?.Invoke(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
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
            Debug.Log("ExpeditionManager: Transitioning to ExpeditionScene");
            SceneManager.LoadSceneAsync("ExpeditionScene").completed += _ =>
            {
                isTransitioning = false;
                OnSceneTransitionCompleted?.Invoke(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
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
            Debug.Log("ExpeditionManager: Transitioning to BattleScene");
            SceneManager.LoadSceneAsync("BattleScene", LoadSceneMode.Additive).completed += _ =>
            {
                OnCombatStarted?.Invoke();
                isTransitioning = false;
                OnSceneTransitionCompleted?.Invoke(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
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
            Debug.Log("ExpeditionManager: Unloading BattleScene");
            SceneManager.UnloadSceneAsync("BattleScene").completed += _ =>
            {
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
            else
            {
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
    }
}