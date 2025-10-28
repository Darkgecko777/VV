using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    [System.Serializable]
    public class NodeData
    {
        [SerializeField] private List<CharacterStats> monsters;
        [SerializeField] private string nodeType;
        [SerializeField] private string biome;
        [SerializeField] private bool isCombat;
        [SerializeField] private string flavourText;
        [SerializeField] private List<VirusSO> seededViruses;
        [SerializeField] private int challengeRating;
        [SerializeField] private bool completed;
        [SerializeField] private TransmissionVector vector;

        // NEW – stores the actual encounter SO
        [SerializeField] private NonCombatEncounterSO nonCombatEncounter;

        public List<CharacterStats> Monsters => monsters;
        public string NodeType => nodeType;
        public string Biome => biome;
        public bool IsCombat => isCombat;
        public string FlavourText => flavourText;
        public List<VirusSO> SeededViruses => seededViruses;
        public int ChallengeRating => challengeRating;
        public bool Completed { get => completed; set => completed = value; }
        public TransmissionVector Vector => vector;
        public NonCombatEncounterSO NonCombatEncounter => nonCombatEncounter;

        public NodeData(
            List<CharacterStats> monsters,
            string nodeType,
            string biome,
            bool isCombat,
            string flavourText,
            List<VirusSO> seededViruses,
            int challengeRating = 0,
            bool completed = false,
            TransmissionVector vector = TransmissionVector.Health,
            NonCombatEncounterSO encounter = null)
        {
            this.monsters = monsters ?? new List<CharacterStats>();
            this.nodeType = nodeType ?? "NonCombat";
            this.biome = biome ?? "";
            this.isCombat = isCombat;
            this.flavourText = flavourText ?? "";
            this.seededViruses = seededViruses ?? new List<VirusSO>();
            this.challengeRating = challengeRating;
            this.completed = completed;
            this.vector = vector;
            this.nonCombatEncounter = encounter;
        }
    }
}