using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne;
using Il2CppScheduleOne.Skating;
using MelonLoader;
using UnityEngine;

namespace ElectricScooterMod;

/// <summary>
/// Assistance électrique : au-dessus d'une vitesse seuil, la trottinette accélère seule tant que la touche
/// avant est maintenue, et le corps du pilote est figé (plus de poussée).
/// Aucune modification de _settings / SkateboardData (potentiellement partagés avec les skates vanilla).
/// </summary>
public static class ScooterMotor
{
    private const float DisengageMarginKmh = 2f;
    private const float FreezeDuration = 0.25f;

    /// <summary>Le moteur a pris le relais (hystérésis).</summary>
    public static bool Engaged { get; private set; }

    /// <summary>0 = animation normale, 1 = corps entièrement figé.</summary>
    public static float FreezeBlend { get; private set; }

    public static void Reset()
    {
        Engaged = false;
        FreezeBlend = 0f;
    }

    public static bool IsScooterBoard(Skateboard board)
    {
        var rig = ScooterVisual.Active;
        return rig != null && board != null && board.GetInstanceID() == rig.BoardId;
    }

    /// <summary>Vitesse (km/h) projetée sur l'avant du board, mise à plat.</summary>
    private static float ForwardSpeedKmh(Skateboard board, out Vector3 flatForward)
    {
        flatForward = Vector3.ProjectOnPlane(board.transform.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = board.transform.forward;
        flatForward.Normalize();
        return Vector3.Dot(board.Rb.velocity, flatForward) * 3.6f;
    }

    /// <summary>Appelé après Skateboard.FixedUpdate du board de la trottinette (propriétaire uniquement).</summary>
    public static void Tick(Skateboard board)
    {
        if (!ScooterConfig.MotorEnabled.Value)
        {
            Engaged = false;
            return;
        }

        float speed = ForwardSpeedKmh(board, out var forward);
        float engage = ScooterConfig.MotorEngageSpeedKmh.Value;

        if (!Engaged && speed >= engage)
            Engaged = true;
        else if (Engaged && speed < engage - DisengageMarginKmh)
            Engaged = false;

        if (!Engaged || !board.isGrounded)
            return;

        var equippable = board.Equippable;
        if (equippable == null || !equippable.State.IsAcceptingInput)
            return;
        if (GameInput.VehicleDriveAxis <= 0.1f)
            return;

        float top = ScooterConfig.MotorTopSpeedKmh.Value;
        if (speed >= top)
            return;

        // Réduit progressivement la poussée à l'approche de la vitesse max
        float taper = Mathf.Clamp01((top - speed) / Mathf.Max(1f, top * 0.2f));
        board.Rb.AddForce(forward * ScooterConfig.MotorAcceleration.Value * taper, ForceMode.Acceleration);
    }

    /// <summary>Appelé chaque frame depuis LockBody : fait évoluer FreezeBlend vers sa cible.</summary>
    public static void UpdateFreezeBlend(float deltaTime)
    {
        float target = Engaged ? 1f : 0f;
        FreezeBlend = Mathf.MoveTowards(FreezeBlend, target, deltaTime / FreezeDuration);
    }

    // ---------- Accès à Skateboard.pushQueued (privé) ----------

    private static PropertyInfo _pushQueuedProp;
    private static FieldInfo _pushQueuedField;
    private static bool _pushQueuedResolved;

    public static void ClearPushQueued(Skateboard board)
    {
        if (!_pushQueuedResolved)
        {
            _pushQueuedResolved = true;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            _pushQueuedProp = typeof(Skateboard).GetProperty("pushQueued", flags);
            if (_pushQueuedProp == null)
                _pushQueuedField = typeof(Skateboard).GetField("pushQueued", flags);
            if (_pushQueuedProp == null && _pushQueuedField == null)
                Core.Instance.LoggerInstance.Warning("Skateboard.pushQueued not found: ghost push possible on disengage.");
        }

        if (_pushQueuedProp != null && _pushQueuedProp.CanWrite)
            _pushQueuedProp.SetValue(board, false);
        else if (_pushQueuedField != null)
            _pushQueuedField.SetValue(board, false);
    }
}

[HarmonyPatch(typeof(Skateboard), "FixedUpdate")]
internal static class ScooterMotorFixedUpdatePatch
{
    private static bool _errorLogged;

    [HarmonyPostfix]
    public static void Postfix(Skateboard __instance)
    {
        if (ScooterVisual.Active == null || !ScooterMotor.IsScooterBoard(__instance))
            return;

        try
        {
            if (__instance.IsOwner)
                ScooterMotor.Tick(__instance);
        }
        catch (Exception ex)
        {
            if (_errorLogged) return;
            _errorLogged = true;
            Core.Instance.LoggerInstance.Error($"ScooterMotor.Tick error (logged once): {ex}");
        }
    }
}

[HarmonyPatch(typeof(Skateboard), "Push")]
internal static class ScooterMotorPushPatch
{
    private static bool _errorLogged;

    /// <summary>Moteur engagé : on annule la poussée (et le push en attente) pour éviter un push fantôme.</summary>
    [HarmonyPrefix]
    public static bool Prefix(Skateboard __instance)
    {
        if (!ScooterMotor.Engaged || ScooterVisual.Active == null || !ScooterMotor.IsScooterBoard(__instance))
            return true;

        try
        {
            ScooterMotor.ClearPushQueued(__instance);
        }
        catch (Exception ex)
        {
            if (!_errorLogged)
            {
                _errorLogged = true;
                Core.Instance.LoggerInstance.Error($"ScooterMotorPushPatch error (logged once): {ex}");
            }
        }
        return false;
    }
}
