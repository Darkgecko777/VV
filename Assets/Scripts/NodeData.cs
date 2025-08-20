using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    [Serializable]
    public class NodeData
    {
        [SerializeField] private List<MonsterStats> monsters = new List<MonsterStats>();
        [SerializeField] private string nodeType; // e.g., "Combat", "NonCombat"
        [SerializeField] private string biome; // e.g., "Swamp"
        [SerializeField] private bool isCombat;
        [SerializeField] private string flavourText;
        [SerializeField] private List<VirusData> seededViruses = new List<VirusData>(); // Virus placeholder

        public List<MonsterStats> Monsters => monsters;
        public string NodeType => nodeType;
        public string Biome => biome;
        public bool IsCombat => isCombat;
        public string FlavourText => flavourText;
        public List<VirusData> SeededViruses => seededViruses;

        public NodeData(List<MonsterStats> monsters, string nodeType, string biome, bool isCombat, string flavourText, List<VirusData> seededViruses = null)
        {
            this.monsters = monsters ?? new List<MonsterStats>();
            this.nodeType = nodeType;
            this.biome = biome;
            this.isCombat = isCombat;
            this.flavourText = flavourText;
            this.seededViruses = seededViruses ?? new List<VirusData>();
        }
    }
}