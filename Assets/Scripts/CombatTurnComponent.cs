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
            CombatSceneComponent.Instance.allCombatLogs.Add(roundMessage);
            eventBus.RaiseLogMessage(roundMessage, CombatSceneComponent.Instance.uiConfig.TextColor);
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