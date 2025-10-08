using UnityEngine;

namespace VirulentVentures
{
    public enum TransmissionVector
    {
        Health, Morale, Buff, Food, Environment, Obstacle
    }

    [CreateAssetMenu(fileName = "VirusSO", menuName = "VirulentVentures/VirusSO", order = 13)]
    public class VirusSO : ScriptableObject
    {
        [SerializeField] private string virusID; // e.g., "BogRot"
        [SerializeField] private TransmissionVector transmissionVector = TransmissionVector.Health;
        [SerializeField] private string effect; // Placeholder, e.g., "HPDrain"
        [SerializeField] private float baseInfectionChance = -0.1f; // Modifier: negative increases infectivity
        [SerializeField] private float effectStrength = 0.05f; // Placeholder
        [SerializeField] private string rarity = "Common";

        public string VirusID => virusID;
        public TransmissionVector TransmissionVector => transmissionVector;
        public string Effect => effect;
        public float BaseInfectionChance => baseInfectionChance;
        public float EffectStrength => effectStrength;
        public string Rarity => rarity;
    }
}