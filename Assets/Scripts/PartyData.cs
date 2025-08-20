using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "PartyData", menuName = "VirulentVentures/PartyData", order = 12)]
    public class PartyData : ScriptableObject
    {
        [SerializeField] private List<CharacterTypeSO> heroTypes = new List<CharacterTypeSO>();
        [SerializeField] private List<HeroSO> heroSOs = new List<HeroSO>();
        [SerializeField] private bool allowCultist = false;
        [SerializeField] private CharacterPositions positions = CharacterPositions.Default();
        [SerializeField] private List<HeroStats> heroStats = new List<HeroStats>();

        public List<HeroStats> HeroStats => heroStats;

        public void InitializeParty()
        {
            heroStats.Clear();

            if (heroSOs == null || heroTypes == null || heroSOs.Count != heroTypes.Count)
            {
                Debug.LogError($"PartyData: Invalid hero setup! HeroSOs: {heroSOs?.Count ?? 0}, HeroTypes: {heroTypes?.Count ?? 0}");
                return;
            }

            if (heroSOs.Count < 1 || heroSOs.Count > 4)
            {
                Debug.LogError($"PartyData: Hero count must be 1-4, got {heroSOs.Count}");
                return;
            }

            if (positions.heroPositions == null || positions.heroPositions.Length < heroSOs.Count)
            {
                Debug.LogError($"PartyData: Invalid hero positions! Length: {positions.heroPositions?.Length ?? 0}, Required: {heroSOs.Count}");
                return;
            }

            for (int i = 0; i < heroSOs.Count; i++)
            {
                if (heroSOs[i] == null || heroTypes[i] == null)
                {
                    Debug.LogError($"PartyData: Null reference at index {i}! HeroSO: {heroSOs[i] != null}, HeroType: {heroTypes[i] != null}");
                    continue;
                }

                if (heroSOs[i].Stats.Type != heroTypes[i])
                {
                    Debug.LogWarning($"PartyData: Mismatch between HeroSO type {heroSOs[i].Stats.Type?.Id} and HeroTypes[{i}] {heroTypes[i].Id}");
                }

                var heroStat = new HeroStats(heroSOs[i], positions.heroPositions[i]);
                if (allowCultist && heroTypes[i].CanBeCultist && i == heroSOs.Count - 1)
                {
                    heroStat.IsCultist = true;
                }
                heroStats.Add(heroStat);
            }
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
                Debug.LogError($"PartyData: Invalid slot {slot} for cultist replacement");
                return;
            }
            if (cultistSO == null)
            {
                Debug.LogError("PartyData: Null cultistSO");
                return;
            }
            if (!cultistSO.Stats.Type.CanBeCultist)
            {
                Debug.LogError($"PartyData: HeroSO type {cultistSO.Stats.Type.Id} cannot be a cultist");
                return;
            }

            var cultistStats = new HeroStats(cultistSO, positions.heroPositions[slot]) { IsCultist = true };
            heroStats[slot] = cultistStats;
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
    }
}