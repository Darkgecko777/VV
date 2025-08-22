using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "PartyData", menuName = "VirulentVentures/PartyData")]
    public class PartyData : ScriptableObject
    {
        public List<HeroSO> HeroSOs;
        public List<HeroStats> HeroStats;
        public bool AllowCultist;

        public void Reset()
        {
            HeroSOs = new List<HeroSO>();
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
            for (int i = 0; i < HeroSOs.Count && i < positions.Length; i++)
            {
                var stats = new HeroStats(HeroSOs[i], positions[i]);
                HeroSOs[i].ApplyStats(stats);
                stats.AbilityId = HeroSOs[i].AbilityIds.Count > 0 ? HeroSOs[i].AbilityIds[0] : "BasicAttack";
                HeroStats.Add(stats);
            }
        }
    }
}