using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "PartyData", menuName = "VirulentVentures/PartyData", order = 12)]
    public class PartyData : ScriptableObject
    {
        [SerializeField] private List<HeroSO> heroSOs = new List<HeroSO>();
        [SerializeField] private bool allowCultist = false;
        private List<HeroStats> heroStats = new List<HeroStats>();

        public List<HeroStats> HeroStats => heroStats;

        public void GenerateHeroStats(Vector2[] positions = null)
        {
            heroStats.Clear();

            if (heroSOs == null || heroSOs.Count == 0 || heroSOs.Count > 4)
            {
                Debug.LogError($"PartyData.GenerateHeroStats: Invalid hero count! Got {heroSOs?.Count ?? 0}, expected 1-4");
                return;
            }

            // Convert Vector2[] to Vector3[] (z=0) if provided, else use default
            Vector3[] positionVectors = positions != null && positions.Length >= heroSOs.Count
                ? Array.ConvertAll(positions, p => new Vector3(p.x, p.y, 0))
                : Array.ConvertAll(CharacterPositions.Default().heroPositions, p => new Vector3(p.x, p.y, 0));

            if (positionVectors.Length < heroSOs.Count)
            {
                Debug.LogError($"PartyData.GenerateHeroStats: Insufficient positions! Got {positionVectors.Length}, needed {heroSOs.Count}");
                return;
            }

            for (int i = 0; i < heroSOs.Count; i++)
            {
                if (heroSOs[i] == null || heroSOs[i].Stats == null || heroSOs[i].Stats.Type == null)
                {
                    Debug.LogError($"PartyData.GenerateHeroStats: Null HeroSO or Stats.Type at index {i}");
                    continue;
                }

                var heroStat = new HeroStats(heroSOs[i], positionVectors[i]);
                if (allowCultist && heroSOs[i].Stats.Type.CanBeCultist && i == heroSOs.Count - 1)
                {
                    heroStat.IsCultist = true;
                }
                heroStats.Add(heroStat);
            }

            Debug.Log($"PartyData.GenerateHeroStats: Initialized {heroStats.Count} heroes");
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

            var position = heroStats[slot].Position; // Preserve position
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
        }

        private void OnValidate()
        {
            if (heroSOs == null || heroSOs.Count == 0)
            {
                Debug.LogWarning($"PartyData.OnValidate: No heroes assigned in {name}");
            }
            else if (heroSOs.Count > 4)
            {
                Debug.LogWarning($"PartyData.OnValidate: Too many heroes ({heroSOs.Count}) in {name}, max is 4");
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