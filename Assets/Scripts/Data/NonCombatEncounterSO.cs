using UnityEngine;

namespace VirulentVentures
{
    public enum SkillType
    {
        Speed,
        Attack,
        Defense,
        Evasion,
        Immunity,
        Morale,
        Health
    }

    public enum CheckMode
    {
        Best,
        Worst,
        Leader,
        AllWeakestLink
    }

    [CreateAssetMenu(fileName = "NonCombatEncounterSO", menuName = "VirulentVentures/NonCombatEncounterSO")]
    public class NonCombatEncounterSO : ScriptableObject
    {
        [SerializeField] private string encounterName;
        [SerializeField] private string description;
        [SerializeField] private TransmissionVector vector;
        [SerializeField] private SkillType skillType;
        [SerializeField] private CheckMode checkMode;
        [SerializeField] private string successOutcome; // e.g., "moraleBoost:10;loot:relic"
        [SerializeField] private string failureOutcome; // e.g., "seedVirus:BogRot;healthLoss:5;moraleLoss:10"

        public string EncounterName => encounterName;
        public string Description => description;
        public TransmissionVector Vector => vector;
        public SkillType SkillType => skillType;
        public CheckMode CheckMode => checkMode;
        public string SuccessOutcome => successOutcome;
        public string FailureOutcome => failureOutcome;
    }
}