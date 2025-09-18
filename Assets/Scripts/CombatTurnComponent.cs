using UnityEngine;

namespace VirulentVentures
{
    public class CombatTurnComponent : MonoBehaviour
    {
        [SerializeField] private CombatConfig combatConfig;
        [SerializeField] private EventBusSO eventBus;
        private int roundNumber;

        void Awake()
        {
            roundNumber = 0;
            if (!ValidateReferences())
            {
                Debug.LogError("CombatTurnComponent: Missing required references. Disabling component.");
                enabled = false;
            }
        }

        void Start()
        {
            eventBus.OnCombatEnded += () => EndCombat(CombatSceneComponent.Instance.ExpeditionManager, CombatSceneComponent.Instance.setupComponent.HeroPositions.Count == 0);
        }

        void OnDestroy()
        {
            eventBus.OnCombatEnded -= () => EndCombat(CombatSceneComponent.Instance.ExpeditionManager, CombatSceneComponent.Instance.setupComponent.HeroPositions.Count == 0);
        }

        public bool CanAttackThisRound(ICombatUnit unit, UnitAttackState state)
        {
            if (unit is not CharacterStats stats) return false;
            if (state.SkipNextAttack)
            {
                state.SkipNextAttack = false;
                return false;
            }
            if (stats.Speed >= combatConfig.SpeedTwoAttacksThreshold)
                return state.AttacksThisRound < 1;
            else if (stats.Speed >= combatConfig.SpeedThreePerTwoThreshold)
                return state.RoundCounter % 2 == 1 ? state.AttacksThisRound < 2 : state.AttacksThisRound < 1;
            else if (stats.Speed >= combatConfig.SpeedOneAttackThreshold)
                return state.AttacksThisRound < 1;
            else if (stats.Speed >= combatConfig.SpeedOnePerTwoThreshold)
                return state.RoundCounter % 2 == 1 && state.AttacksThisRound < 1;
            return false;
        }

        public void IncrementRound()
        {
            roundNumber++;
            string roundMessage = $"Round {roundNumber} begins!";
            CombatSceneComponent.Instance.setupComponent.AllCombatLogs.Add(roundMessage);
            eventBus.RaiseLogMessage(roundMessage, CombatSceneComponent.Instance.UIConfig.TextColor);
        }

        public void EndCombat(ExpeditionManager expeditionManager, bool partyDead)
        {
            string endMessage = "Combat ends!";
            CombatSceneComponent.Instance.setupComponent.AllCombatLogs.Add(endMessage);
            eventBus.RaiseLogMessage(endMessage, CombatSceneComponent.Instance.UIConfig.TextColor);
            expeditionManager.SaveProgress();
            if (!partyDead)
            {
                var expedition = expeditionManager.GetExpedition();
                if (expedition != null && expedition.CurrentNodeIndex < expedition.NodeData.Count)
                {
                    expedition.NodeData[expedition.CurrentNodeIndex].Completed = true;
                    string victoryMessage = "Party victorious!";
                    CombatSceneComponent.Instance.setupComponent.AllCombatLogs.Add(victoryMessage);
                    eventBus.RaiseLogMessage(victoryMessage, Color.green);
                    expeditionManager.TransitionToExpeditionScene();
                }
                else
                {
                    Debug.LogWarning("CombatTurnComponent: Invalid expedition data, cannot mark node as completed.");
                }
            }
            else
            {
                string defeatMessage = "Party defeated!";
                CombatSceneComponent.Instance.setupComponent.AllCombatLogs.Add(defeatMessage);
                eventBus.RaiseLogMessage(defeatMessage, Color.red);
                expeditionManager.TransitionToExpeditionScene();
            }
        }

        private bool ValidateReferences()
        {
            if (combatConfig == null || eventBus == null)
            {
                Debug.LogError($"CombatTurnComponent: Missing references! CombatConfig: {combatConfig != null}, EventBus: {eventBus != null}");
                return false;
            }
            return true;
        }
    }
}