using UnityEngine;

namespace VirulentVentures
{
    public abstract class SpecialAbilitySO : ScriptableObject
    {
        [SerializeField] private string animationTrigger; // Animator trigger, e.g., "FighterAttack"

        public string AnimationTrigger => animationTrigger;

        public abstract void Apply(HeroStats target, PartyData partyData);
    }

    [CreateAssetMenu(fileName = "FighterAbilitySO", menuName = "VirulentVentures/SpecialAbility/Fighter", order = 11)]
    public class FighterAbilitySO : SpecialAbilitySO
    {
        public override void Apply(HeroStats target, PartyData partyData)
        {
            if (target.Health < target.MaxHealth * 0.3f)
            {
                target.Attack += 3;
            }
        }
    }

    [CreateAssetMenu(fileName = "HealerAbilitySO", menuName = "VirulentVentures/SpecialAbility/Healer", order = 12)]
    public class HealerAbilitySO : SpecialAbilitySO
    {
        public override void Apply(HeroStats target, PartyData partyData)
        {
            if (partyData != null)
            {
                HeroStats lowestAlly = partyData.FindLowestHealthAlly();
                if (lowestAlly != null && lowestAlly.Health > 0)
                {
                    lowestAlly.Health = Mathf.Min(lowestAlly.Health + 5, lowestAlly.MaxHealth);
                }
            }
        }
    }

    [CreateAssetMenu(fileName = "ScoutAbilitySO", menuName = "VirulentVentures/SpecialAbility/Scout", order = 13)]
    public class ScoutAbilitySO : SpecialAbilitySO
    {
        public override void Apply(HeroStats target, PartyData partyData)
        {
            target.Defense += 2;
        }
    }

    [CreateAssetMenu(fileName = "TreasureHunterAbilitySO", menuName = "VirulentVentures/SpecialAbility/TreasureHunter", order = 14)]
    public class TreasureHunterAbilitySO : SpecialAbilitySO
    {
        public override void Apply(HeroStats target, PartyData partyData)
        {
            if (partyData != null)
            {
                HeroStats[] allies = partyData.FindAllies();
                foreach (var ally in allies)
                {
                    if (ally.Health > 0)
                    {
                        ally.Morale = Mathf.Min(ally.Morale + 3, 100);
                    }
                }
            }
        }
    }
}