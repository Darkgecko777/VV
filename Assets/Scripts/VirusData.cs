using UnityEngine;

namespace VirulentVentures
{
    public enum TransmissionVector
    {
        Health, Morale, Buff, Food, Environment, Obstacle
    }

    [CreateAssetMenu(fileName = "VirusData", menuName = "VirulentVentures/VirusData", order = 13)]
    public class VirusData : ScriptableObject
    {
        [SerializeField] private string virusID; // e.g., "BogRot"
        [SerializeField] private TransmissionVector transmissionVector = TransmissionVector.Health; // Default to Health
        [SerializeField] private string effect; // e.g., "HPDrain"
        [SerializeField] private float baseInfectionChance = 0.2f; // Default 20%
        [SerializeField] private float effectStrength = 0.05f; // e.g., 5% HP drain per round
        [SerializeField] private string rarity = "Common"; // e.g., "Common", "Rare", "Epic"

        public string VirusID => virusID;
        public TransmissionVector TransmissionVector => transmissionVector;
        public string Effect => effect;
        public float BaseInfectionChance => baseInfectionChance;
        public float EffectStrength => effectStrength;
        public string Rarity => rarity;
    }
}