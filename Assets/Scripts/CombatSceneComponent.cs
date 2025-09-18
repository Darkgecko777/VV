using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public class CombatSceneComponent : MonoBehaviour
    {
        [SerializeField] public CombatConfig combatConfig;
        [SerializeField] public VisualConfig visualConfig;
        [SerializeField] public UIConfig uiConfig;
        [SerializeField] public EventBusSO eventBus;
        [SerializeField] public CombatEffectsComponent effectsComponent;
        [SerializeField] public Camera combatCamera;
        [SerializeField] public CombatTurnComponent turnComponent;
        [SerializeField] public CombatSetupComponent setupComponent;
        [SerializeField] public CombatLoopComponent loopComponent;
        public ExpeditionManager ExpeditionManager => ExpeditionManager.Instance;
        public static CombatSceneComponent Instance { get; private set; }
        private bool isPaused;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("CombatSceneComponent: Duplicate instance detected, destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            isPaused = false;
            Debug.Log("CombatSceneComponent: Awake completed.");
        }

        void Start()
        {
            if (ExpeditionManager == null)
            {
                Debug.LogError("CombatSceneComponent: ExpeditionManager.Instance not found.");
                return;
            }
            if (!ValidateReferences())
            {
                Debug.LogError("CombatSceneComponent: Validation failed, aborting Start.");
                return;
            }
            eventBus.OnCombatPaused += () => { isPaused = true; Debug.Log("CombatSceneComponent: Combat paused."); };
            eventBus.OnCombatPlayed += () => { isPaused = false; Debug.Log("CombatSceneComponent: Combat resumed."); };
            var expedition = ExpeditionManager.GetExpedition();
            if (expedition == null || expedition.Party == null || expedition.NodeData == null || expedition.CurrentNodeIndex >= expedition.NodeData.Count)
            {
                Debug.LogError("CombatSceneComponent: Invalid expedition data, cannot initialize units.");
                return;
            }
            Debug.Log("CombatSceneComponent: Initializing units...");
            setupComponent.InitializeUnits(expedition.Party.GetHeroes(), expedition.NodeData[expedition.CurrentNodeIndex].Monsters);
            Debug.Log("CombatSceneComponent: Starting combat loop...");
            loopComponent.StartCombatLoop(expedition.Party);
        }

        void OnDestroy()
        {
            eventBus.OnCombatPaused -= () => { isPaused = true; Debug.Log("CombatSceneComponent: Combat paused."); };
            eventBus.OnCombatPlayed -= () => { isPaused = false; Debug.Log("CombatSceneComponent: Combat resumed."); };
            Debug.Log("CombatSceneComponent: Destroyed.");
        }

        public void PauseCombat()
        {
            isPaused = true;
            eventBus.RaiseCombatPaused();
        }

        public void PlayCombat()
        {
            isPaused = false;
            eventBus.RaiseCombatPlayed();
        }

        public void SetCombatSpeed(float speed)
        {
            if (combatConfig != null)
            {
                float oldSpeed = combatConfig.CombatSpeed;
                combatConfig.CombatSpeed = Mathf.Clamp(speed, combatConfig.MinCombatSpeed, combatConfig.MaxCombatSpeed);
                if (oldSpeed != combatConfig.CombatSpeed)
                {
                    string speedMessage = $"Combat speed set to {combatConfig.CombatSpeed:F1}x!";
                    AllCombatLogs.Add(speedMessage);
                    eventBus.RaiseLogMessage(speedMessage, uiConfig.TextColor);
                    eventBus.RaiseCombatSpeedChanged(combatConfig.CombatSpeed);
                }
            }
        }

        public List<string> AllCombatLogs => setupComponent.AllCombatLogs;
        public bool IsPaused => isPaused;
        public EventBusSO EventBus => eventBus;
        public UIConfig UIConfig => uiConfig;

        private bool ValidateReferences()
        {
            if (combatConfig == null)
                Debug.LogError("CombatSceneComponent: combatConfig is null.");
            if (eventBus == null)
                Debug.LogError("CombatSceneComponent: eventBus is null.");
            if (visualConfig == null)
                Debug.LogError("CombatSceneComponent: visualConfig is null.");
            if (uiConfig == null)
                Debug.LogError("CombatSceneComponent: uiConfig is null.");
            if (combatCamera == null)
                Debug.LogError("CombatSceneComponent: combatCamera is null.");
            if (effectsComponent == null)
                Debug.LogError("CombatSceneComponent: effectsComponent is null.");
            if (turnComponent == null)
                Debug.LogError("CombatSceneComponent: turnComponent is null.");
            if (setupComponent == null)
                Debug.LogError("CombatSceneComponent: setupComponent is null.");
            if (loopComponent == null)
                Debug.LogError("CombatSceneComponent: loopComponent is null.");
            if (combatConfig == null || eventBus == null || visualConfig == null || uiConfig == null || combatCamera == null || effectsComponent == null || turnComponent == null || setupComponent == null || loopComponent == null)
                return false;
            Debug.Log("CombatSceneComponent: All references validated.");
            return true;
        }
    }
}