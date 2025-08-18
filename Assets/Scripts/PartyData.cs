using UnityEngine;
using System.Collections.Generic;

public class PartyData : MonoBehaviour
{
    [SerializeField] private HeroSO fighter;
    [SerializeField] private HeroSO healer;
    [SerializeField] private HeroSO treasureHunter;
    [SerializeField] private HeroSO scout;
    [SerializeField] private bool allowCultist = false; // Placeholder for temple role swap
    private List<CharacterRuntimeStats> heroStats = new List<CharacterRuntimeStats>();

    void Awake()
    {
        InitializeParty();
    }

    public void InitializeParty()
    {
        heroStats.Clear();
        if (fighter == null || healer == null || treasureHunter == null || scout == null)
        {
            Debug.LogError("PartyData: Missing HeroSO assignments in Inspector!");
            return;
        }

        // Setup Fighter
        GameObject fighterObj = new GameObject("Fighter");
        var fighterStats = fighterObj.AddComponent<CharacterRuntimeStats>();
        fighterStats.SetStats(fighter.Stats);
        heroStats.Add(fighterStats);

        // Setup Healer
        GameObject healerObj = new GameObject("Healer");
        var healerStats = healerObj.AddComponent<CharacterRuntimeStats>();
        healerStats.SetStats(healer.Stats);
        heroStats.Add(healerStats);

        // Setup Treasure Hunter
        GameObject treasureHunterObj = new GameObject("TreasureHunter");
        var treasureHunterStats = treasureHunterObj.AddComponent<CharacterRuntimeStats>();
        treasureHunterStats.SetStats(treasureHunter.Stats);
        heroStats.Add(treasureHunterStats);

        // Setup Scout (or Cultist if flagged)
        GameObject scoutObj = new GameObject(allowCultist ? "Cultist" : "Scout");
        var scoutStats = scoutObj.AddComponent<CharacterRuntimeStats>();
        if (allowCultist)
        {
            CharacterStatsData scoutData = scout.Stats;
            scoutData.isCultist = true;
            scoutData.bogRotSpreadChance = 0.20f; // Cultist spread
            scoutStats.SetStats(scoutData);
        }
        else
        {
            scoutStats.SetStats(scout.Stats);
        }
        heroStats.Add(scoutStats);
    }

    public List<CharacterRuntimeStats> GetHeroes()
    {
        return heroStats;
    }

    public List<CharacterRuntimeStats> CheckDeadStatus()
    {
        return heroStats.FindAll(h => h != null && h.Stats.health > 0);
    }

    // Placeholder for temple role cultist insertion
    public void ReplaceWithCultist(int slot, HeroSO cultistSO)
    {
        if (slot < 0 || slot >= heroStats.Count)
        {
            Debug.LogError("PartyData: Invalid slot for cultist replacement!");
            return;
        }
        if (cultistSO == null)
        {
            Debug.LogError("PartyData: Missing Cultist HeroSO!");
            return;
        }

        // Replace hero at slot (default Scout, slot 3) with Cultist
        GameObject newCultist = new GameObject("Cultist");
        var cultistStats = newCultist.AddComponent<CharacterRuntimeStats>();
        CharacterStatsData cultistData = cultistSO.Stats;
        cultistData.isCultist = true;
        cultistData.bogRotSpreadChance = 0.20f;
        cultistStats.SetStats(cultistData);
        heroStats[slot] = cultistStats;
        // Destroy old hero GameObject (handled in scene cleanup if needed)
    }

    // For HealerSO: Find lowest-HP ally
    public CharacterRuntimeStats FindLowestHealthAlly()
    {
        CharacterRuntimeStats lowestAlly = null;
        float lowestHealth = float.MaxValue;
        foreach (var hero in heroStats)
        {
            if (hero != null && hero.Stats.health > 0 && hero.Stats.health < lowestHealth)
            {
                lowestAlly = hero;
                lowestHealth = hero.Stats.health;
            }
        }
        return lowestAlly;
    }

    // For TreasureHunterSO: Find all living allies
    public CharacterRuntimeStats[] FindAllies()
    {
        List<CharacterRuntimeStats> allies = heroStats.FindAll(h => h != null && h.Stats.health > 0);
        return allies.ToArray();
    }
}