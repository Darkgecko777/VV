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
        [SerializeField] private List<VirusData> seededViruses;
        [SerializeField] private int challengeRating;
        [SerializeField] private bool completed;

        public List<CharacterStats> Monsters => monsters;
        public string NodeType => nodeType;
        public string Biome => biome;
        public bool IsCombat => isCombat;
        public string FlavourText => flavourText;
        public List<VirusData> SeededViruses => seededViruses;
        public int ChallengeRating => challengeRating;
        public bool Completed { get => completed; set => completed = value; }

        public NodeData(List<CharacterStats> monsters, string nodeType, string biome, bool isCombat, string flavourText, List<VirusData> seededViruses, int challengeRating = 0, bool completed = false)
        {
            this.monsters = monsters ?? new List<CharacterStats>();
            this.nodeType = nodeType ?? "NonCombat";
            this.biome = biome ?? "";
            this.isCombat = isCombat;
            this.flavourText = flavourText ?? "";
            this.seededViruses = seededViruses ?? new List<VirusData>();
            this.challengeRating = challengeRating;
            this.completed = completed;
        }
    }
}