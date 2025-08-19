using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "EncounterData", menuName = "VirulentVentures/EncounterData", order = 10)]
public class EncounterData : ScriptableObject
{
    [SerializeField] private MonsterSO ghoulSO; // Single GhoulSO reference
    [SerializeField] private MonsterSO wraithSO; // Single WraithSO reference
    [SerializeField] private bool isCombatNode = true; // Always true for prototype
    [SerializeField] private CharacterPositions positions = CharacterPositions.Default();
    private List<CharacterRuntimeStats> cachedMonsters;

    public bool IsCombatNode => isCombatNode;

    public List<CharacterRuntimeStats> SpawnMonsters()
    {
        if (cachedMonsters != null && cachedMonsters.Count == 4 && cachedMonsters.All(m => m != null && m.gameObject != null))
        {
            Debug.Log("EncounterData: Reusing cached monsters");
            return cachedMonsters;
        }

        cachedMonsters = new List<CharacterRuntimeStats>();

        if (ghoulSO == null || wraithSO == null)
        {
            Debug.LogError($"EncounterData: Missing SO references! GhoulSO: {ghoulSO != null}, WraithSO: {wraithSO != null}");
            return cachedMonsters;
        }
        if (positions.monsterPositions == null || positions.monsterPositions.Length != 4)
        {
            Debug.LogError($"EncounterData: Invalid monster positions! Positions: {positions.monsterPositions != null}, Length: {positions.monsterPositions?.Length ?? 0}");
            return cachedMonsters;
        }

        // Fixed composition: 2 Ghouls, 2 Wraiths
        MonsterSO[] monsterComposition = new MonsterSO[] { ghoulSO, ghoulSO, wraithSO, wraithSO };

        for (int i = 0; i < monsterComposition.Length; i++)
        {
            if (monsterComposition[i] == null)
            {
                Debug.LogWarning($"EncounterData: Null MonsterSO or Stats at index {i}");
                continue;
            }
            GameObject monsterObj = new GameObject($"Monster{i + 1}_{monsterComposition[i].Stats.characterType}");
            monsterObj.transform.position = positions.monsterPositions[i];
            var runtimeStats = monsterObj.AddComponent<CharacterRuntimeStats>();
            var renderer = monsterObj.AddComponent<SpriteRenderer>();
            renderer.sprite = monsterComposition[i].Sprite;
            renderer.sortingLayerName = "Characters";
            renderer.transform.localScale = new Vector3(2f, 2f, 1f);
            runtimeStats.SetCharacterSO(monsterComposition[i]);
            runtimeStats.Initialize();
            cachedMonsters.Add(runtimeStats);
            Debug.Log($"EncounterData: Spawned {monsterComposition[i].Stats.characterType} as {monsterObj.name} at position {i}");
        }

        return cachedMonsters;
    }

    // Initialize encounter with fixed 2 Ghouls, 2 Wraiths (for testing)
    public void InitializeRandomEncounter(MonsterSO ghoulSO, MonsterSO wraithSO)
    {
        this.ghoulSO = ghoulSO;
        this.wraithSO = wraithSO;
        cachedMonsters = null; // Reset cache if reinitializing
    }
}