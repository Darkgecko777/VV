using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace VirulentVentures
{
    public static class AbilityFactory
    {
        public static Dictionary<string, Type> abilityTypes { get; private set; }

        public static void Initialize()
        {
            if (abilityTypes != null) return;
            abilityTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IAbility).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);
        }

        public static IAbility[] GetAbilities(string[] abilityIds)
        {
            Initialize();
            if (abilityIds == null) return new IAbility[0];
            var abilities = new List<IAbility>();
            foreach (var id in abilityIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                if (abilityTypes.TryGetValue(id, out var type))
                {
                    try
                    {
                        var ability = Activator.CreateInstance(type) as IAbility;
                        if (ability != null && string.Equals(ability.Id, id, StringComparison.OrdinalIgnoreCase))
                        {
                            abilities.Add(ability);
                        }
                        else
                        {
                            Debug.LogWarning($"AbilityFactory: Created ability {id} has mismatched Id or is null.");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"AbilityFactory: Failed to create ability {id}. Error: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"AbilityFactory: Unknown ability ID {id}.");
                }
            }
            return abilities.ToArray();
        }

        public static List<string> GetAvailableAbilityIds()
        {
            Initialize();
            return abilityTypes.Keys.ToList();
        }

        public static bool HasAbility(string id)
        {
            Initialize();
            return !string.IsNullOrEmpty(id) && abilityTypes.ContainsKey(id);
        }
    }
}