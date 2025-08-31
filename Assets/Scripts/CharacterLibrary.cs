using System.Collections.Generic;
using UnityEngine;

namespace VirulentVentures
{
    public static class CharacterLibrary
    {
        public struct CharacterData
        {
            public string Id;
            public int Health;
            public int MaxHealth;
            public int Attack;
            public int Defense;
            public int Speed;
            public int Evasion;
            public int Morale;
            public int MaxMorale;
            public List<string> AbilityIds;
            public bool CanBeCultist;
            public int PartyPosition;

            public CharacterStats.DisplayStats GetDisplayStats(bool isHero)
            {
                return new CharacterStats.DisplayStats(
                    name: Id,
                    health: Health,
                    maxHealth: MaxHealth,
                    attack: Attack,
                    defense: Defense,
                    speed: Mathf.Clamp(Speed, 1, 8),
                    evasion: Mathf.Clamp(Evasion, 0, 100),
                    morale: Mathf.Clamp(Morale, 0, MaxMorale),
                    maxMorale: MaxMorale,
                    isHero: isHero
                );
            }
        }

        private static readonly Dictionary<string, CharacterData> HeroData = new Dictionary<string, CharacterData>
        {
            {
                "Fighter", new CharacterData
                {
                    Id = "Fighter",
                    Health = 80,
                    MaxHealth = 100,
                    Attack = 25,
                    Defense = 15,
                    Speed = 5,
                    Evasion = 20,
                    Morale = 100,
                    MaxMorale = 100,
                    AbilityIds = new List<string> { "BasicAttack", "FighterAttack" },
                    CanBeCultist = true,
                    PartyPosition = 1
                }
            },
            {
                "Healer", new CharacterData
                {
                    Id = "Healer",
                    Health = 60,
                    MaxHealth = 80,
                    Attack = 10,
                    Defense = 5,
                    Speed = 3,
                    Evasion = 30,
                    Morale = 100,
                    MaxMorale = 100,
                    AbilityIds = new List<string> { "BasicAttack", "HealerHeal" },
                    CanBeCultist = false,
                    PartyPosition = 2
                }
            },
            {
                "Scout", new CharacterData
                {
                    Id = "Scout",
                    Health = 65,
                    MaxHealth = 85,
                    Attack = 15,
                    Defense = 10,
                    Speed = 6,
                    Evasion = 40,
                    Morale = 100,
                    MaxMorale = 100,
                    AbilityIds = new List<string> { "BasicAttack", "ScoutDefend" },
                    CanBeCultist = true,
                    PartyPosition = 3
                }
            },
            {
                "TreasureHunter", new CharacterData
                {
                    Id = "TreasureHunter",
                    Health = 70,
                    MaxHealth = 90,
                    Attack = 20,
                    Defense = 12,
                    Speed = 4,
                    Evasion = 35,
                    Morale = 100,
                    MaxMorale = 100,
                    AbilityIds = new List<string> { "BasicAttack", "TreasureFind" },
                    CanBeCultist = false,
                    PartyPosition = 4
                }
            }
        };

        private static readonly Dictionary<string, CharacterData> MonsterData = new Dictionary<string, CharacterData>
        {
            {
                "Ghoul", new CharacterData
                {
                    Id = "Ghoul",
                    Health = 50,
                    MaxHealth = 70,
                    Attack = 15,
                    Defense = 10,
                    Speed = 4,
                    Evasion = 25,
                    Morale = 80,
                    MaxMorale = 80,
                    AbilityIds = new List<string> { "BasicAttack", "GhoulClaw" },
                    CanBeCultist = false,
                    PartyPosition = 0
                }
            },
            {
                "Wraith", new CharacterData
                {
                    Id = "Wraith",
                    Health = 60,
                    MaxHealth = 80,
                    Attack = 18,
                    Defense = 5,
                    Speed = 6,
                    Evasion = 40,
                    Morale = 90,
                    MaxMorale = 90,
                    AbilityIds = new List<string> { "BasicAttack", "WraithStrike" },
                    CanBeCultist = false,
                    PartyPosition = 0
                }
            },
            {
                "Skeleton", new CharacterData
                {
                    Id = "Skeleton",
                    Health = 55,
                    MaxHealth = 75,
                    Attack = 12,
                    Defense = 15,
                    Speed = 3,
                    Evasion = 10,
                    Morale = 70,
                    MaxMorale = 70,
                    AbilityIds = new List<string> { "BasicAttack", "SkeletonSlash" },
                    CanBeCultist = false,
                    PartyPosition = 0
                }
            },
            {
                "Vampire", new CharacterData
                {
                    Id = "Vampire",
                    Health = 65,
                    MaxHealth = 85,
                    Attack = 22,
                    Defense = 8,
                    Speed = 5,
                    Evasion = 30,
                    Morale = 85,
                    MaxMorale = 85,
                    AbilityIds = new List<string> { "BasicAttack", "VampireBite" },
                    CanBeCultist = false,
                    PartyPosition = 0
                }
            }
        };

        public static CharacterData GetHeroData(string id)
        {
            if (HeroData.TryGetValue(id, out var data))
            {
                return data;
            }
            Debug.LogWarning($"CharacterLibrary: Hero ID {id} not found, returning default");
            return new CharacterData
            {
                Id = id,
                Health = 50,
                MaxHealth = 50,
                Attack = 10,
                Defense = 5,
                Speed = 3,
                Evasion = 10,
                Morale = 100,
                MaxMorale = 100,
                AbilityIds = new List<string> { "BasicAttack" },
                CanBeCultist = false,
                PartyPosition = 1
            };
        }

        public static CharacterData GetMonsterData(string id)
        {
            if (MonsterData.TryGetValue(id, out var data))
            {
                return data;
            }
            Debug.LogWarning($"CharacterLibrary: Monster ID {id} not found, returning default");
            return new CharacterData
            {
                Id = id,
                Health = 50,
                MaxHealth = 50,
                Attack = 10,
                Defense = 5,
                Speed = 3,
                Evasion = 10,
                Morale = 80,
                MaxMorale = 80,
                AbilityIds = new List<string> { "BasicAttack" },
                CanBeCultist = false,
                PartyPosition = 0
            };
        }

        public static List<string> GetMonsterIds()
        {
            return new List<string>(MonsterData.Keys);
        }
    }
}