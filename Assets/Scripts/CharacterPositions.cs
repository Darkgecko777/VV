using UnityEngine;

[System.Serializable]
public struct CharacterPositions
{
    public Vector2[] heroPositions;
    public Vector2[] monsterPositions;

    // Default constructor with specified positions
    public CharacterPositions(Vector2[] heroPos, Vector2[] monsterPos)
    {
        heroPositions = heroPos;
        monsterPositions = monsterPos;
    }

    // Static method to get default positions
    public static CharacterPositions Default()
    {
        return new CharacterPositions(
            new Vector2[] {
                new Vector2(-2.5f, 0f), // Hero 1 (Fighter)
                new Vector2(-4f, 0f),   // Hero 2 (Healer)
                new Vector2(-5.5f, 0f), // Hero 3 (Treasure Hunter)
                new Vector2(-7f, 0f)    // Hero 4 (Scout)
            },
            new Vector2[] {
                new Vector2(1.5f, 0f),  // Monster 1
                new Vector2(3.5f, 0f),  // Monster 2
                new Vector2(5.5f, 0f),  // Monster 3
                new Vector2(7.5f, 0f)   // Monster 4
            }
        );
    }
}