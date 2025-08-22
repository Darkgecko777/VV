using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "PartyData", menuName = "VirulentVentures/PartyData", order = 12)]
    public class PartyData : ScriptableObject
    {
        private List<HeroSO> heroSOs = new List<HeroSO>(); // Non-serialized
        [SerializeField] public bool AllowCultist = false;
        private List<HeroStats> heroStats = new List<HeroStats>();

        public List<HeroStats> HeroStats => heroStats;
        public List<HeroSO> HeroSOs
        {
            get => heroSOs;
            set => heroSOs = value ?? new List<HeroSO>();
        }

        public void GenerateHeroStats(Vector2[] positions = null)
        {
            heroStats.Clear();

            if (heroSOs == null || heroSOs.Count == 0 || heroSOs.Count > 4)
            {
                Debug.LogError($"PartyData.GenerateHeroStats: Invalid hero count! Got {heroSOs?.Count ?? 0}, expected 1-4");
                return;
            }

            // Filter out null HeroSOs and sort by PartyPosition (ascending: 1=front/left, 7=back/right)
            var sortedHeroes = heroSOs
                .Where(h => h != null)
                .OrderBy(h => h.PartyPosition) // Fixed to PartyPosition
                .ToList();

            if (sortedHeroes.Count != heroSOs.Count)
            {
                Debug.LogError($"PartyData.GenerateHeroStats: Found {heroSOs.Count - sortedHeroes.Count} null HeroSOs, expected none");
            }

            // Convert Vector2[] to Vector3[] (z=0 for screen position)
            Vector3[] positionVectors = positions != null && positions.Length >= sortedHeroes.Count
                ? Array.ConvertAll(positions, p => new Vector3(p.x, p.y, 0))
                : Array.ConvertAll(CharacterPositions.Default().heroPositions, p => new Vector3(p.x, p.y, 0));

            if (positionVectors.Length < sortedHeroes.Count)
            {
                Debug.LogError($"PartyData.GenerateHeroStats: Insufficient positions! Got {positionVectors.Length}, needed {sortedHeroes.Count}");
                return;
            }

            for (int i = 0; i < sortedHeroes.Count; i++)
            {
                if (sortedHeroes[i].Stats == null || sortedHeroes[i].Stats.Type == null)
                {
                    Debug.LogError($"PartyData.GenerateHeroStats: Null Stats or Type for HeroSO at index {i}");
                    continue;
                }

                var heroStat = new HeroStats(sortedHeroes[i], positionVectors[i]);
                if (AllowCultist && sortedHeroes[i].Stats.Type.CanBeCultist && i == sortedHeroes.Count - 1)
                {
                    heroStat.IsCultist = true;
                }
                heroStats.Add(heroStat);
            }

            Debug.Log($"PartyData.GenerateHeroStats: Initialized {heroStats.Count} heroes in order: {string.Join(", ", heroStats.Select(h => h.PartyPosition))}");
        }

        public List<HeroStats> GetHeroes()
        {
            return heroStats;
        }

        public List<HeroStats> CheckDeadStatus()
        {
            return heroStats.FindAll(h => h.Health > 0);
        }

        public void ReplaceWithCultist(int slot, HeroSO cultistSO)
        {
            if (slot < 0 || slot >= heroStats.Count)
            {
                Debug.LogError($"PartyData.ReplaceWithCultist: Invalid slot {slot}");
                return;
            }
            if (cultistSO == null || cultistSO.Stats == null || cultistSO.Stats.Type == null)
            {
                Debug.LogError("PartyData.ReplaceWithCultist: Null or invalid cultistSO");
                return;
            }
            if (!cultistSO.Stats.Type.CanBeCultist)
            {
                Debug.LogError($"PartyData.ReplaceWithCultist: HeroSO type {cultistSO.Stats.Type.Id} cannot be a cultist");
                return;
            }

            var position = heroStats[slot].Position; // Preserve screen position
            heroStats[slot] = new HeroStats(cultistSO, position) { IsCultist = true };
        }

        public HeroStats FindLowestHealthAlly()
        {
            HeroStats lowestAlly = null;
            int lowestHealth = int.MaxValue;
            foreach (var hero in heroStats)
            {
                if (hero.Health > 0 && hero.Health < lowestHealth)
                {
                    lowestAlly = hero;
                    lowestHealth = hero.Health;
                }
            }
            return lowestAlly;
        }

        public HeroStats[] FindAllies()
        {
            return heroStats.FindAll(h => h.Health > 0).ToArray();
        }

        public void Reset()
        {
            heroStats.Clear();
            heroSOs.Clear();
        }

        private void OnValidate()
        {
            if (heroSOs == null || heroSOs.Count == 0)
            {
                Debug.Log($"PartyData.OnValidate: No heroes assigned in {name}, awaiting ExpeditionManager setup");
                return;
            }
            if (heroSOs.Count > 4)
            {
                Debug.LogWarning($"PartyData.OnValidate: Too many heroes ({heroSOs.Count}) in {name}, max is 4");
            }
            else
            {
                var positions = heroSOs.Where(h => h != null).Select(h => h.PartyPosition).ToList();
                if (positions.Any(p => p != 1))
                {
                    var uniquePositions = positions.Distinct().Count();
                    if (uniquePositions < positions.Count)
                    {
                        Debug.LogWarning($"PartyData.OnValidate: Duplicate PartyPosition values in {name}. Please assign unique values (1-7).");
                    }
                }
            }

            for (int i = 0; i < heroSOs.Count; i++)
            {
                if (heroSOs[i] == null)
                {
                    Debug.LogWarning($"PartyData.OnValidate: Null HeroSO at index {i} in {name}");
                }
                else if (heroSOs[i].Stats == null || heroSOs[i].Stats.Type == null)
                {
                    Debug.LogWarning($"PartyData.OnValidate: Invalid Stats or Type for HeroSO at index {i} in {name}");
                }
            }
        }
    }
}