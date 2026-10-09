using Il2CppScheduleOne;
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Skating;
using MelonLoader;
using UnityEngine;

namespace ElectricScooterMod;

/// <summary>
/// Crée l'item "Electric Scooter" en clonant la définition d'un skateboard existant
/// (le montage, le spawn réseau et le viewmodel restent gérés par Skateboard_Equippable).
/// </summary>
public static class ScooterItem
{
    public const string ID = "electric_scooter";
    public const string DisplayName = "Electric Scooter";
    public const string Description = "A battery-powered scooter. No pushing required.";
    public const float Price = 1500f;

    public static StorableItemDefinition Definition { get; private set; }

    private static GameObject _templateHolder;

    /// <summary>Vrai si l'instance d'item est notre trottinette.</summary>
    public static bool IsScooter(ItemInstance item)
    {
        return item != null && item.ID == ID;
    }

    /// <summary>Crée (une seule fois) puis enregistre l'item dans le Registry si besoin.</summary>
    public static void Init()
    {
        var registry = Registry.Instance;
        if (registry == null)
        {
            Core.Instance.LoggerInstance.Error("Registry not available, scooter item not created.");
            return;
        }

        if (Definition == null)
            Definition = CreateDefinition(registry);

        if (Definition == null)
            return;

        // Le Registry retire les items runtime à chaque changement de scène : on réenregistre
        if (!Registry.ItemExists(ID))
        {
            registry.AddToRegistry(Definition);
            Core.Instance.LoggerInstance.Msg($"Registered item '{ID}'.");
        }
    }

    private static StorableItemDefinition CreateDefinition(Registry registry)
    {
        var log = Core.Instance.LoggerInstance;

        // 1. Trouver les définitions dont l'équippable est un Skateboard_Equippable
        StorableItemDefinition source = null;
        var all = registry.GetAllItems();
        for (int i = 0; i < all.Count; i++)
        {
            var item = all[i];
            if (item == null || item.Equippable == null || item.ID == ID)
                continue;
            if (item.Equippable.TryCast<Skateboard_Equippable>() == null)
                continue;

            log.Msg($"Skateboard item found in registry: id='{item.ID}', name='{item.Name}'");

            var storable = item.TryCast<StorableItemDefinition>();
            if (storable == null)
                continue;

            // On préfère un skate "standard" au skate doré
            if (source == null || (source.ID.Contains("golden") && !storable.ID.Contains("golden")))
                source = storable;
        }

        if (source == null)
        {
            log.Error("No skateboard item definition found to clone.");
            return null;
        }

        log.Msg($"Cloning skateboard item '{source.ID}' for the scooter.");

        // 2. Cloner la définition
        var def = UnityEngine.Object.Instantiate(source);
        def.name = DisplayName;
        def.hideFlags = HideFlags.DontUnloadUnusedAsset;
        def.ID = ID;
        def.Name = DisplayName;
        def.Description = Description;
        def.BasePurchasePrice = Price;
        def.StackLimit = 1;

        // Icône d'inventaire : rendu du prefab de la trottinette (sinon on garde celle du skate cloné, déjà copiée)
        var icon = ScooterIcon.Create(ScooterAssets.Prefab);
        if (icon != null)
            def.Icon = icon;

        // 3. Cloner l'équippable. Le template est placé sous un parent inactif pour que
        //    ses Awake/Update ne tournent pas (le jeu l'instancie ensuite à l'équipement).
        if (_templateHolder == null)
        {
            _templateHolder = new GameObject("ElectricScooter_Templates");
            _templateHolder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(_templateHolder);
        }

        var equippableGo = UnityEngine.Object.Instantiate(source.Equippable.gameObject, _templateHolder.transform);
        equippableGo.name = "ElectricScooter_Equippable";
        def.Equippable = equippableGo.GetComponent<Equippable>();

        return def;
    }
}
