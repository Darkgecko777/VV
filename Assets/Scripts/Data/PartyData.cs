using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "PartyData", menuName = "VirulentVentures/PartyData")]
    public class PartyData : ScriptableObject
    {
        [SerializeField] private string partyId;
        public List<string> HeroIds;
        public List<CharacterStats> HeroStats;
        public bool AllowCultist;

        public string PartyID
        {
            get => partyId;
            set => partyId = value;
        }

        public void Reset()
        {
            partyId = "";
            HeroIds = new List<string>();
            HeroStats = new List<CharacterStats>();
            AllowCultist = false;
        }

        public List<CharacterStats> GetHeroes()
        {
            return HeroStats?.Where(h => h.Type == CharacterType.Hero).ToList() ?? new List<CharacterStats>();
        }

        public List<CharacterStats> CheckDeadStatus()
        {
            return HeroStats?.FindAll(h => h.Type == CharacterType.Hero && h.Health > 0) ?? new List<CharacterStats>();
        }

        public CharacterStats FindLowestHealthAlly()
        {
            return HeroStats?.Where(h => h.Type == CharacterType.Hero && h.Health > 0).OrderBy(h => h.Health).FirstOrDefault();
        }

        public CharacterStats[] FindAllies()
        {
            return HeroStats?.Where(h => h.Type == CharacterType.Hero).ToArray() ?? new CharacterStats[0];
        }

        public bool CanHealParty()
        {
            if (HeroStats == null || HeroStats.Count == 0)
                return false;
            return HeroStats.Any(hero =>
                !hero.HasRetreated && hero.Health > 0 &&
                (hero.Health < hero.MaxHealth || hero.Morale < hero.MaxMorale || hero.Infections.Any()));
        }

        public bool CheckRetreat(ICombatUnit unit, EventBusSO eventBus, UIConfig uiConfig, CombatConfig combatConfig)
        {
            if (unit is not CharacterStats stats || stats.Type != CharacterType.Hero || stats.HasRetreated)
                return false;
            return stats.Morale <= combatConfig.RetreatMoraleThreshold;
        }

        public void ProcessRetreat(ICombatUnit unit, EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs, CombatConfig combatConfig)
        {
            if (unit == null || unit.HasRetreated) return;
            if (unit is not CharacterStats stats || stats.Type != CharacterType.Hero) return;

            stats.HasRetreated = true;
            stats.Morale = Mathf.Min(stats.Morale + 20, stats.MaxMorale);
            string retreatMessage = $"{stats.Id} flees! <color=#FFFF00>[Morale <= {combatConfig.RetreatMoraleThreshold}]</color>";
            combatLogs.Add(retreatMessage);
            eventBus.RaiseLogMessage(retreatMessage, uiConfig.TextColor);
            eventBus.RaiseUnitRetreated(unit);

            int penalty = 10;
            var teammates = HeroStats.Where(h => h.Type == stats.Type && h.Health > 0 && !h.HasRetreated && h != stats).ToList();
            foreach (var teammate in teammates)
            {
                teammate.Morale = Mathf.Max(0, teammate.Morale - penalty);
                string teammateMessage = $"{teammate.Id}'s morale drops by {penalty} due to {stats.Id}'s retreat! <color=#FFFF00>[-{penalty} Morale]</color>";
                combatLogs.Add(teammateMessage);
                eventBus.RaiseLogMessage(teammateMessage, uiConfig.TextColor);
                eventBus.RaiseUnitUpdated(teammate, teammate.GetDisplayStats());
            }
            eventBus.RaiseUnitUpdated(unit, stats.GetDisplayStats());
        }

        public void GenerateHeroStats(Vector3[] positions)
        {
            HeroStats = HeroStats ?? new List<CharacterStats>();
            HeroStats.Clear();
            var positionMap = new Dictionary<int, Vector3>
            {
                { 1, positions[0] },
                { 2, positions[1] },
                { 3, positions[2] },
                { 4, positions[3] }
            };
            foreach (var heroId in HeroIds)
            {
                var data = CharacterLibrary.GetHeroData(heroId);
                int partyPosition = data.PartyPosition;
                if (partyPosition < 1 || partyPosition > 4)
                {
                    Debug.LogWarning($"PartyData: Invalid PartyPosition {partyPosition} for {heroId}, skipping.");
                    continue;
                }
                if (positionMap.ContainsKey(partyPosition))
                {
                    var stats = new CharacterStats(data, positionMap[partyPosition]);
                    if (stats.Rank < 1 || stats.Rank > 3)
                    {
                        Debug.LogWarning($"PartyData: Invalid Rank {stats.Rank} for {heroId}, setting to 1.");
                        stats.Rank = 1;
                    }
                    stats.Infections = new List<VirusSO>();
                    HeroStats.Add(stats);
                    positionMap.Remove(partyPosition);
                }
                else
                {
                    Debug.LogWarning($"PartyData: PartyPosition {partyPosition} for {heroId} already occupied or invalid.");
                }
            }
            HeroStats = HeroStats.OrderBy(h => CharacterLibrary.GetHeroData(h.Id).PartyPosition).ToList();
        }

        public CharacterStats GetHeroByPosition(int position)
        {
            return HeroStats?.Find(h => CharacterLibrary.GetHeroData(h.Id).PartyPosition == position);
        }

        public void ApplyVirusEffects(EventBusSO eventBus, UIConfig uiConfig, List<string> combatLogs)
        {
            foreach (var unit in HeroStats)
            {
                if (unit.Health <= 0 || unit.HasRetreated) continue;

                foreach (var virus in unit.Infections)
                {
                    var effect = VirusEffectLibrary.GetEffect(virus.VirusID, virus.Rarity);
                    string message = $"{unit.Id} suffers {virus.DisplayName} ({effect.Stat}: {effect.Value})";
                    combatLogs.Add(message);
                    eventBus.RaiseLogMessage(message, Color.red);

                    switch (effect.Stat)
                    {
                        case "Health":
                        case "Morale":
                            unit.ApplyDoTHoT(effect.Stat, effect.Value);
                            break;
                        case "MaxHealth":
                        case "MaxMorale":
                            unit.ApplyMaxPercentMod(effect.Stat, effect.Value);
                            break;
                        case "Stun":
                        case "Confusion":
                        case "Blindness":
                            // Skip - handled by CombatSceneComponent
                            break;
                        default:
                            // Static stat mods - apply directly
                            ApplyDirectStatMod(unit, effect.Stat, effect.Value, combatLogs, eventBus, uiConfig);
                            break;
                    }
                    eventBus.RaiseUnitUpdated(unit, unit.GetDisplayStats());
                }
            }
        }

        // ADD THIS NEW METHOD (after ApplyVirusEffects):
        private void ApplyDirectStatMod(CharacterStats unit, string stat, float value, List<string> combatLogs, EventBusSO eventBus, UIConfig uiConfig)
        {
            int intValue = Mathf.RoundToInt(value);
            string message = value >= 0 ? "gains" : "loses";

            switch (stat.ToLower())
            {
                case "speed":
                    unit.Speed = Mathf.Max(0, unit.Speed + intValue);
                    break;
                case "attack":
                    unit.Attack = Mathf.Max(0, unit.Attack + intValue);
                    break;
                case "defense":
                    unit.Defense = Mathf.Max(0, unit.Defense + intValue);
                    break;
                case "evasion":
                    unit.Evasion = Mathf.Max(0, unit.Evasion + intValue);
                    break;
                case "immunity":
                    unit.Immunity = Mathf.Clamp(unit.Immunity + intValue, 0, 100);
                    break;
            }

            combatLogs.Add($"{unit.Id} {message} {Mathf.Abs(intValue)} {stat.ToLower()} from virus!");
            eventBus.RaiseLogMessage($"{unit.Id} {message} {Mathf.Abs(intValue)} {stat.ToLower()} from virus!", Color.red);
        }

        private UnitAttackState GetUnitAttackState(CharacterStats unit)
        {
            // This will be called from CombatSceneComponent - return null for now
            return null;
        }

        private void ApplyStatModifier(CharacterStats unit, VirusSO.Modifier modifier, List<string> combatLogs,
            EventBusSO eventBus, UIConfig uiConfig)
        {
            int value = Mathf.RoundToInt(modifier.Value);
            string statName = modifier.Type.ToLower();

            switch (modifier.Type.ToLower())
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
                    unit.Evasion = Mathf.Max(0, unit.Evasion + value);
                    break;
                case "immunity":
                    unit.Immunity = Mathf.Clamp(unit.Immunity + value, 0, 100);
                    break;
            }

            string message = value >= 0 ? $"gains" : $"loses";
            combatLogs.Add($"{unit.Id} {message} {Mathf.Abs(value)} {statName} from {modifier.Type}!");
            eventBus.RaiseLogMessage($"{unit.Id} {message} {Mathf.Abs(value)} {statName} from {modifier.Type}!", Color.red);
            eventBus.RaiseUnitUpdated(unit, unit.GetDisplayStats());
        }
    }
}