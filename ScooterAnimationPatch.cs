using HarmonyLib;
using Il2CppScheduleOne.Skating;
using MelonLoader;

namespace ElectricScooterMod.Patches;

/// <summary>
/// Prefix : fige le torse (annule l'avancée du bassin de l'animation de poussée).
/// Postfix : décale le corps, positionne mains et pieds sur la trottinette.
/// Les cibles sont réinitialisées dans Update : on repasse donc à chaque frame.
/// </summary>
[HarmonyPatch(typeof(SkateboardAnimation), "LateUpdate")]
internal static class ScooterAnimationPatch
{
    private static bool _prefixErrorLogged;
    private static bool _postfixErrorLogged;

    [HarmonyPrefix]
    public static void Prefix(SkateboardAnimation __instance)
    {
        if (ScooterVisual.Active == null)
            return;

        try
        {
            ScooterVisual.LockBody(__instance);
        }
        catch (Exception ex)
        {
            if (_prefixErrorLogged) return;
            _prefixErrorLogged = true;
            Core.Instance.LoggerInstance.Error($"ScooterAnimationPatch.Prefix error (logged once): {ex}");
        }
    }

    [HarmonyPostfix]
    public static void Postfix(SkateboardAnimation __instance)
    {
        if (ScooterVisual.Active == null)
            return;

        try
        {
            ScooterVisual.ApplyIK(__instance);
        }
        catch (Exception ex)
        {
            if (_postfixErrorLogged) return;
            _postfixErrorLogged = true;
            Core.Instance.LoggerInstance.Error($"ScooterAnimationPatch.Postfix error (logged once): {ex}");
        }
    }
}
