using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using VirulentVentures;

public class CombatViewController : MonoBehaviour
{
    [SerializeField] private VisualConfig visualConfig;
    [SerializeField] private UIConfig uiConfig;
    [SerializeField] private EventBusSO eventBus;
    [SerializeField] private CharacterPositions characterPositions;
    private VisualElement root;
    private Dictionary<ICombatUnit, GameObject> unitGameObjects = new Dictionary<ICombatUnit, GameObject>();
    private GameObject backgroundGameObject;

    void Awake()
    {
        if (!ValidateReferences()) return;
        SetupUI();
        eventBus.OnCombatInitialized += InitializeCombat;
        eventBus.OnUnitAttacking += HandleUnitAttacking;
        eventBus.OnUnitDamaged += HandleUnitDamaged;
    }

    void OnDestroy()
    {
        eventBus.OnCombatInitialized -= InitializeCombat;
        eventBus.OnUnitAttacking -= HandleUnitAttacking;
        eventBus.OnUnitDamaged -= HandleUnitDamaged;
        if (backgroundGameObject != null)
        {
            Destroy(backgroundGameObject);
        }
    }

    private void SetupUI()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null)
        {
            Debug.LogError("CombatViewController: UIDocument rootVisualElement is null! Ensure CombatScene.uxml is assigned to UIDocument in the Inspector.");
            return;
        }

        var combatRoot = root.Q<VisualElement>("combat-root");
        if (combatRoot == null)
        {
            Debug.LogError("CombatViewController: combat-root not found in UXML!");
            return;
        }

        var bottomPanel = combatRoot.Q<VisualElement>("bottom-panel");
        if (bottomPanel == null)
        {
            Debug.LogError("CombatViewController: bottom-panel not found in UXML!");
            return;
        }

        // Debug to confirm panel is found and styled
        Debug.Log($"CombatViewController: bottom-panel found. Resolved size: {bottomPanel.resolvedStyle.width}x{bottomPanel.resolvedStyle.height}, Position: {bottomPanel.resolvedStyle.position}, Display: {bottomPanel.resolvedStyle.display}");
    }

    private void InitializeCombat(EventBusSO.CombatInitData data)
    {
        unitGameObjects.Clear();

        var heroes = data.units.Where(u => u.stats.isHero).ToList();
        var monsters = data.units.Where(u => !u.stats.isHero).ToList();

        // Setup Hero Sprites
        for (int i = 0; i < heroes.Count && i < characterPositions.heroPositions.Length; i++)
        {
            var unit = heroes[i].unit;
            var stats = heroes[i].stats;
            var go = CreateUnitGameObject(unit, stats, true, characterPositions.heroPositions[i]);
            unitGameObjects[unit] = go;
        }

        // Setup Monster Sprites
        for (int i = 0; i < monsters.Count && i < characterPositions.monsterPositions.Length; i++)
        {
            var unit = monsters[i].unit;
            var stats = monsters[i].stats;
            var go = CreateUnitGameObject(unit, stats, false, characterPositions.monsterPositions[i]);
            unitGameObjects[unit] = go;
        }
    }

    private void HandleUnitAttacking(EventBusSO.AttackData data)
    {
        if (unitGameObjects.TryGetValue(data.attacker, out GameObject attackerGo))
        {
            var animator = attackerGo.GetComponent<SpriteAnimation>();
            if (animator != null)
            {
                bool isHero = data.attacker is CharacterStats charStats && charStats.Type == CharacterType.Hero;
                animator.TiltForward(isHero);
            }
        }
    }

    private void HandleUnitDamaged(EventBusSO.DamagePopupData data)
    {
        if (unitGameObjects.TryGetValue(data.unit, out GameObject targetGo))
        {
            var animator = targetGo.GetComponent<SpriteAnimation>();
            if (animator != null)
            {
                animator.Jiggle();
            }
        }
    }

    private GameObject CreateUnitGameObject(ICombatUnit unit, CharacterStats.DisplayStats stats, bool isHero, Vector3 position)
    {
        var go = new GameObject(stats.name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = isHero ? visualConfig.GetCombatSprite(stats.name) : visualConfig.GetEnemySprite(stats.name);
        sr.sortingLayerName = "Characters"; // Ensure above Background layer
        sr.sortingOrder = 1;
        go.transform.position = position;
        go.AddComponent<SpriteAnimation>();
        return go;
    }

    private bool ValidateReferences()
    {
        if (visualConfig == null || uiConfig == null || eventBus == null || characterPositions == null)
        {
            Debug.LogError($"CombatViewController: Missing references! VisualConfig: {visualConfig != null}, UIConfig: {uiConfig != null}, EventBus: {eventBus != null}, CharacterPositions: {characterPositions != null}");
            return false;
        }
        return true;
    }
}