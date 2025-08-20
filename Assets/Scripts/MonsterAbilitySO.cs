using UnityEngine;

namespace VirulentVentures
{
    public abstract class MonsterAbilitySO : ScriptableObject
    {
        [SerializeField] private string animationTrigger; // Animator trigger, e.g., "GhoulAttack"

        public string AnimationTrigger => animationTrigger;

        public virtual void Apply(MonsterStats target, PartyData partyData) { }
        public virtual bool CheckDodge() => false; // For ethereal monsters, override as needed
    }

    [CreateAssetMenu(fileName = "DefaultMonsterAbilitySO", menuName = "VirulentVentures/MonsterAbility/Default", order = 15)]
    public class DefaultMonsterAbilitySO : MonsterAbilitySO
    {
        // No-op for monsters like ghouls with no special ability
    }
}