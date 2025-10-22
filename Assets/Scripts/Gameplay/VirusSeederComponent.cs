// Full revised VirusSeederComponent.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VirulentVentures
{
    public class VirusSeederComponent : MonoBehaviour
    {
        [SerializeField] private EventBusSO eventBus; // Assign in Inspector
        [SerializeField] private ExpeditionManager expeditionManager; // Assign in Inspector (Instance)

        // Call this from CombatSceneComponent.Start() or NonCombat handler
        public void SeedViruses(NodeData nodeData, int depth)
        {
            Biome biome = (Biome)System.Enum.Parse(typeof(Biome), nodeData.Biome, true);
            List<VirusSO> biomeViruses = VirusEffectLibrary.GetBiomeViruses(biome);
            if (biomeViruses.Count == 0) return;

            // For combat: Seed on monsters
            if (nodeData.IsCombat)
            {
                foreach (var unit in nodeData.Monsters)
                {
                    SeedVirusOnUnit(unit, biomeViruses, depth);
                }
            }
            // For non-combat: Adapt in Step 4 (seed on heroes post-failure)
        }

        public void SeedVirusesForMonsters(List<CharacterStats> monsters, int depth)
        {
            Biome biome = Biome.Swamps; // Hardcoded for swamp prototype - pass as param later
            List<VirusSO> biomeViruses = VirusEffectLibrary.GetBiomeViruses(biome);
            if (biomeViruses.Count == 0) return;

            foreach (var unit in monsters)
            {
                SeedVirusOnUnit(unit, biomeViruses, depth);
            }
        }

        private void SeedVirusOnUnit(CharacterStats unit, List<VirusSO> biomeViruses, int depth)
        {
            if (unit.Type == CharacterType.Hero) // Heroes use Immunity check
            {
                if (Random.Range(0, 100) < unit.Immunity) return; // Resisted
            }

            // FIXED: Correct null-safe access to expeditionManager.expeditionData
            VirusSO customVirus = expeditionManager != null && expeditionManager.expeditionData != null
                ? expeditionManager.expeditionData.CustomVirus
                : null;

            if (customVirus != null && Random.value < 0.5f && IsVectorCompatible(unit, customVirus.TransmissionVector))
            {
                ApplyVirus(unit, customVirus);
                return;
            }

            // Natural seeding
            List<VirusSO> filtered = biomeViruses.Where(v => IsVectorCompatible(unit, v.TransmissionVector)).ToList();
            if (filtered.Count == 0) return;

            // Weighted rarity by depth (e.g., common high early)
            float[] weights = new float[] { 0.7f - depth * 0.05f, 0.2f + depth * 0.02f, 0.08f + depth * 0.02f, 0.02f + depth * 0.01f }; // Common/Uncommon/Rare/Epic
            VirusRarity rarity = GetWeightedRarity(weights);

            // Filter to specific rarity (assume SOs are per rarity)
            VirusSO baseVirus = filtered.GroupBy(v => v.VirusID).Select(g => g.First()).OrderBy(_ => Random.value).First(); // Random base
            VirusSO virus = filtered.FirstOrDefault(v => v.VirusID == baseVirus.VirusID && v.Rarity == rarity) ?? baseVirus; // Fallback to common

            // Mutation roll
            float mutationChance = 0.05f + Random.Range(0f, 0.15f) + depth * 0.02f + (customVirus != null ? 0.2f : 0f);
            if (Random.value < mutationChance && (int)virus.Rarity < (int)VirusRarity.Epic)
            {
                rarity = (VirusRarity)((int)virus.Rarity + 1);
                virus = filtered.FirstOrDefault(v => v.VirusID == virus.VirusID && v.Rarity == rarity) ?? virus; // Upgrade if exists
            }

            ApplyVirus(unit, virus);
        }

        private bool IsVectorCompatible(CharacterStats unit, TransmissionVector vector)
        {
            var caps = CharacterLibrary.GetCharacterData(unit.Id).Capabilities; // From CharacterSO
            return vector switch
            {
                TransmissionVector.Health => caps.canTransmitHealth,
                TransmissionVector.Morale => caps.canTransmitMorale,
                TransmissionVector.Environment => caps.canTransmitEnvironment,
                TransmissionVector.Obstacle => caps.canTransmitObstacle,
                _ => false
            };
        }

        private VirusRarity GetWeightedRarity(float[] weights)
        {
            float total = weights.Sum();
            float rand = Random.value * total;
            float cumulative = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (rand <= cumulative) return (VirusRarity)i;
            }
            return VirusRarity.Common;
        }

        private void ApplyVirus(CharacterStats unit, VirusSO virus)
        {
            unit.Infections.Add(virus);
            var effect = VirusEffectLibrary.GetEffect(virus.VirusID, virus.Rarity);
            ApplyEffectToUnit(unit, effect);
            eventBus.RaiseVirusSeeded(virus, unit);
        }

        private void ApplyEffectToUnit(CharacterStats unit, VirusEffectLibrary.VirusEffect effect)
        {
            // Apply immediate stat modifications (DoT/HoT/specials handled in combat loop)
            int value = Mathf.RoundToInt(effect.Value);
            switch (effect.Stat.ToLower())
            {
                case "speed":
                    unit.Speed = Mathf.Max(0, unit.Speed + value);
                    break;
                case "attack":
                    unit.Attack = Mathf.Max(0, unit.Attack + value);
                    break;
                case "defense":
                    unit.Defense = Mathf.Max(0, unit.Defense + value);
                    break;
                case "evasion":
                    unit.Evasion = Mathf.Clamp(unit.Evasion + value, 0, 100);
                    break;
                case "maxhealth":
                    unit.MaxHealth = Mathf.Max(1, unit.MaxHealth + value);
                    break;
                case "maxmorale":
                    unit.MaxMorale = Mathf.Max(1, unit.MaxMorale + value);
                    break;
                case "immunity":
                    unit.Immunity = Mathf.Clamp(unit.Immunity + value, 0, 100);
                    break;
                // Specials (Stun/Confusion/Blindness) - handled by CombatSceneComponent
                case "stun":
                case "confusion":
                case "blindness":
                    // Skip immediate application - flag for combat logic
                    break;
            }
        }

        // NEW: For non-combat (Step 4)
        public void SeedVirusOnHeroes(PartyData party, TransmissionVector vector, int depth)
        {
            Biome biome = Biome.Swamps; // Adjust as needed
            List<VirusSO> biomeViruses = VirusEffectLibrary.GetBiomeViruses(biome)
                .Where(v => v.TransmissionVector == vector).ToList();
            if (biomeViruses.Count == 0) return;

            var heroes = party.GetHeroes();
            if (heroes.Count == 0) return;

            // Seed on random hero (or worst by stat - extend later)
            var target = heroes[Random.Range(0, heroes.Count)];
            SeedVirusOnUnit(target, biomeViruses, depth);
        }
    }
}