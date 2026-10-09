using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(ElectricScooterMod.Core), "ElectricScooter", "0.1.0", "Wumbaloo", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ElectricScooterMod;

public class Core : MelonMod
{
    public static Core Instance { get; private set; }

    public override void OnInitializeMelon()
    {
        Instance = this;
        HarmonyInstance.PatchAll();
        LoggerInstance.Msg("Successfully loaded!");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName != "Main")
            return;

        ScooterVisual.Clear();

        // Nouvelle session : l'item doit être enregistré avant le chargement de la sauvegarde
        try
        {
            ScooterItem.Init();
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Error initializing scooter item on scene load: {ex}");
        }
    }
}
