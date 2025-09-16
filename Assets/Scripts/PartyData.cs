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
            return HeroStats?.Where(h => h.Type == CharacterType.Hero).ToList() ?? new List<CharacterStats>();
        }

        public List<CharacterStats> CheckDeadStatus()
        {
            return HeroStats?.FindAll(h => h.Type == CharacterType.Hero && h.Health > 0) ?? new List<CharacterStats>();
        }

        public CharacterStats FindLowestHealthAlly()
        {
            return HeroStats?.Find(h => h.Type == CharacterType.Hero && h.Health > 0 && h.Health == HeroStats.Where(hs => hs.Type == CharacterType.Hero).Min(hs => hs.Health));
        }

        public CharacterStats[] FindAllies()
        {
            return HeroStats?.Where(h => h.Type == CharacterType.Hero).ToArray() ?? new CharacterStats[0];
        }

        public bool CanHealParty()
        {
            if (HeroStats == null || HeroStats.Count == 0)
            {
                return false;
            }
            return HeroStats.Any(hero =>
                !hero.HasRetreated && hero.Health > 0 &&
                (hero.Health < hero.MaxHealth || hero.Morale < hero.MaxMorale));
        }

        public void GenerateHeroStats(Vector3[] positions)
        {
            HeroStats = HeroStats ?? new List<CharacterStats>(); // Ensure initialization
            HeroStats.Clear(); // Clear existing stats to avoid duplicates
            var positionMap = new Dictionary<int, Vector3>
            {
                { 1, positions[0] },
                { 2, positions[1] },
                { 3, positions[2] },
                { 4, positions[3] }
            };

            foreach (var heroId in HeroIds)
            {
                var data = CharacterLibrary.GetHeroData(heroId);
                int partyPosition = data.PartyPosition;
                if (partyPosition < 1 || partyPosition > 4)
                {
                    Debug.LogWarning($"PartyData: Invalid PartyPosition {partyPosition} for {heroId}, skipping.");
                    continue;
                }
                if (positionMap.ContainsKey(partyPosition))
                {
                    var stats = new CharacterStats(data, positionMap[partyPosition]);
                    if (stats.Rank < 1 || stats.Rank > 3)
                    {
                        Debug.LogWarning($"PartyData: Invalid Rank {stats.Rank} for {heroId}, setting to 1.");
                        stats.Rank = 1;
                    }
                    HeroStats.Add(stats);
                    positionMap.Remove(partyPosition);
                }
                else
                {
                    Debug.LogWarning($"PartyData: PartyPosition {partyPosition} for {heroId} already occupied or invalid.");
                }
            }

            HeroStats = HeroStats.OrderBy(h => CharacterLibrary.GetHeroData(h.Id).PartyPosition).ToList();
        }

        public CharacterStats GetHeroByPosition(int position)
        {
            return HeroStats?.Find(h => CharacterLibrary.GetHeroData(h.Id).PartyPosition == position);
        }
    }
}