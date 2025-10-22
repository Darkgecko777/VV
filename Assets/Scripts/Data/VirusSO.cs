using UnityEngine;
using System;

namespace VirulentVentures
{
    public enum TransmissionVector
    {
        Health,
        Morale,
        Environment,
        Obstacle
    }

    public enum VirusRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic
    }

    public enum Biome
    {
        Ruins,
        Swamps,
        Forests,
        Caverns
    }

    [CreateAssetMenu(fileName = "VirusSO", menuName = "VirulentVentures/VirusSO", order = 13)]
    public class VirusSO : ScriptableObject
    {
        [Serializable]
        public struct Modifier
        {
            public string Type; // e.g., "Speed"
            public float Value; // e.g., -1f
        }

        [SerializeField] private string virusID;
        [SerializeField] private string displayName;
        [SerializeField] private TransmissionVector transmissionVector = TransmissionVector.Health;
        [SerializeField] private VirusRarity rarity = VirusRarity.Common;
        [SerializeField] private Color labelColor = Color.red;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Biome biome;
        [SerializeField] private bool isCrafted;

        // REMOVED: [Header("Effects")] combatEffect, nonCombatEffect
        // UPDATED: Removed infectivityModifier

        public string VirusID => virusID;
        public string DisplayName => displayName ?? virusID;
        public TransmissionVector TransmissionVector => transmissionVector;
        public VirusRarity Rarity => rarity;
        public string RarityString => rarity.ToString();
        public Color LabelColor => labelColor;
        public Sprite Sprite => sprite;
        public Biome Biome => biome;
        public bool IsCrafted => isCrafted;
    }
}