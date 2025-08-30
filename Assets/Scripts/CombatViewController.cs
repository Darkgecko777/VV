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
            Debug.LogError("CombatViewController: UIDocument rootVisualElement is null!");
            return;
        }

        // Setup background as SpriteRenderer
        Sprite background = visualConfig.GetCombatBackground();
        if (background != null)
        {
            backgroundGameObject = new GameObject("CombatBackground");
            var sr = backgroundGameObject.AddComponent<SpriteRenderer>();
            sr.sprite = background;
            sr.sortingLayerName = "Background";
            sr.sortingOrder = -1;
            // Position to center of top half, scale to fill orthographic view (size 5)
            backgroundGameObject.transform.position = new Vector3(0, 2.5f, 0); // Top half center (orthographic size 5, Y=0 to Y=5)
            float pixelsPerUnit = background.pixelsPerUnit;
            float spriteWidth = background.rect.width / pixelsPerUnit;
            float spriteHeight = background.rect.height / pixelsPerUnit;
            float scaleX = 10f / spriteWidth; // Orthographic width = 10 (size 5 * 2)
            float scaleY = 5f / spriteHeight; // Top half height = 5
            backgroundGameObject.transform.localScale = new Vector3(scaleX, scaleY, 1);
        }
        else
        {
            Debug.LogWarning("CombatViewController: Failed to load combat background sprite!");
        }

        // Setup minimalist UI (combat-root and bottom-panel)
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