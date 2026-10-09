using HarmonyLib;
using Il2CppScheduleOne.Dialogue;
using MelonLoader;

namespace ElectricScooterMod.Patches;

/// <summary>
/// Ajoute la trottinette aux options du vendeur de skateboards (Shred Shack).
/// ModifyChoiceList est appelé à l'ouverture du dialogue ; on s'assure donc que l'option
/// existe juste avant, sans dépendre du moment où la scène/le PNJ est chargé.
/// </summary>
[HarmonyPatch(typeof(DialogueController_SkateboardSeller), "ModifyChoiceList")]
internal static class SkateboardSellerPatch
{
    [HarmonyPrefix]
    public static void Prefix(DialogueController_SkateboardSeller __instance, string dialogueLabel)
    {
        try
        {
            if (dialogueLabel != "ENTRY")
                return;

            EnsureOption(__instance);
        }
        catch (Exception ex)
        {
            Core.Instance.LoggerInstance.Error($"SkateboardSellerPatch error: {ex}");
        }
    }

    private static void EnsureOption(DialogueController_SkateboardSeller seller)
    {
        if (ScooterItem.Definition == null)
            ScooterItem.Init();
        if (ScooterItem.Definition == null)
            return;

        var options = seller.Options;
        if (options == null)
            return;

        for (int i = 0; i < options.Count; i++)
        {
            if (options[i] != null && options[i].Name == ScooterItem.DisplayName)
                return;
        }

        options.Add(new DialogueController_SkateboardSeller.Option
        {
            Name = ScooterItem.DisplayName,
            Price = ScooterItem.Price,
            IsAvailable = true,
            NotAvailableReason = string.Empty,
            Item = ScooterItem.Definition
        });

        Core.Instance.LoggerInstance.Msg($"Added '{ScooterItem.DisplayName}' to the skateboard seller ({options.Count} options).");
    }
}
