using UnityEngine;

public class HeroStats : BaseCharacterStats
{
    private const float bogRotMoraleDrain = 5f;
    private const float bogRotSpreadChance = 0.15f; // Lower spread chance for heroes

    void Awake()
    {
        characterType = CharacterType.Fighter;
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