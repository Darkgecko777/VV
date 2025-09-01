using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    [Serializable]
    public class NodeData
    {
        [SerializeField] private List<CharacterStats> monsters = new List<CharacterStats>();
        [SerializeField] private string nodeType; // e.g., "Combat", "NonCombat"
        [SerializeField] private string biome; // e.g., "Swamp"
        [SerializeField] private bool isCombat;
        [SerializeField] private string flavourText;
        [SerializeField] private List<VirusData> seededViruses = new List<VirusData>();
        [SerializeField] private bool completed; // New field

        public List<CharacterStats> Monsters => monsters;
        public string NodeType => nodeType;
        public string Biome => biome;
        public bool IsCombat => isCombat;
        public string FlavourText => flavourText;
        public List<VirusData> SeededViruses => seededViruses;
        public bool Completed { get => completed; set => completed = value; } // New property

        public NodeData(List<CharacterStats> monsters, string nodeType, string biome, bool isCombat, string flavourText, List<VirusData> seededViruses = null)
        {
            this.monsters = monsters?.Where(m => m is CharacterStats cs && cs.Type == CharacterType.Monster).ToList() ?? new List<CharacterStats>();
            this.nodeType = nodeType;
            this.biome = biome;
            this.isCombat = isCombat;
            this.flavourText = flavourText;
            this.seededViruses = seededViruses ?? new List<VirusData>();
            this.completed = false; // Initialize as not completed
        }
    }
}