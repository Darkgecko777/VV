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
                fadeOverlay.style.opacity = new UnityEngine.UIElements.StyleFloat(0f);
                fadeOverlay.pickingMode = PickingMode.Ignore;
            }

            SaveManager.Instance.ClearProgressOnStart(expeditionData, partyData, playerProgress);
        }

        void Start()
        {
            SaveManager.Instance.LoadProgress(expeditionData, partyData, playerProgress);
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
            expeditionData.Reset();
            if (!SaveManager.Instance.ClearDataOnStart)
            {
                playerProgress.Reset();
            }
            SaveManager.Instance.ClearProgress();
            TransitionToTemplePlanningScene();
        }

        public void SaveProgress()
        {
            SaveManager.Instance.SaveProgress(expeditionData, partyData, playerProgress);
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