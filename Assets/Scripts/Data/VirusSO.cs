// <DOCUMENT filename="VirusSO.cs">
using UnityEngine;
using System;

namespace VirulentVentures
{
    public enum TransmissionVector
    {
        Health,
        Morale,
        Buff,
        Food,
        Environment,
        Obstacle
    }

    public enum Biome
    {
        Ruins,
        Swamps,
        Forests
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

        [SerializeField] private string virusID; // e.g., "BogRot"
        [SerializeField] private TransmissionVector transmissionVector = TransmissionVector.Health;
        [SerializeField] private float infectivityModifier = -0.1f;
        [SerializeField] private float effectStrength = 0.05f;
        [SerializeField] private string rarity = "Common";
        [SerializeField] private Color labelColor = Color.red;
        [SerializeField] private Sprite sprite; // Token visual
        [SerializeField] private Modifier modifier; // Stat effect
        [SerializeField] private Biome biome; // Biome association
        [SerializeField] private bool isCrafted; // Natural or crafted

        [Header("Effects")]
        [SerializeField] private EffectSO combatEffect;  // Primary combat effect
        [SerializeField] private EffectSO nonCombatEffect;  // Environmental effect

        public string VirusID => virusID;
        public TransmissionVector TransmissionVector => transmissionVector;
        public float InfectivityModifier => infectivityModifier;
        public float EffectStrength => effectStrength;
        public string Rarity => rarity;
        public Color LabelColor => labelColor;
        public Sprite Sprite => sprite;
        public Modifier VirusModifier => modifier;
        public Biome Biome => biome;
        public bool IsCrafted => isCrafted;
        public EffectSO CombatEffect => combatEffect;
        public EffectSO NonCombatEffect => nonCombatEffect;
    }
}
// </DOCUMENT>