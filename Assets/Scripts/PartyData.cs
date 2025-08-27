using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "PartyData", menuName = "VirulentVentures/PartyData")]
    public class PartyData : ScriptableObject
    {
        [SerializeField] private string partyId; // Added for save/load
        public List<string> HeroIds;
        public List<HeroStats> HeroStats;
        public bool AllowCultist;

        public string PartyID
        {
            get => partyId;
            set => partyId = value;
        }

        public void Reset()
        {
            partyId = "";
            HeroIds = new List<string>();
            HeroStats = new List<HeroStats>();
            AllowCultist = false;
        }

        public List<HeroStats> GetHeroes()
        {
            return HeroStats;
        }

        public List<HeroStats> CheckDeadStatus()
        {
            return HeroStats.FindAll(h => h.Health > 0);
        }

        public HeroStats FindLowestHealthAlly()
        {
            return HeroStats.Find(h => h.Health > 0 && h.Health == HeroStats.Min(hs => hs.Health));
        }

        public HeroStats[] FindAllies()
        {
            return HeroStats.ToArray();
        }

        public void GenerateHeroStats(Vector3[] positions)
        {
            HeroStats = new List<HeroStats>();
            for (int i = 0; i < HeroIds.Count && i < positions.Length; i++)
            {
                var stats = new HeroStats(HeroIds[i], positions[i]);
                HeroStats.Add(stats);
            }
        }
    }
}