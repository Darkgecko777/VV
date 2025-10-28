using UnityEngine;

namespace VirulentVentures
{
    public enum SkillType
    {
        Speed, Attack, Defense, Evasion, Immunity, Morale, Health
    }

    public enum CheckMode
    {
        Best, Worst, Leader, AllWeakestLink
    }

    [CreateAssetMenu(fileName = "NonCombatEncounterSO", menuName = "VirulentVentures/NonCombatEncounterSO")]
    public class NonCombatEncounterSO : ScriptableObject
    {
        [SerializeField] private string encounterName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private TransmissionVector vector;
        [SerializeField] private SkillType skillType;
        [SerializeField] private CheckMode checkMode;
        [SerializeField] private int difficultyCheck = 10; // DC
        [SerializeField] private string successOutcome; // e.g., "moraleBoost:10;loot:relic"
        [SerializeField] private string failureOutcome; // e.g., "seedVirus:BogRot;healthLoss:5"
        [SerializeField, TextArea] private string successText = "Success!";
        [SerializeField, TextArea] private string failureText = "Failure!";
        // Natural virus seeding (optional per encounter)
        [SerializeField] private VirusSO[] naturalVirusPool;
        [SerializeField, Range(0f, 1f)] private float naturalVirusChance = 0.2f;
        public string FailureText => failureText;
        public string SuccessText => successText;

        public string EncounterName => encounterName;
        public string Description => description;
        public TransmissionVector Vector => vector;
        public SkillType SkillType => skillType;
        public CheckMode CheckMode => checkMode;
        public int DifficultyCheck => difficultyCheck;
        public string SuccessOutcome => successOutcome;
        public string FailureOutcome => failureOutcome;
        public VirusSO[] NaturalVirusPool => naturalVirusPool;
        public float NaturalVirusChance => naturalVirusChance;
    }
}