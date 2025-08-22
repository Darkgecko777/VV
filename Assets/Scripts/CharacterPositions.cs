using UnityEngine;

namespace VirulentVentures
{
    [CreateAssetMenu(fileName = "CharacterPositions", menuName = "VirulentVentures/CharacterPositions")]
    public class CharacterPositions : ScriptableObject
    {
        public Vector3[] heroPositions = new Vector3[]
        {
            new Vector3(-2f, 0f, 0f),
            new Vector3(-1f, 0f, 0f),
            new Vector3(-3f, 0f, 0f),
            new Vector3(-4f, 0f, 0f)
        };

        public Vector3[] monsterPositions = new Vector3[]
        {
            new Vector3(1.5f, 0f, 0f),
            new Vector3(3.5f, 0f, 0f),
            new Vector3(5.5f, 0f, 0f),
            new Vector3(7.5f, 0f, 0f)
        };

        public static CharacterPositions Default()
        {
            return CreateInstance<CharacterPositions>();
        }
    }
}