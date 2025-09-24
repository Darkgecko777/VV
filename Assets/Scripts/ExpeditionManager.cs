using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VirulentVentures
{
    public class ExpeditionManager : MonoBehaviour
    {
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private PartyData partyData;
        [SerializeField] private PlayerProgress playerProgress;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private UIDocument transitionUIDocument;

        private bool isTransitioning = false;
        private static ExpeditionManager instance;
        private VisualElement fadeOverlay;
        private const float FADE_DURATION = 1f;

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
                fadeOverlay.style.opacity = new StyleFloat(0f);
                fadeOverlay.pickingMode = PickingMode.Ignore;
            }
        }

        void Start()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.ClearProgressOnStart(expeditionData, partyData, playerProgress);
                SaveManager.Instance.LoadProgress(expeditionData, partyData, playerProgress);
                if (expeditionData.Party == null && partyData != null)
                {
                    expeditionData.SetParty(partyData);
                }
                PostLoad();
            }
            else
            {
                Debug.LogError("ExpeditionManager: SaveManager.Instance is null in Start, cannot clear/load progress.");
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
            expeditionData.Party.ResetHeroAbilities();
            StartCoroutine(FadeAndLoad("CombatScene", () =>
            {
                OnCombatStarted?.Invoke();
                OnSceneTransitionCompleted?.Invoke(expeditionData?.NodeData, expeditionData?.CurrentNodeIndex ?? 0);
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
                OnSceneTransitionCompleted?.Invoke(expeditionData?.NodeData, expeditionData?.CurrentNodeIndex ?? 0);
            }));
        }

        public AsyncOperation TransitionToTemplePlanningScene()
        {
            if (isTransitioning || fadeOverlay == null)
            {
                Debug.LogWarning("ExpeditionManager: Transition blocked (already transitioning or no fade overlay)");
                return null;
            }
            isTransitioning = true;
            StartCoroutine(FadeAndLoad("TemplePlanningScene", () =>
            {
                OnSceneTransitionCompleted?.Invoke(null, 0);
            }));
            return CurrentAsyncOp;
        }

        private IEnumerator FadeAndLoad(string sceneName, Action onComplete)
        {
            fadeOverlay.AddToClassList("fade-out");
            yield return new WaitForSeconds(FADE_DURATION);

            CurrentAsyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            yield return CurrentAsyncOp;

            fadeOverlay.RemoveFromClassList("fade-out");
            fadeOverlay.AddToClassList("fade-in");
            isTransitioning = false;
            CurrentAsyncOp = null;

            onComplete?.Invoke();
        }

        public void EndExpedition()
        {
            if (expeditionData != null)
            {
                expeditionData.Reset();
            }
            if (SaveManager.Instance != null && !SaveManager.Instance.ClearDataOnStart)
            {
                playerProgress?.Reset();
            }
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.ClearProgress();
            }
            else
            {
                Debug.LogError("ExpeditionManager: SaveManager.Instance is null, cannot clear progress.");
            }
            TransitionToTemplePlanningScene();
        }

        public void SaveProgress()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveProgress(expeditionData, partyData, playerProgress);
            }
            else
            {
                Debug.LogError("ExpeditionManager: SaveManager.Instance is null, cannot save progress.");
            }
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
            if (expeditionData == null || expeditionData.NodeData == null || expeditionData.Party == null || expeditionData.Party.HeroStats == null)
            {
                Debug.LogWarning("ExpeditionManager: Invalid expedition data in PostLoad, skipping. ExpeditionData: " +
                    (expeditionData != null) + ", NodeData: " + (expeditionData?.NodeData != null) +
                    ", Party: " + (expeditionData?.Party != null) + ", HeroStats: " +
                    (expeditionData?.Party?.HeroStats != null));
                if (expeditionData?.Party != null && expeditionData.Party.HeroStats == null)
                {
                    expeditionData.Party.HeroStats = new List<CharacterStats>();
                    Debug.Log("ExpeditionManager: Initialized expeditionData.Party.HeroStats as empty list.");
                }
                return;
            }

            if (partyData != null && partyData.HeroStats != null)
            {
                partyData.HeroStats = partyData.HeroStats.OrderBy(h => CharacterLibrary.GetHeroData(h.Id).PartyPosition).ToList();
                foreach (var hero in partyData.HeroStats)
                {
                    var data = CharacterLibrary.GetHeroData(hero.Id);
                    if (data != null)
                    {
                        hero.Speed = data.Speed;
                    }
                    else
                    {
                        Debug.LogWarning($"ExpeditionManager: CharacterSO for hero {hero.Id} not found in PostLoad.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("ExpeditionManager: partyData or partyData.HeroStats is null in PostLoad.");
            }

            foreach (var node in expeditionData.NodeData)
            {
                if (node == null || node.Monsters == null) continue;
                foreach (var monster in node.Monsters)
                {
                    var data = CharacterLibrary.GetMonsterData(monster.Id);
                    if (data == null)
                    {
                        Debug.LogWarning($"ExpeditionManager: CharacterSO for monster {monster.Id} not found in PostLoad.");
                    }
                }
            }
        }

        public void OnContinueClicked()
        {
            eventBus?.RaiseContinueClicked();
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