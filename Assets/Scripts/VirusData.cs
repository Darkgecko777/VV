using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "VirusData", menuName = "VirulentVentures/VirusData", order = 13)]
    public class VirusData : ScriptableObject
    {
        [SerializeField] private string virusID; // e.g., "BogRot"
        [SerializeField] private string transmissionVector; // e.g., "Melee", "Ambient"
        [SerializeField] private string effect; // e.g., "MoraleDrain", "SanityHit"

        public string VirusID => virusID;
        public string TransmissionVector => transmissionVector;
        public string Effect => effect;
    }
}