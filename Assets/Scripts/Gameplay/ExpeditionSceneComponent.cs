using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    public class ExpeditionSceneComponent : MonoBehaviour
    {
        [SerializeField] private EventBusSO eventBus;
        [SerializeField] private ExpeditionData expeditionData;
        [SerializeField] private PartyData partyData;
        [SerializeField] private List<string> fallbackHeroIds = new List<string> { "Fighter", "Monk", "Scout", "Healer" };
        [SerializeField] private CharacterPositions defaultPositions;
        [SerializeField] private EncounterData combatEncounterData;
        [SerializeField] private ExpeditionUIComponent viewComponent;

        void Awake()
        {
            if (!ValidateReferences()) return;
        }

        void Start()
        {
            eventBus.OnNodeUpdated += HandleNodeUpdate;
            eventBus.OnSceneTransitionCompleted += HandleNodeUpdate;
            eventBus.OnContinueClicked += HandleContinueClicked;
            eventBus.OnNonCombatResolveRequested += HandleNonCombatResolve;

            var expedition = ExpeditionManager.Instance.GetExpedition();
            if (!expedition.IsValid())
                GenerateExpedition();

            HandleNodeUpdate(new EventBusSO.NodeUpdateData
            {
                nodes = expedition.NodeData,
                currentIndex = expedition.CurrentNodeIndex
            });
        }

        void OnDestroy()
        {
            if (eventBus != null)
            {
                eventBus.OnNodeUpdated -= HandleNodeUpdate;
                eventBus.OnSceneTransitionCompleted -= HandleNodeUpdate;
                eventBus.OnContinueClicked -= HandleContinueClicked;
                eventBus.OnNonCombatResolveRequested -= HandleNonCombatResolve;
            }
        }

        private void GenerateExpedition()
        {
            partyData.Reset();
            partyData.HeroIds = fallbackHeroIds;
            partyData.PartyID = System.Guid.NewGuid().ToString();
            partyData.GenerateHeroStats(defaultPositions.heroPositions);
            foreach (var h in partyData.HeroStats)
            {
                h.Health = h.MaxHealth;
                h.Morale = h.MaxMorale;
            }

            int totalDifficulty = Random.Range(24, 37);
            List<NodeData> nodes = new List<NodeData>();
            int remainingDifficulty = totalDifficulty;
            bool isCombat = Random.value > 0.5f;

            var nonCombatGen = gameObject.AddComponent<NonCombatNodeGenerator>();
            nonCombatGen.InitializeCache();
            nodes.Add(nonCombatGen.GenerateNonCombatNode("", 1, 0, isTempleNode: true));

            var combatGen = gameObject.AddComponent<CombatNodeGenerator>();
            while (remainingDifficulty >= 2 && nodes.Count < 8)
            {
                int rating = Random.Range(3, 7);
                if (remainingDifficulty < rating)
                    rating = Mathf.Max(2, remainingDifficulty);

                NodeData node = isCombat
                    ? combatGen.GenerateCombatNode("", 1, combatEncounterData, rating)
                    : nonCombatGen.GenerateNonCombatNode("", 1, rating);

                nodes.Add(node);
                remainingDifficulty -= rating;
                isCombat = !isCombat;
            }
            Destroy(nonCombatGen);
            Destroy(combatGen);

            expeditionData.SetNodes(nodes);
            expeditionData.CurrentNodeIndex = 0;
            expeditionData.SetParty(partyData);

            eventBus.RaiseExpeditionGenerated(expeditionData, partyData);
            eventBus.RaisePartyUpdated(partyData);
            eventBus.RaiseNodeUpdated(nodes, 0);
        }

        private void HandleNodeUpdate(EventBusSO.NodeUpdateData data)
        {
            if (data.nodes == null || data.currentIndex < 0 || data.currentIndex >= data.nodes.Count)
                return;

            var node = data.nodes[data.currentIndex];

            if (node.IsCombat && !node.Completed)
            {
                viewComponent?.SetContinueButtonEnabled(false);
                viewComponent?.FadeToCombat(() => ExpeditionManager.Instance.TransitionToCombatScene());
                return;
            }

            viewComponent?.SetContinueButtonEnabled(true);

            // Skip non-combat UI for Temple (node 0)
            if (data.currentIndex == 0)
            {
                eventBus.RaiseLogMessage(node.FlavourText, Color.white);
                return;
            }

            if (!node.IsCombat && !node.Completed)
            {
                if (node.NonCombatEncounter == null)
                {
                    Debug.LogError($"Non-combat node {data.currentIndex} has no encounter SO!");
                    return;
                }
                eventBus.RaiseNonCombatEncounter(node.NonCombatEncounter, node);
                return;
            }

            eventBus.RaiseLogMessage(node.Completed ? "Combat Won!" : node.FlavourText, Color.white);
            eventBus.RaisePartyUpdated(partyData);

            if (CheckForHeroDeaths()) return;
            CheckExpeditionFailure();
        }

        private void HandleContinueClicked()
        {
            var expedition = ExpeditionManager.Instance.GetExpedition();
            if (expedition == null || expedition.NodeData == null) return;

            if (CheckForHeroDeaths())
            {
                ExpeditionManager.Instance.TransitionToTemplePlanningScene();
                return;
            }

            if (expedition.CurrentNodeIndex >= expedition.NodeData.Count - 1)
                ExpeditionManager.Instance.TransitionToTemplePlanningScene();
            else
            {
                expedition.CurrentNodeIndex++;
                eventBus.RaiseNodeUpdated(expedition.NodeData, expedition.CurrentNodeIndex);
                ExpeditionManager.Instance.SaveProgress();
            }
        }

        private void HandleNonCombatResolve(EventBusSO.NonCombatResolveData resolveData)
        {
            var encounter = resolveData.encounter;
            var node = resolveData.node;
            var party = partyData.GetHeroes();

            int checkValue = GetPartySkillValue(party, encounter.SkillType, encounter.CheckMode);
            bool success = checkValue >= encounter.DifficultyCheck;

            string result = success
                ? ParseAndApplyOutcome(encounter.SuccessOutcome, party)
                : ParseAndApplyOutcome(encounter.FailureOutcome, party, node.SeededViruses);

            node.Completed = true;
            eventBus.RaiseNonCombatResolved(result, success);
            eventBus.RaisePartyUpdated(partyData);
            eventBus.RaiseNodeUpdated(expeditionData.NodeData, expeditionData.CurrentNodeIndex);
        }

        private int GetPartySkillValue(List<CharacterStats> heroes, SkillType type, CheckMode mode)
        {
            if (heroes == null || heroes.Count == 0) return 0;
            var values = heroes.Select(h => GetStat(h, type)).Where(v => v > 0).ToList();
            if (values.Count == 0) return 0;

            return mode switch
            {
                CheckMode.Best => values.Max(),
                CheckMode.Worst => values.Min(),
                CheckMode.Leader => GetStat(heroes[0], type),
                CheckMode.AllWeakestLink => values.Min(),
                _ => (int)values.Average()
            };
        }

        private int GetStat(CharacterStats hero, SkillType type) => type switch
        {
            SkillType.Speed => hero.Speed,
            SkillType.Attack => hero.Attack,
            SkillType.Defense => hero.Defense,
            SkillType.Evasion => hero.Evasion,
            SkillType.Immunity => hero.Immunity,
            SkillType.Morale => hero.Morale,
            SkillType.Health => hero.Health,
            _ => 0
        };

        private string ParseAndApplyOutcome(string outcome, List<CharacterStats> heroes, List<VirusSO> extraViruses = null)
        {
            if (string.IsNullOrEmpty(outcome)) return "No effect.";
            var parts = outcome.Split(';');
            string log = "";

            foreach (var part in parts)
            {
                var kv = part.Split(':');
                if (kv.Length < 2) continue;
                string key = kv[0].Trim().ToLower();
                string val = kv[1].Trim();

                switch (key)
                {
                    case "moraleboost":
                        int m = int.TryParse(val, out m) ? m : 0;
                        heroes.ForEach(h => h.Morale = Mathf.Min(h.Morale + m, h.MaxMorale));
                        log += $"Morale +{m}. ";
                        break;
                    case "healthloss":
                        int h = int.TryParse(val, out h) ? h : 0;
                        heroes.ForEach(hero => hero.Health = Mathf.Max(hero.Health - h, 0));
                        log += $"Health -{h}. ";
                        break;
                    case "seedvirus":
                        var virusSO = Resources.Load<VirusSO>($"Viruses/{val}");
                        if (virusSO != null && heroes.Count > 0)
                        {
                            var target = heroes[Random.Range(0, heroes.Count)];
                            target.Infections.Add(virusSO);
                            log += $"Seeded {virusSO.DisplayName}. ";
                            eventBus.RaiseVirusSeeded(virusSO, target);
                        }
                        break;
                }
            }

            if (extraViruses != null)
            {
                foreach (var v in extraViruses)
                {
                    if (heroes.Count > 0)
                    {
                        var target = heroes[Random.Range(0, heroes.Count)];
                        target.Infections.Add(v);
                        log += $"Natural: {v.DisplayName}. ";
                        eventBus.RaiseVirusSeeded(v, target);
                    }
                }
            }

            return log.Trim();
        }

        private bool CheckForHeroDeaths()
        {
            bool dead = partyData.HeroStats.Any(h => h.Type == CharacterType.Hero && h.Health <= 0);
            if (dead) Debug.Log("Hero death → return to Temple");
            return dead;
        }

        private void CheckExpeditionFailure()
        {
            if (partyData.HeroStats.All(h => h.HasRetreated || h.Health <= 0))
                ExpeditionManager.Instance.TransitionToTemplePlanningScene();
        }

        private bool ValidateReferences()
        {
            if (eventBus == null || expeditionData == null || partyData == null ||
                defaultPositions == null || combatEncounterData == null || viewComponent == null)
            {
                Debug.LogError("ExpeditionSceneComponent missing references!");
                return false;
            }
            return true;
        }
    }
}