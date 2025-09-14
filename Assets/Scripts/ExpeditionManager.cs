using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System;
using System.Collections;
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
        public PlayerProgress playerProgress;

        public SaveDataWrapper(string version, ExpeditionData expeditionData, PartyData partyData, PlayerProgress playerProgress)
        {
            this.version = version;
            this.expeditionData = expeditionData;
            this.partyData = partyData;
            this.playerProgress = playerProgress;
        }
    }

    public class ExpeditionManager : MonoBehaviour
    {
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private PartyData partyData;
        [SerializeField] private PlayerProgress playerProgress;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private UIDocument transitionUIDocument; // Persistent fade overlay
        [SerializeField] private bool clearDataOnStart = true;

        private bool isTransitioning = false;
        private static ExpeditionManager instance;
        private const string CURRENT_VERSION = "1.0";
        private VisualElement fadeOverlay;
        private const float FADE_DURATION = 1f; // Matches USS transition

        public static ExpeditionManager Instance => instance;
        public bool IsTransitioning => isTransitioning;
        public event Action OnCombatStarted;
        public event Action<List<NodeData>, int> OnSceneTransitionCompleted;
        public AsyncOperation CurrentAsyncOp { get; private set; }

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

            if (!ValidateReferences()) return;

            fadeOverlay = transitionUIDocument?.rootVisualElement.Q<VisualElement>("fade-overlay");
            if (fadeOverlay == null)
            {
                Debug.LogError("ExpeditionManager: Missing fade-overlay VisualElement in transitionUIDocument!");
            }
            else
            {
                fadeOverlay.style.opacity = new UnityEngine.UIElements.StyleFloat(0f); // Explicit float to resolve ambiguity
                fadeOverlay.pickingMode = PickingMode.Ignore; // Ignore pointer events to prevent blocking underlying UI
            }

            if (clearDataOnStart)
            {
                PlayerPrefs.DeleteKey("ExpeditionSave");
                PlayerPrefs.DeleteKey("PartySave");
                PlayerPrefs.DeleteKey("PlayerProgressSave");
                PlayerPrefs.Save();
                expeditionData.Reset();
                partyData.Reset();
                playerProgress.Reset();
                Debug.Log("ExpeditionManager: Cleared all save data for new session");
            }
        }

        void Start()
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
        }

        public AsyncOperation TransitionToCombatScene()
        {
            if (isTransitioning || fadeOverlay == null)
            {
                Debug.LogWarning("ExpeditionManager: Transition blocked (already transitioning or no fade overlay)");
                return null;
            }
            isTransitioning = true;
            StartCoroutine(FadeAndLoad("CombatScene", () =>
            {
                OnCombatStarted?.Invoke();
                OnSceneTransitionCompleted?.Invoke(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
            }));
            return CurrentAsyncOp;
        }

        public void TransitionToExpeditionScene()
        {
            if (isTransitioning || fadeOverlay == null)
            {
                Debug.LogWarning("ExpeditionManager: Transition blocked (already transitioning or no fade overlay)");
                return;
            }
            isTransitioning = true;
            StartCoroutine(FadeAndLoad("ExpeditionScene", () =>
            {
                OnSceneTransitionCompleted?.Invoke(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
            }));
        }

        public void TransitionToTemplePlanningScene()
        {
            if (isTransitioning || fadeOverlay == null)
            {
                Debug.LogWarning("ExpeditionManager: Transition blocked (already transitioning or no fade overlay)");
                return;
            }
            isTransitioning = true;
            StartCoroutine(FadeAndLoad("TemplePlanningScene", () =>
            {
                OnSceneTransitionCompleted?.Invoke(null, 0);
            }));
        }

        private IEnumerator FadeAndLoad(string sceneName, Action onComplete)
        {
            // Optional: Block input during fade if needed (uncomment below)
            // if (fadeOverlay != null) fadeOverlay.pickingMode = PickingMode.Position;

            // Fade out (to black)
            fadeOverlay.AddToClassList("fade-out");
            yield return new WaitForSeconds(FADE_DURATION);

            // Start async load
            CurrentAsyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return CurrentAsyncOp;

            // Fade in (to transparent)
            fadeOverlay.RemoveFromClassList("fade-out");
            fadeOverlay.AddToClassList("fade-in");
            isTransitioning = false;
            CurrentAsyncOp = null;

            // Optional: Restore ignoring after fade (uncomment if blocking during transition)
            // if (fadeOverlay != null) fadeOverlay.pickingMode = PickingMode.Ignore;

            // Trigger completion callback
            onComplete?.Invoke();
        }

        public void EndExpedition()
        {
            expeditionData.Reset();
            if (!clearDataOnStart)
            {
                playerProgress.Reset();
            }
            PlayerPrefs.DeleteKey("ExpeditionSave");
            PlayerPrefs.DeleteKey("PlayerProgressSave");
            PlayerPrefs.Save();
            TransitionToTemplePlanningScene();
        }

        public void SaveProgress()
        {
            if (partyData != null && partyData.HeroStats != null)
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
        }

        public void SetTransitioning(bool state)
        {
            isTransitioning = state;
        }

        public ExpeditionData GetExpedition()
        {
            return expeditionData;
        }

        public PlayerProgress GetPlayerProgress()
        {
            return playerProgress;
        }

        public void PostLoad()
        {
            if (partyData == null) return;
            if (partyData.HeroStats != null)
            {
                partyData.HeroStats = partyData.HeroStats.OrderBy(h => CharacterLibrary.GetHeroData(h.Id).PartyPosition).ToList();
            }
            foreach (var hero in partyData.HeroStats)
            {
                var data = CharacterLibrary.GetHeroData(hero.Id);
                hero.Speed = data.Speed;
            }
            foreach (var node in expeditionData.NodeData)
            {
                foreach (var monster in node.Monsters)
                {
                    var data = CharacterLibrary.GetMonsterData(monster.Id);
                }
            }
        }

        public void OnContinueClicked()
        {
            eventBus.RaiseContinueClicked();
        }

        private bool ValidateReferences()
        {
            if (expeditionData == null || partyData == null || playerProgress == null || eventBus == null || transitionUIDocument == null)
            {
                Debug.LogError($"ExpeditionManager: Missing references! ExpeditionData: {expeditionData != null}, PartyData: {partyData != null}, PlayerProgress: {playerProgress != null}, EventBus: {eventBus != null}, TransitionUIDocument: {transitionUIDocument != null}");
                return false;
            }
            return true;
        }
    }
}