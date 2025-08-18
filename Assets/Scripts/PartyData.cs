using UnityEngine;
using System.Collections.Generic;

public class PartyData : MonoBehaviour
{
    [SerializeField] private HeroSO fighter;
    [SerializeField] private HeroSO healer;
    [SerializeField] private HeroSO treasureHunter;
    [SerializeField] private HeroSO scout;
    [SerializeField] private bool allowCultist = false; // Placeholder for temple role swap
    [SerializeField] private CharacterPositions positions = CharacterPositions.Default();
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
            return;
        }
        if (positions.heroPositions.Length != 4)
        {
            return;
        }

        // Setup Fighter
        GameObject fighterObj = new GameObject("Fighter");
        fighterObj.transform.position = positions.heroPositions[0];
        var fighterStats = fighterObj.AddComponent<CharacterRuntimeStats>();
        var fighterRenderer = fighterObj.AddComponent<SpriteRenderer>();
        fighterRenderer.sprite = fighter.Sprite;
        fighterRenderer.sortingLayerName = "Characters";
        fighterRenderer.transform.localScale = new Vector3(2f, 2f, 1f);
        fighterStats.SetCharacterSO(fighter);
        fighterStats.Initialize();
        heroStats.Add(fighterStats);

        // Setup Healer
        GameObject healerObj = new GameObject("Healer");
        healerObj.transform.position = positions.heroPositions[1];
        var healerStats = healerObj.AddComponent<CharacterRuntimeStats>();
        var healerRenderer = healerObj.AddComponent<SpriteRenderer>();
        healerRenderer.sprite = healer.Sprite;
        healerRenderer.sortingLayerName = "Characters";
        healerRenderer.transform.localScale = new Vector3(2f, 2f, 1f);
        healerStats.SetCharacterSO(healer);
        healerStats.Initialize();
        heroStats.Add(healerStats);

        // Setup Treasure Hunter
        GameObject treasureHunterObj = new GameObject("TreasureHunter");
        treasureHunterObj.transform.position = positions.heroPositions[2];
        var treasureHunterStats = treasureHunterObj.AddComponent<CharacterRuntimeStats>();
        var treasureHunterRenderer = treasureHunterObj.AddComponent<SpriteRenderer>();
        treasureHunterRenderer.sprite = treasureHunter.Sprite;
        treasureHunterRenderer.sortingLayerName = "Characters";
        treasureHunterRenderer.transform.localScale = new Vector3(2f, 2f, 1f);
        treasureHunterStats.SetCharacterSO(treasureHunter);
        treasureHunterStats.Initialize();
        heroStats.Add(treasureHunterStats);

        // Setup Scout (or Cultist if flagged)
        GameObject scoutObj = new GameObject(allowCultist ? "Cultist" : "Scout");
        scoutObj.transform.position = positions.heroPositions[3];
        var scoutStats = scoutObj.AddComponent<CharacterRuntimeStats>();
        var scoutRenderer = scoutObj.AddComponent<SpriteRenderer>();
        scoutRenderer.sprite = scout.Sprite;
        scoutRenderer.sortingLayerName = "Characters";
        scoutRenderer.transform.localScale = new Vector3(2f, 2f, 1f);
        scoutStats.SetCharacterSO(scout);
        if (allowCultist)
        {
            CharacterStatsData scoutData = scout.Stats;
            scoutData.isCultist = true;
            scoutData.bogRotSpreadChance = 0.20f;
            scoutStats.SetStats(scoutData);
        }
        scoutStats.Initialize();
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

    public void ReplaceWithCultist(int slot, HeroSO cultistSO)
    {
        if (slot < 0 || slot >= heroStats.Count)
        {
            return;
        }
        if (cultistSO == null)
        {
            return;
        }

        // Replace hero at slot (default Scout, slot 3) with Cultist
        GameObject newCultist = new GameObject("Cultist");
        newCultist.transform.position = positions.heroPositions[slot];
        var cultistStats = newCultist.AddComponent<CharacterRuntimeStats>();
        var cultistRenderer = newCultist.AddComponent<SpriteRenderer>();
        cultistRenderer.sprite = cultistSO.Sprite;
        cultistRenderer.sortingLayerName = "Characters";
        cultistRenderer.transform.localScale = new Vector3(2f, 2f, 1f);
        cultistStats.SetCharacterSO(cultistSO);
        CharacterStatsData cultistData = cultistSO.Stats;
        cultistData.isCultist = true;
        cultistData.bogRotSpreadChance = 0.20f;
        cultistStats.SetStats(cultistData);
        cultistStats.Initialize();
        heroStats[slot] = cultistStats;
        // Destroy old hero GameObject (handled in scene cleanup if needed)
    }

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

    public CharacterRuntimeStats[] FindAllies()
    {
        List<CharacterRuntimeStats> allies = heroStats.FindAll(h => h != null && h.Stats.health > 0);
        return allies.ToArray();
    }
}