using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "CharacterTypeSO", menuName = "VirulentVentures/CharacterTypeSO", order = 10)]
    public class CharacterTypeSO : ScriptableObject
    {
        [SerializeField] private string id; // e.g., "Fighter", "Ghoul"
        [SerializeField] private bool isHero;
        [SerializeField] private bool isMonster;
        [SerializeField] private bool canBeCultist;

        public string Id => id;
        public bool IsHero => isHero;
        public bool IsMonster => isMonster;
        public bool CanBeCultist => canBeCultist;
    }
}