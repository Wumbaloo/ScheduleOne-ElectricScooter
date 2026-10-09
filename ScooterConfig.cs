using UnityEngine;

namespace ElectricScooterMod;

/// <summary>Valeur fixe (réglages intégrés au mod, aucun fichier de configuration).</summary>
public sealed class Setting<T>
{
    public T Value { get; }
    public Setting(T value) => Value = value;
}

/// <summary>Réglages intégrés du mod (posture du pilote, modèle, moteur).</summary>
public static class ScooterConfig
{
    // Trottinette en mode "conduite"
    public static readonly Setting<float> Scale = new(0.5f);
    public static readonly Setting<float> Yaw = new(180f);
    public static readonly Setting<float> OffsetX = new(0f);
    public static readonly Setting<float> OffsetY = new(0.06f);
    public static readonly Setting<float> OffsetZ = new(0f);

    // Correction de rotation appliquée aux mains (par-dessus la rotation des sockets Hand_L / Hand_R)
    public static readonly Setting<float> HandRotX = new(90f);
    public static readonly Setting<float> HandRotY = new(0f);
    public static readonly Setting<float> HandRotZ = new(0f);

    // Corps du joueur sur la trottinette
    public static readonly Setting<float> BodyOffsetY = new(-0.1f);
    public static readonly Setting<float> BodyOffsetZ = new(0f);
    public static readonly Setting<bool> FreezeBodyDuringPush = new(true);
    public static readonly Setting<float> TorsoYaw = new(0f);
    public static readonly Setting<float> FootYaw = new(180f);
    public static readonly Setting<float> FootOffsetY = new(-0.035f);

    // Positions fines des mains (en mètres, dans les axes du board : X droite, Y haut, Z avant)
    public static readonly Setting<float> LeftHandOffsetX = new(0f);
    public static readonly Setting<float> LeftHandOffsetY = new(0.025f);
    public static readonly Setting<float> LeftHandOffsetZ = new(-0.075f);
    public static readonly Setting<float> RightHandOffsetX = new(0f);
    public static readonly Setting<float> RightHandOffsetY = new(0.025f);
    public static readonly Setting<float> RightHandOffsetZ = new(-0.05f);

    // Coudes (cibles de flexion des bras, offset par rapport à la main, axes du board)
    public static readonly Setting<bool> ElbowsEnabled = new(true);
    public static readonly Setting<float> ElbowWeight = new(1f);
    public static readonly Setting<float> LeftElbowOffsetX = new(-0.9f);
    public static readonly Setting<float> LeftElbowOffsetY = new(-0.8f);
    public static readonly Setting<float> LeftElbowOffsetZ = new(-0.2f);
    public static readonly Setting<float> RightElbowOffsetX = new(0.5f);
    public static readonly Setting<float> RightElbowOffsetY = new(-0.3f);
    public static readonly Setting<float> RightElbowOffsetZ = new(-0.2f);

    // Moteur
    public static readonly Setting<bool> MotorEnabled = new(true);
    public static readonly Setting<float> MotorEngageSpeedKmh = new(16f);
    public static readonly Setting<float> MotorTopSpeedKmh = new(40f);
    public static readonly Setting<float> MotorAcceleration = new(5f);

    // Trottinette tenue en main (viewmodel première personne)
    public static readonly Setting<float> HeldScale = new(0.2f);
    public static readonly Setting<float> HeldYaw = new(180f);
    public static readonly Setting<float> HeldOffsetX = new(-0.1f);
    public static readonly Setting<float> HeldOffsetY = new(-0.05f);
    public static readonly Setting<float> HeldOffsetZ = new(0f);

    public static Vector3 Offset => new Vector3(OffsetX.Value, OffsetY.Value, OffsetZ.Value);
    public static Quaternion HandRotation => Quaternion.Euler(HandRotX.Value, HandRotY.Value, HandRotZ.Value);
    public static Vector3 HeldOffset => new Vector3(HeldOffsetX.Value, HeldOffsetY.Value, HeldOffsetZ.Value);
    public static Vector3 LeftHandOffset => new Vector3(LeftHandOffsetX.Value, LeftHandOffsetY.Value, LeftHandOffsetZ.Value);
    public static Vector3 RightHandOffset => new Vector3(RightHandOffsetX.Value, RightHandOffsetY.Value, RightHandOffsetZ.Value);
    public static Vector3 LeftElbowOffset => new Vector3(LeftElbowOffsetX.Value, LeftElbowOffsetY.Value, LeftElbowOffsetZ.Value);
    public static Vector3 RightElbowOffset => new Vector3(RightElbowOffsetX.Value, RightElbowOffsetY.Value, RightElbowOffsetZ.Value);
}
