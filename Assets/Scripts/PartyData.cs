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
            // Create a mapping of PartyPosition to heroPositions index (1-based to 0-based)
            var positionMap = new Dictionary<int, Vector3>
            {
                { 1, positions[0] }, // e.g., [-1f, 0f, 0f] for position 1
                { 2, positions[1] }, // e.g., [-2f, 0f, 0f] for position 2
                { 3, positions[2] }, // e.g., [-3f, 0f, 0f] for position 3
                { 4, positions[3] }  // e.g., [-4f, 0f, 0f] for position 4
            };

            // Assign heroes to their PartyPosition slots
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
                    var stats = new CharacterStats(heroId, positionMap[partyPosition], CharacterType.Hero);
                    HeroStats.Add(stats);
                    positionMap.Remove(partyPosition); // Ensure position isn’t reused
                }
                else
                {
                    Debug.LogWarning($"PartyData: PartyPosition {partyPosition} for {heroId} already occupied or invalid.");
                }
            }

            // Sort HeroStats by PartyPosition for consistency
            HeroStats = HeroStats.OrderBy(h => CharacterLibrary.GetHeroData(h.Id).PartyPosition).ToList();
        }

        // Helper to get hero by PartyPosition
        public CharacterStats GetHeroByPosition(int position)
        {
            return HeroStats.Find(h => CharacterLibrary.GetHeroData(h.Id).PartyPosition == position);
        }
    }
}