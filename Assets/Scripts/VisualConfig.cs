using UnityEngine;
using System.Collections.Generic;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "VisualConfig", menuName = "ScriptableObjects/VisualConfig", order = 1)]
    public class VisualConfig : ScriptableObject
    {
        [SerializeField] private Sprite defaultPortrait;
        [SerializeField] private Sprite defaultCombatSprite;
        [SerializeField] private Sprite defaultEnemySprite;
        [SerializeField] private Dictionary<string, Sprite> portraits = new Dictionary<string, Sprite>();
        [SerializeField] private Dictionary<string, Sprite> combatSprites = new Dictionary<string, Sprite>();
        [SerializeField] private Dictionary<string, Sprite> enemySprites = new Dictionary<string, Sprite>();
        [SerializeField]
        private Dictionary<string, Color> nodeColors = new Dictionary<string, Color> {
            { "NonCombat", Color.green },
            { "Combat", Color.red },
            { "Temple", Color.gray } // Added for Temple nodes to eliminate warnings
        };

        public Sprite GetPortrait(string id)
        {
            return portraits.TryGetValue(id, out var sprite) ? sprite : defaultPortrait;
        }

        public Sprite GetCombatSprite(string id)
        {
            return combatSprites.TryGetValue(id, out var sprite) ? sprite : defaultCombatSprite;
        }

        public Sprite GetEnemySprite(string id)
        {
            return enemySprites.TryGetValue(id, out var sprite) ? sprite : defaultEnemySprite;
        }

        public Color GetNodeColor(string nodeType)
        {
            if (nodeColors.TryGetValue(nodeType, out var color))
            {
                return color;
            }
            Debug.LogWarning($"VisualConfig.GetNodeColor: No color found for nodeType {nodeType}, returning white");
            return Color.white;
        }
    }
}