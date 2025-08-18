using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EncounterData", menuName = "VirulentVentures/EncounterData", order = 10)]
public class EncounterData : ScriptableObject
{
    [SerializeField] private List<MonsterSO> monsters; // 1-4 Ghouls/Wraiths
    [SerializeField] private bool isCombatNode = true; // Always true for prototype

    public List<MonsterSO> Monsters => monsters;
    public bool IsCombatNode => isCombatNode;

    public List<CharacterRuntimeStats> SpawnMonsters()
    {
        List<CharacterRuntimeStats> monsterStats = new List<CharacterRuntimeStats>();
        if (monsters == null || monsters.Count == 0)
        {
            Debug.LogError("EncounterData: No monsters assigned!");
            return monsterStats;
        }

        for (int i = 0; i < monsters.Count; i++)
        {
            if (monsters[i] != null)
            {
                GameObject monsterObj = new GameObject(monsters[i].Stats.characterType.ToString());
                var runtimeStats = monsterObj.AddComponent<CharacterRuntimeStats>();
                runtimeStats.SetStats(monsters[i].Stats); // Apply MonsterSO stats
                monsterObj.AddComponent<SpriteAnimation>(); // For jiggle animations
                monsterStats.Add(runtimeStats);
            }
        }

        return monsterStats;
    }

    // Initialize encounter with random 1-4 Ghouls/Wraiths (for testing)
    public void InitializeRandomEncounter(MonsterSO ghoulSO, MonsterSO wraithSO)
    {
        monsters = new List<MonsterSO>();
        int monsterCount = Random.Range(1, 5); // 1-4 monsters
        for (int i = 0; i < monsterCount; i++)
        {
            MonsterSO monster = Random.value < 0.5f ? ghoulSO : wraithSO;
            if (monster != null)
            {
                monsters.Add(monster);
            }
        }
    }
}