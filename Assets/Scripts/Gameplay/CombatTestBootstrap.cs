using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace VirulentVentures
{
    public class CombatTestBootstrap : MonoBehaviour
    {
        [SerializeField] private CombatSceneComponent combatComponent;
        [SerializeField] private CombatNodeGenerator nodeGenerator;
        [SerializeField] private VirusSeederComponent virusSeeder;
        [SerializeField] private CombatConfig combatConfig;
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private UIConfig uiConfig;
        [SerializeField] private VirusConfigSO virusConfig;
        [SerializeField] private List<CharacterSO> defaultHeroes = new List<CharacterSO>(); // Set 4 in Inspector
        [SerializeField] private List<VirusSO> testViruses = new List<VirusSO>(); // Optional viruses
        [SerializeField] private Biome testBiome = Biome.Swamps; // Dropdown for biome
        [SerializeField] private UIDocument uiDocument; // UI Toolkit panel

        private PartyData partyData;
        private EncounterData encounterData;

        void Start()
        {
            // Validate references
            if (!ValidateReferences()) return;

            // Setup UI Toolkit controls
            SetupUIControls();

            // Initialize mocks
            partyData = ScriptableObject.CreateInstance<PartyData>();
            encounterData = ScriptableObject.CreateInstance<EncounterData>();

            // Setup heroes (4 default from CharacterLibrary)
            var heroes = defaultHeroes.Select(so => new CharacterStats(so, new Vector3(-2f * so.PartyPosition, 0, 0))).ToList();
            if (heroes.Count != 4)
            {
                Debug.LogWarning("CombatTestBootstrap: Exactly 4 heroes required. Using defaults.");
                heroes = CreateDefaultHeroes();
            }
            partyData.HeroStats = heroes;
            partyData.HeroIds = heroes.Select(h => h.Id).ToList();
            partyData.PartyID = "TestParty";

            // Generate monsters via CombatNodeGenerator
            var nodeData = nodeGenerator.GenerateCombatNode(testBiome.ToString(), 1, encounterData, 4);

            // Seed viruses on monsters
            virusSeeder.SeedVirusesForMonsters(nodeData.Monsters, 1);

            // Apply test viruses to heroes (if any)
            if (testViruses.Count > 0)
            {
                var hero = heroes[Random.Range(0, heroes.Count)];
                hero.AddInfection(testViruses[0]); // e.g., Sludge Infection
                Debug.Log($"CombatTestBootstrap: Applied {testViruses[0].DisplayName} to {hero.Id}");
                eventBus.RaiseLogMessage($"My {testViruses[0].DisplayName} infects {hero.Id}!", Color.red);
            }

            // Mock gear break (20% chance on attack)
            eventBus.OnUnitAttacking += (data) =>
            {
                if (data.attacker.IsHero && Random.value < 0.2f)
                {
                    var hero = (CharacterStats)data.attacker;
                    hero.Attack = 0; // Disable attack
                    eventBus.RaiseLogMessage($"My flawed gear shatters {hero.Id}'s weapon!", Color.red); // Red popout
                    eventBus.RaiseUnitUpdated(hero, hero.GetDisplayStats());
                }
            };

            // Start combat
            combatComponent.InitializeUnits(heroes, nodeData.Monsters);
            combatComponent.StartCombatLoop(partyData);
        }

        private bool ValidateReferences()
        {
            if (combatComponent == null) Debug.LogError("CombatTestBootstrap: combatComponent not assigned.");
            if (nodeGenerator == null) Debug.LogError("CombatTestBootstrap: nodeGenerator not assigned.");
            if (virusSeeder == null) Debug.LogError("CombatTestBootstrap: virusSeeder not assigned.");
            if (combatConfig == null) Debug.LogError("CombatTestBootstrap: combatConfig not assigned.");
            if (eventBus == null) Debug.LogError("CombatTestBootstrap: eventBus not assigned.");
            if (uiConfig == null) Debug.LogError("CombatTestBootstrap: uiConfig not assigned.");
            if (virusConfig == null) Debug.LogError("CombatTestBootstrap: virusConfig not assigned.");
            if (uiDocument == null) Debug.LogError("CombatTestBootstrap: uiDocument not assigned.");
            return combatComponent && nodeGenerator && virusSeeder && combatConfig && eventBus && uiConfig && virusConfig && uiDocument;
        }

        private void SetupUIControls()
        {
            var root = uiDocument.rootVisualElement;
            var regenerateButton = new Button(() => RegenerateEncounter()) { text = "Regenerate Encounter" };
            var virusButton = new Button(() => ApplyRandomVirus()) { text = "Apply Random Virus" };
            var speedResetButton = new Button(() => ResetCombatSpeed()) { text = "Reset Speed" };
            root.Add(regenerateButton);
            root.Add(virusButton);
            root.Add(speedResetButton);
            regenerateButton.style.color = uiConfig.TextColor;
            virusButton.style.color = uiConfig.TextColor;
            speedResetButton.style.color = uiConfig.TextColor;
        }

        private void RegenerateEncounter()
        {
            var nodeData = nodeGenerator.GenerateCombatNode(testBiome.ToString(), 1, encounterData, 4);
            virusSeeder.SeedVirusesForMonsters(nodeData.Monsters, 1);
            combatComponent.InitializeUnits(partyData.HeroStats, nodeData.Monsters);
            combatComponent.StartCombatLoop(partyData);
            eventBus.RaiseLogMessage("New encounter generated!", uiConfig.TextColor);
        }

        private void ApplyRandomVirus()
        {
            var viruses = virusConfig.GetViruses();
            if (viruses.Length == 0) return;
            var virus = viruses[Random.Range(0, viruses.Length)];
            var hero = partyData.HeroStats[Random.Range(0, partyData.HeroStats.Count)];
            hero.AddInfection(virus);
            eventBus.RaiseLogMessage($"My {virus.DisplayName} infects {hero.Id}!", Color.red);
            eventBus.RaiseUnitInfected(hero, virus.VirusID);
        }

        private void ResetCombatSpeed()
        {
            combatComponent.SetCombatSpeed(1f);
        }

        private List<CharacterStats> CreateDefaultHeroes()
        {
            return new List<CharacterStats>
            {
                CreateMockHero("Barbarian", 150, 30, 5, 1),
                CreateMockHero("Ranger", 120, 25, 7, 2),
                CreateMockHero("Healer", 100, 20, 4, 3),
                CreateMockHero("Scout", 110, 25, 6, 4)
            };
        }

        private CharacterStats CreateMockHero(string id, int health, int attack, int speed, int position)
        {
            var so = CharacterLibrary.GetHeroData(id);
            var stats = new CharacterStats(so, new Vector3(-2f * position, 0, 0))
            {
                Health = health,
                MaxHealth = health,
                Attack = attack,
                Speed = speed,
                Morale = 100,
                MaxMorale = 100,
                Immunity = 20,
                Type = CharacterType.Hero,
                PartyPosition = position
            };
            return stats;
        }
    }
}