using MelonLoader;
using UnityEngine;

namespace ElectricScooterMod;

/// <summary>
/// Réglages modifiables dans UserData\MelonPreferences.cfg (catégorie ElectricScooter).
/// En jeu, F9 relit le fichier et réapplique les valeurs sur la trottinette active.
/// </summary>
public static class ScooterConfig
{
    private static MelonPreferences_Category _category;

    // Trottinette en mode "conduite"
    public static MelonPreferences_Entry<float> Scale;
    public static MelonPreferences_Entry<float> Yaw;
    public static MelonPreferences_Entry<float> OffsetX;
    public static MelonPreferences_Entry<float> OffsetY;
    public static MelonPreferences_Entry<float> OffsetZ;

    // Correction de rotation appliquée aux mains (par-dessus la rotation des sockets Hand_L / Hand_R)
    public static MelonPreferences_Entry<float> HandRotX;
    public static MelonPreferences_Entry<float> HandRotY;
    public static MelonPreferences_Entry<float> HandRotZ;

    // Corps du joueur sur la trottinette
    public static MelonPreferences_Entry<float> BodyOffsetY;
    public static MelonPreferences_Entry<float> BodyOffsetZ;
    public static MelonPreferences_Entry<bool> FreezeBodyDuringPush;
    public static MelonPreferences_Entry<float> TorsoYaw;
    public static MelonPreferences_Entry<float> FootYaw;
    public static MelonPreferences_Entry<float> FootOffsetY;

    // Positions fines des mains (en mètres, dans les axes du board : X droite, Y haut, Z avant)
    public static MelonPreferences_Entry<float> LeftHandOffsetX;
    public static MelonPreferences_Entry<float> LeftHandOffsetY;
    public static MelonPreferences_Entry<float> LeftHandOffsetZ;
    public static MelonPreferences_Entry<float> RightHandOffsetX;
    public static MelonPreferences_Entry<float> RightHandOffsetY;
    public static MelonPreferences_Entry<float> RightHandOffsetZ;

    // Coudes (cibles de flexion des bras, offset par rapport à la main, axes du board : X droite, Y haut, Z avant)
    public static MelonPreferences_Entry<bool> ElbowsEnabled;
    public static MelonPreferences_Entry<float> ElbowWeight;
    public static MelonPreferences_Entry<float> LeftElbowOffsetX;
    public static MelonPreferences_Entry<float> LeftElbowOffsetY;
    public static MelonPreferences_Entry<float> LeftElbowOffsetZ;
    public static MelonPreferences_Entry<float> RightElbowOffsetX;
    public static MelonPreferences_Entry<float> RightElbowOffsetY;
    public static MelonPreferences_Entry<float> RightElbowOffsetZ;

    public static Vector3 LeftElbowOffset => new Vector3(LeftElbowOffsetX.Value, LeftElbowOffsetY.Value, LeftElbowOffsetZ.Value);
    public static Vector3 RightElbowOffset => new Vector3(RightElbowOffsetX.Value, RightElbowOffsetY.Value, RightElbowOffsetZ.Value);

    // Moteur
    public static MelonPreferences_Entry<bool> MotorEnabled;
    public static MelonPreferences_Entry<float> MotorEngageSpeedKmh;
    public static MelonPreferences_Entry<float> MotorTopSpeedKmh;
    public static MelonPreferences_Entry<float> MotorAcceleration;

    // Trottinette tenue en main (viewmodel première personne)
    public static MelonPreferences_Entry<float> HeldScale;
    public static MelonPreferences_Entry<float> HeldYaw;
    public static MelonPreferences_Entry<float> HeldOffsetX;
    public static MelonPreferences_Entry<float> HeldOffsetY;
    public static MelonPreferences_Entry<float> HeldOffsetZ;

    public static Vector3 Offset => new Vector3(OffsetX.Value, OffsetY.Value, OffsetZ.Value);
    public static Quaternion HandRotation => Quaternion.Euler(HandRotX.Value, HandRotY.Value, HandRotZ.Value);
    public static Vector3 HeldOffset => new Vector3(HeldOffsetX.Value, HeldOffsetY.Value, HeldOffsetZ.Value);
    public static Vector3 LeftHandOffset => new Vector3(LeftHandOffsetX.Value, LeftHandOffsetY.Value, LeftHandOffsetZ.Value);
    public static Vector3 RightHandOffset => new Vector3(RightHandOffsetX.Value, RightHandOffsetY.Value, RightHandOffsetZ.Value);

