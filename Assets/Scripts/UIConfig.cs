using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "UIConfig", menuName = "VirulentVentures/UIConfig", order = 1)]
    public class UIConfig : ScriptableObject
    {
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color textOutlineColor = new Color(0f, 0f, 0f, 1f); // Black outline
        [SerializeField] private Color bogRotColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Greenish tint
        [SerializeField] private Color tauntColor = new Color(1f, 1f, 0f, 1f); // Yellow #FFFF00 for Taunt
        [SerializeField] private Color thornsColor = new Color(1f, 1f, 0f, 1f); // Yellow #FFFF00 for Thorns
        [SerializeField] private Font pixelFont; // Assign Press Start 2P in Editor

        public Color TextColor => textColor;
        public Color TextOutlineColor => textOutlineColor;
        public Color BogRotColor => bogRotColor;
        public Color TauntColor => tauntColor; // Added getter for Taunt
        public Color ThornsColor => thornsColor; // Added getter for Thorns
        public Font PixelFont => pixelFont;
    }
}