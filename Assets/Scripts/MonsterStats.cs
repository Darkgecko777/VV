using UnityEngine;

public class MonsterStats : BaseCharacterStats
{
    private const float bogRotMoraleDrain = 5f;
    private const float bogRotSpreadChance = 0.25f; // Higher spread chance for monsters

    void Awake()
    {
        characterType = CharacterType.Ghoul;
    }

    public override bool TryInfect()
    {
        if (Random.value <= bogRotSpreadChance)
        {
            isInfected = true;
            morale = Mathf.Max(morale - bogRotMoraleDrain, 0f);
            OnInfected.Invoke(this);
            return true;
        }
        return false;
    }
}