    public static void Init()
    {
        _category = MelonPreferences.CreateCategory("ElectricScooter", "Electric Scooter");

        Scale = _category.CreateEntry("ScooterScale", 0.5f, description: "Scale of the scooter prefab while riding.");
        Yaw = _category.CreateEntry("ScooterYaw", 180f, description: "Rotation (degrees, around Y) of the prefab relative to the board. The prefab front is -Z, the board front is +Z.");
        OffsetX = _category.CreateEntry("ScooterOffsetX", 0f, description: "Local offset of the prefab relative to the board (X).");
        OffsetY = _category.CreateEntry("ScooterOffsetY", 0.06f, description: "Local offset of the prefab relative to the board (Y). Raise/lower to make the wheels touch the ground.");
        OffsetZ = _category.CreateEntry("ScooterOffsetZ", 0f, description: "Local offset of the prefab relative to the board (Z).");

        HandRotX = _category.CreateEntry("HandRotX", 90f, description: "Extra rotation (degrees) applied to both hands, X.");
        HandRotY = _category.CreateEntry("HandRotY", 0f, description: "Extra rotation (degrees) applied to both hands, Y.");
        HandRotZ = _category.CreateEntry("HandRotZ", 0f, description: "Extra rotation (degrees) applied to both hands, Z.");

        BodyOffsetY = _category.CreateEntry("BodyOffsetY", -0.1f, description: "Vertical shift of the rider's body (pelvis, spine, knees) relative to the board, in meters. Negative = lower.");
        BodyOffsetZ = _category.CreateEntry("BodyOffsetZ", 0f, description: "Forward/backward shift of the rider's body relative to the board, in meters. Positive = forward.");
        FreezeBodyDuringPush = _category.CreateEntry("FreezeBodyDuringPush", true, description: "Keep the torso still during the push animation (only the feet move).");
        TorsoYaw = _category.CreateEntry("TorsoYaw", 0f, description: "Rotation (degrees, around the vertical axis) applied to the rider's pelvis/spine. The skateboard stance is sideways; 90 makes the rider face forward. Use -90 if he faces backward.");
        FootYaw = _category.CreateEntry("FootYaw", 180f, description: "Rotation (degrees, around the vertical axis) applied to both feet (and knee hints). 90 = toes pointing forward.");
        FootOffsetY = _category.CreateEntry("FootOffsetY", -0.035f, description: "Vertical shift of both feet relative to the Foot_Pos socket, in meters. Negative = lower (feet closer to the deck).");

        LeftHandOffsetX = _category.CreateEntry("LeftHandOffsetX", 0f, description: "Fine position of the left hand, meters, board axes (X = right).");
        LeftHandOffsetY = _category.CreateEntry("LeftHandOffsetY", 0.025f, description: "Fine position of the left hand, meters, board axes (Y = up).");
        LeftHandOffsetZ = _category.CreateEntry("LeftHandOffsetZ", -0.075f, description: "Fine position of the left hand, meters, board axes (Z = forward).");
        RightHandOffsetX = _category.CreateEntry("RightHandOffsetX", 0f, description: "Fine position of the right hand, meters, board axes (X = right).");
        RightHandOffsetY = _category.CreateEntry("RightHandOffsetY", 0.025f, description: "Fine position of the right hand, meters, board axes (Y = up).");
        RightHandOffsetZ = _category.CreateEntry("RightHandOffsetZ", -0.05f, description: "Fine position of the right hand, meters, board axes (Z = forward).");

        ElbowsEnabled = _category.CreateEntry("ElbowsEnabled", true, description: "Control the elbow direction with the ElbowOffset entries. Set to false to let the game's IK place the elbows.");
        ElbowWeight = _category.CreateEntry("ElbowWeight", 1f, description: "How strongly the elbows are pulled toward their target (0 to 1).");
        LeftElbowOffsetX = _category.CreateEntry("LeftElbowOffsetX", -0.9f, description: "Left elbow target relative to the left hand, meters, board axes (X = right). Negative = outward.");
        LeftElbowOffsetY = _category.CreateEntry("LeftElbowOffsetY", -0.8f, description: "Left elbow target relative to the left hand, meters, board axes (Y = up).");
        LeftElbowOffsetZ = _category.CreateEntry("LeftElbowOffsetZ", -0.2f, description: "Left elbow target relative to the left hand, meters, board axes (Z = forward). Negative = toward the rider.");
        RightElbowOffsetX = _category.CreateEntry("RightElbowOffsetX", 0.5f, description: "Right elbow target relative to the right hand, meters, board axes (X = right). Positive = outward.");
        RightElbowOffsetY = _category.CreateEntry("RightElbowOffsetY", -0.3f, description: "Right elbow target relative to the right hand, meters, board axes (Y = up).");
        RightElbowOffsetZ = _category.CreateEntry("RightElbowOffsetZ", -0.2f, description: "Right elbow target relative to the right hand, meters, board axes (Z = forward). Negative = toward the rider.");

        MotorEnabled =_category.CreateEntry("MotorEnabled", true, description: "Electric assist: above the engage speed the motor accelerates while the forward key is held, and the rider stops animating.");
        MotorEngageSpeedKmh = _category.CreateEntry("MotorEngageSpeedKmh", 16f, description: "Speed (km/h) above which the motor takes over. Below it, the rider pushes with his foot.");
        MotorTopSpeedKmh = _category.CreateEntry("MotorTopSpeedKmh", 40f, description: "Speed (km/h) up to which the motor accelerates.");
        MotorAcceleration = _category.CreateEntry("MotorAcceleration", 5f, description: "Motor acceleration in m/s2. Raise it if the scooter does not reach its top speed (ground friction is subtracted).");

        HeldScale = _category.CreateEntry("HeldScale", 0.2f, description: "Scale of the scooter when held in hand (first person).");
        HeldYaw = _category.CreateEntry("HeldYaw", 180f, description: "Rotation (degrees, around Y) of the scooter when held in hand.");
        HeldOffsetX = _category.CreateEntry("HeldOffsetX", -0.1f, description: "Local offset of the held scooter (X).");
        HeldOffsetY = _category.CreateEntry("HeldOffsetY", -0.05f, description: "Local offset of the held scooter (Y).");
        HeldOffsetZ = _category.CreateEntry("HeldOffsetZ", 0f, description: "Local offset of the held scooter (Z).");

        // Écrit les valeurs par défaut dans le fichier pour pouvoir les éditer
        MelonPreferences.Save();
    }

    public static void Reload()
    {
        MelonPreferences.Load();
    }
}
