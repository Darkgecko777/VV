using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VirulentVentures
{
    public class ExpeditionManager : MonoBehaviour
    {
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private EventBusSO eventBus;

        private bool isTransitioning = false;
        private static ExpeditionManager instance;
        private AsyncOperation currentAsyncOp;

        public static ExpeditionManager Instance => instance;
        public bool IsTransitioning => isTransitioning;
        public AsyncOperation CurrentAsyncOp => currentAsyncOp;

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
        }

        public AsyncOperation TransitionToCombatScene()
        {
            if (isTransitioning) return null;

            isTransitioning = true;
            var asyncOp = SceneManager.LoadSceneAsync("CombatScene", LoadSceneMode.Single);
            currentAsyncOp = asyncOp;
            asyncOp.completed += _ =>
            {
                isTransitioning = false;
                currentAsyncOp = null;
                OnCombatStarted?.Invoke();
                OnSceneTransitionCompleted?.Invoke(expeditionData?.NodeData, expeditionData?.CurrentNodeIndex ?? 0);
            };
            return asyncOp;
        }

        public AsyncOperation TransitionToExpeditionScene()
        {
            if (isTransitioning) return null;

            isTransitioning = true;
            var asyncOp = SceneManager.LoadSceneAsync("ExpeditionScene", LoadSceneMode.Single);
            currentAsyncOp = asyncOp;
            asyncOp.completed += _ =>
            {
                isTransitioning = false;
                currentAsyncOp = null;
                OnSceneTransitionCompleted?.Invoke(expeditionData?.NodeData, expeditionData?.CurrentNodeIndex ?? 0);
            };
            return asyncOp;
        }

        public AsyncOperation TransitionToTemplePlanningScene()
        {
            if (isTransitioning) return null;

            isTransitioning = true;
            var asyncOp = SceneManager.LoadSceneAsync("TemplePlanningScene", LoadSceneMode.Single);
            currentAsyncOp = asyncOp;
            asyncOp.completed += _ =>
            {
                isTransitioning = false;
                currentAsyncOp = null;
                OnSceneTransitionCompleted?.Invoke(null, 0);
            };
            return asyncOp;
        }

        public void EndExpedition()
        {
            eventBus.RaiseExpeditionEnded();
            TransitionToTemplePlanningScene();
        }

        public void OnContinueClicked()
        {
            eventBus.RaiseContinueClicked();
        }

        public ExpeditionData GetExpedition()
        {
            return expeditionData;
        }

        public PlayerProgress GetPlayerProgress()
        {
            return SaveManager.Instance?.GetPlayerProgress();
        }

        public void SetTransitioning(bool state)
        {
            isTransitioning = state;
        }

        private bool ValidateReferences()
        {
            if (expeditionData == null || eventBus == null)
            {
                Debug.LogError($"ExpeditionManager: Missing references! ExpeditionData: {expeditionData != null}, EventBus: {eventBus != null}");
                return false;
            }
            return true;
        }
    }
}