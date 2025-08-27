using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "CombatConfig", menuName = "VirulentVentures/CombatConfig", order = 8)]
    public class CombatConfig : ScriptableObject
    {
        [SerializeField] private float combatSpeed = 1f;
        [SerializeField] private float minCombatSpeed = 0.5f;
        [SerializeField] private float maxCombatSpeed = 2f;
        [SerializeField] private int retreatMoraleThreshold = 20; // Heroes retreat if Morale <= 20
        [SerializeField] private int maxRounds = 5; // Max rounds per battle
        [SerializeField] private int speedTwoAttacksThreshold = 7; // Speed 7-8: 2 attacks/round
        [SerializeField] private int speedThreePerTwoThreshold = 5; // Speed 5-6: 3 attacks/2 rounds
        [SerializeField] private int speedOneAttackThreshold = 3; // Speed 3-4: 1 attack/round
        [SerializeField] private int speedOnePerTwoThreshold = 1; // Speed 1-2: 1 attack/every other round

        public float CombatSpeed { get => combatSpeed; set => combatSpeed = Mathf.Clamp(value, minCombatSpeed, maxCombatSpeed); }
        public float MinCombatSpeed => minCombatSpeed;
        public float MaxCombatSpeed => maxCombatSpeed;
        public int RetreatMoraleThreshold => retreatMoraleThreshold;
        public int MaxRounds => maxRounds;
        public int SpeedTwoAttacksThreshold => speedTwoAttacksThreshold;
        public int SpeedThreePerTwoThreshold => speedThreePerTwoThreshold;
        public int SpeedOneAttackThreshold => speedOneAttackThreshold;
        public int SpeedOnePerTwoThreshold => speedOnePerTwoThreshold;
    }
}