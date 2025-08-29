using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "PartyData", menuName = "VirulentVentures/PartyData")]
    public class PartyData : ScriptableObject
    {
        [SerializeField] private string partyId;
        public List<string> HeroIds;
        public List<CharacterStats> HeroStats;
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
            HeroStats = new List<CharacterStats>();
            AllowCultist = false;
        }

        public List<CharacterStats> GetHeroes()
        {
            return HeroStats.Where(h => h.Type == CharacterType.Hero).ToList();
        }

        public List<CharacterStats> CheckDeadStatus()
        {
            return HeroStats.FindAll(h => h.Type == CharacterType.Hero && h.Health > 0);
        }

        public CharacterStats FindLowestHealthAlly()
        {
            return HeroStats.Find(h => h.Type == CharacterType.Hero && h.Health > 0 && h.Health == HeroStats.Where(hs => hs.Type == CharacterType.Hero).Min(hs => hs.Health));
        }

        public CharacterStats[] FindAllies()
        {
            return HeroStats.Where(h => h.Type == CharacterType.Hero).ToArray();
        }

        public void GenerateHeroStats(Vector3[] positions)
        {
            HeroStats = new List<CharacterStats>();
            for (int i = 0; i < HeroIds.Count && i < positions.Length; i++)
            {
                var stats = new CharacterStats(HeroIds[i], positions[i], CharacterType.Hero);
                HeroStats.Add(stats);
            }
        }
    }
}