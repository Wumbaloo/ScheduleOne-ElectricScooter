using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(ElectricScooterMod.Core), "ElectricScooter", "0.1.0", "Wumbaloo", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ElectricScooterMod;

public class Core : MelonMod
{
    public static Core Instance { get; private set; }

    private bool _legacyInputUnavailable;

    public override void OnInitializeMelon()
    {
        Instance = this;
        ScooterConfig.Init();
        HarmonyInstance.PatchAll();
        LoggerInstance.Msg("Successfully loaded! (F9 in game = reload ElectricScooter settings)");
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

    public override void OnUpdate()
    {
        if (_legacyInputUnavailable)
            return;

        try
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                ScooterConfig.Reload();
                ScooterVisual.Refresh();
                LoggerInstance.Msg("Settings reloaded.");
            }
        }
        catch (Exception)
        {
            // Input legacy désactivé dans le jeu : on abandonne le raccourci
            _legacyInputUnavailable = true;
            LoggerInstance.Warning("Legacy input unavailable, F9 reload disabled (restart the game to apply settings).");
        }
    }
}
