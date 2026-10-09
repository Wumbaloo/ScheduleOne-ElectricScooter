using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Skating;
using MelonLoader;

namespace ElectricScooterMod.Patches;

/// <summary>
/// Montage : à la fin d'OnMount, ActiveSkateboard est instancié et itemInstance est renseigné.
/// On remplace alors le visuel du skate par la trottinette.
/// </summary>
[HarmonyPatch(typeof(Skateboard_Equippable), "OnMount")]
internal static class ScooterMountPatch
{
    [HarmonyPostfix]
    public static void Postfix(Skateboard_Equippable __instance)
    {
        try
        {
            if (!ScooterItem.IsScooter(__instance.itemInstance))
                return;

            ScooterVisual.AttachToBoard(__instance.ActiveSkateboard);
        }
        catch (Exception ex)
        {
            Core.Instance.LoggerInstance.Error($"ScooterMountPatch error: {ex}");
        }
    }
}

/// <summary>Descente : le skate est détruit par le jeu (avec notre visuel enfant), on oublie juste les références.</summary>
[HarmonyPatch(typeof(Skateboard_Equippable), "OnDismount")]
internal static class ScooterDismountPatch
{
    [HarmonyPostfix]
    public static void Postfix(Skateboard_Equippable __instance)
    {
        try
        {
            if (ScooterItem.IsScooter(__instance.itemInstance))
                ScooterVisual.Clear();
        }
        catch (Exception ex)
        {
            Core.Instance.LoggerInstance.Error($"ScooterDismountPatch error: {ex}");
        }
    }
}

/// <summary>Équipement (trottinette tenue en main) : remplace le modèle du viewmodel.</summary>
[HarmonyPatch(typeof(Skateboard_Equippable), nameof(Skateboard_Equippable.Equip))]
internal static class ScooterEquipPatch
{
    [HarmonyPostfix]
    public static void Postfix(Skateboard_Equippable __instance, ItemInstance item)
    {
        try
        {
            if (!ScooterItem.IsScooter(item))
                return;

            ScooterVisual.AttachToHeld(__instance);
        }
        catch (Exception ex)
        {
            Core.Instance.LoggerInstance.Error($"ScooterEquipPatch error: {ex}");
        }
    }
}
