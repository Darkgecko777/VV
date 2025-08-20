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

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"CharacterTypeSO.OnValidate: ID is missing for {name}! This will break lookups.");
            }
            else if (id.Trim().Length == 0)
            {
                Debug.LogWarning($"CharacterTypeSO.OnValidate: ID is whitespace for {name}! Please set a valid ID.");
            }

            // Auto-set canBeCultist = true for heroes, false for monsters
            if (isHero && !isMonster)
            {
                canBeCultist = true;
            }
            else if (isMonster && !isHero)
            {
                canBeCultist = false;
            }
        }
    }
}