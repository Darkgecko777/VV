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
        [SerializeField] private bool isCombat; // Added to match ExpeditionManager
        [SerializeField] private string flavourText; // Added for non-combat nodes

        public List<MonsterStats> Monsters => monsters;
        public string NodeType => nodeType;
        public string Biome => biome;
        public bool IsCombat => isCombat;
        public string FlavourText => flavourText;

        public NodeData(List<MonsterStats> monsters, string nodeType, string biome, bool isCombat, string flavourText)
        {
            this.monsters = monsters ?? new List<MonsterStats>();
            this.nodeType = nodeType;
            this.biome = biome;
            this.isCombat = isCombat;
            this.flavourText = flavourText;
        }
    }
}