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
                (hero.Health < hero.MaxHealth || hero.Morale < hero.MaxMorale));
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
                    stats.Infections = new List<VirusData>();
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
            foreach (var hero in HeroStats.Where(h => h.Infections.Any() && h.Health > 0 && !h.HasRetreated))
            {
                foreach (var virus in hero.Infections)
                {
                    if (virus.Effect == "HPDrain")
                    {
                        int damage = Mathf.RoundToInt(hero.MaxHealth * virus.EffectStrength);
                        hero.Health = Mathf.Max(1, hero.Health - damage);
                        string virusMessage = $"{hero.Id} suffers {damage} damage from {virus.VirusID}!";
                        combatLogs.Add(virusMessage);
                        eventBus.RaiseLogMessage(virusMessage, Color.red);
                        eventBus.RaiseUnitUpdated(hero, hero.GetDisplayStats());
                    }
                }
            }
        }
    }
}