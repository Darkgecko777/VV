using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "VirusConfigSO", menuName = "VirulentVentures/VirusConfigSO", order = 14)]
    public class VirusConfigSO : ScriptableObject
    {
        [SerializeField] private VirusSO[] viruses;

        public VirusSO GetVirus(string id)
        {
            var virus = viruses.FirstOrDefault(v => v.VirusID == id);
            if (virus == null)
            {
                Debug.LogWarning($"VirusConfigSO: Virus ID {id} not found, returning first available or null.");
                return viruses.FirstOrDefault();
            }
            return virus;
        }

        public VirusSO[] GetViruses()
        {
            return viruses;
        }
    }
}