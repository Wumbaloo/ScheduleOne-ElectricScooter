using Il2CppRootMotion.FinalIK;
using Il2CppScheduleOne.Skating;
using UnityEngine;
using UnityEngine.Rendering;

namespace ElectricScooterMod;

/// <summary>Références de la trottinette actuellement utilisée par le joueur local.</summary>
public class ScooterRig
{
    public GameObject Root;
    public Transform HandL;
    public Transform HandR;
    public Transform FootPos;
    public Transform BoardRoot;
    public int AnimationId;
    public int BoardId;

    // Coudes : cibles de flexion (bendGoal) des solveurs de bras, et état d'origine à restaurer
    public Transform ElbowL;
    public Transform ElbowR;
    public BipedIK BodyIK;
    public IKSolverLimb.BendModifier OrigModL, OrigModR;
    public Transform OrigGoalL, OrigGoalR;
    public float OrigWeightL, OrigWeightR;
    public bool ElbowsOverridden;
}

public static class ScooterVisual
{
    /// <summary>Trottinette en cours d'utilisation (joueur local uniquement), ou null.</summary>
    public static ScooterRig Active { get; private set; }

    // ---------- Mode conduite ----------

    /// <summary>
    /// Masque le skateboard d'origine et accroche le prefab de trottinette sous SkateboardVisuals.Board
    /// (pour qu'il penche avec la direction).
    /// </summary>
    public static void AttachToBoard(Skateboard board)
    {
        var log = Core.Instance.LoggerInstance;
        if (board == null)
            return;

        var prefab = ScooterAssets.Prefab;
        if (prefab == null)
        {
            log.Warning("Scooter prefab unavailable, keeping the skateboard visuals.");
            return;
        }

        var visuals = board.GetComponent<SkateboardVisuals>();
        var animation = board.GetComponent<SkateboardAnimation>();
        Transform boardRoot = (visuals != null && visuals.Board != null) ? visuals.Board : board.transform;

        // Masquer le visuel d'origine AVANT d'ajouter le nôtre
        HideOriginalRenderers(board.gameObject, "board");

        var root = UnityEngine.Object.Instantiate(prefab, boardRoot);
        root.name = "ElectricScooter_Visual";
        root.SetActive(true);
        StripColliders(root);
        SetLayerRecursively(root, board.gameObject.layer);

        var rig = new ScooterRig
        {
            Root = root,
            HandL = root.transform.Find("Sockets/Hand_L"),
            HandR = root.transform.Find("Sockets/Hand_R"),
            FootPos = root.transform.Find("Sockets/Foot_Pos"),
            BoardRoot = boardRoot,
            AnimationId = animation != null ? animation.GetInstanceID() : 0,
            BoardId = board.GetInstanceID()
        };

        if (rig.HandL == null) log.Warning("Socket 'Sockets/Hand_L' not found in the scooter prefab.");
        if (rig.HandR == null) log.Warning("Socket 'Sockets/Hand_R' not found in the scooter prefab.");
        if (rig.FootPos == null) log.Warning("Socket 'Sockets/Foot_Pos' not found in the scooter prefab.");
        if (animation == null) log.Warning("SkateboardAnimation not found on the board: IK will not be applied.");

        ScooterMotor.Reset();
        Active = rig;
        ApplyTransform(rig);

        log.Msg($"Scooter visual attached (board hover height: {board.CurentSettings.HoverHeight:0.###}).");
    }

    public static void Clear()
    {
        RestoreElbows(Active);
        Active = null;
        ScooterMotor.Reset();
    }

    private static void ApplyTransform(ScooterRig rig)
    {
        var t = rig.Root.transform;
        t.localPosition = ScooterConfig.Offset;
        t.localRotation = Quaternion.Euler(0f, ScooterConfig.Yaw.Value, 0f);
        t.localScale = Vector3.one * ScooterConfig.Scale.Value;
    }

    // ---------- Corps / IK ----------

    private static bool IsActiveFor(SkateboardAnimation animation, out ScooterRig rig)
    {
        rig = Active;
        return rig != null && rig.Root != null && animation.GetInstanceID() == rig.AnimationId;
    }

    /// <summary>
    /// Appelé AVANT SkateboardAnimation.LateUpdate. L'animation de poussée du skate fait avancer le bassin
    /// et la colonne : on les remet à leur pose par défaut pour que le torse reste fixe (seuls les pieds bougent).
    /// </summary>
    public static void LockBody(SkateboardAnimation animation)
    {
        if (!IsActiveFor(animation, out _))
            return;

        ScooterMotor.UpdateFreezeBlend(Time.deltaTime);
        float blend = ScooterMotor.FreezeBlend;

        if (ScooterConfig.FreezeBodyDuringPush.Value || blend > 0f)
        {
            ResetToDefault(animation.PelvisContainerAlignment);
            ResetToDefault(animation.PelvisAlignment);
            ResetToDefault(animation.SpineContainerAlignment);
            ResetToDefault(animation.SpineAlignment);
        }

        // Moteur engagé : pieds et genoux reviennent en douceur vers leur pose par défaut (aucune animation)
        if (blend > 0f)
        {
            BlendToDefault(animation.LeftFootAlignment, blend);
            BlendToDefault(animation.RightFootAlignment, blend);
            BlendToDefault(animation.LeftLegBendTarget, blend);
            BlendToDefault(animation.RightLegBendTarget, blend);
        }
    }

    private static void BlendToDefault(SkateboardAnimation.AlignmentSet set, float blend)
    {
        if (set == null || set.Transform == null || set.Default == null)
            return;

        set.Transform.localPosition = Vector3.Lerp(set.Transform.localPosition, set.Default.localPosition, blend);
        set.Transform.localRotation = Quaternion.Slerp(set.Transform.localRotation, set.Default.localRotation, blend);
    }

    private static void ResetToDefault(SkateboardAnimation.AlignmentSet set)
    {
        if (set == null || set.Transform == null || set.Default == null)
            return;

        set.Transform.localPosition = set.Default.localPosition;
        set.Transform.localRotation = set.Default.localRotation;
    }

    /// <summary>
    /// Appelé APRÈS SkateboardAnimation.LateUpdate. Toutes les cibles manipulées ici sont réinitialisées dans
    /// Update (ou recalculées dans LateUpdate) : rien ne s'accumule d'une frame à l'autre.
    ///
    /// - Bassin et colonne : décalage (BodyOffset) + rotation TorsoYaw pour faire face à l'avant
    ///   (la posture du skate est de côté).
    /// - Pieds : pose d'origine décalée de Foot_Pos et FootOffsetY, tournés de FootYaw.
    /// - Genoux (cibles de flexion) : gardent leur position relative au pied, tournée de FootYaw.
    /// - Mains : posées sur Hand_L / Hand_R.
    /// </summary>
    public static void ApplyIK(SkateboardAnimation animation)
    {
        if (!IsActiveFor(animation, out var rig))
            return;

        var boardTransform = animation.transform;
        Vector3 up = boardTransform.up;

        Vector3 shift = up * ScooterConfig.BodyOffsetY.Value
                      + boardTransform.forward * ScooterConfig.BodyOffsetZ.Value;
        Quaternion torsoRot = Quaternion.AngleAxis(ScooterConfig.TorsoYaw.Value, up);
        Quaternion footRot = Quaternion.AngleAxis(ScooterConfig.FootYaw.Value, up);

        var pelvis = animation.PelvisAlignment.Transform;
        var spine = animation.SpineAlignment.Transform;
        var leftKnee = animation.LeftLegBendTarget.Transform;
        var rightKnee = animation.RightLegBendTarget.Transform;
        var leftFoot = animation.LeftFootAlignment.Transform;
        var rightFoot = animation.RightFootAlignment.Transform;

        // Poses calculées par le jeu, avant nos modifications
        Vector3 pelvisPos = pelvis.position;
        Quaternion pelvisRot = pelvis.rotation;
        Vector3 spinePos = spine.position;
        Quaternion spineRot = spine.rotation;
        Vector3 leftKneePos = leftKnee.position;
        Vector3 rightKneePos = rightKnee.position;
        Vector3 leftFootPos = leftFoot.position;
        Quaternion leftFootRot = leftFoot.rotation;
        Vector3 rightFootPos = rightFoot.position;
        Quaternion rightFootRot = rightFoot.rotation;

        // --- Pieds ---
        Vector3 footDelta = up * ScooterConfig.FootOffsetY.Value;
        if (rig.FootPos != null && rig.BoardRoot != null)
            footDelta += rig.FootPos.position - rig.BoardRoot.position;

        Vector3 leftFootNew = leftFootPos + footDelta;
        Vector3 rightFootNew = rightFootPos + footDelta;
        leftFoot.SetPositionAndRotation(leftFootNew, footRot * leftFootRot);
        rightFoot.SetPositionAndRotation(rightFootNew, footRot * rightFootRot);

        // --- Bassin et colonne (le bassin peut être le parent de la colonne : on repositionne la colonne en absolu) ---
        pelvis.SetPositionAndRotation(pelvisPos + shift, torsoRot * pelvisRot);
        spine.SetPositionAndRotation(pelvisPos + torsoRot * (spinePos - pelvisPos) + shift, torsoRot * spineRot);

        // --- Genoux : position relative au pied conservée, tournée comme le pied ---
        leftKnee.position = leftFootNew + footRot * (leftKneePos - leftFootPos) + shift;
        rightKnee.position = rightFootNew + footRot * (rightKneePos - rightFootPos) + shift;

        // --- Mains ---
        var handRotation = ScooterConfig.HandRotation;
        if (rig.HandL != null)
            animation.LeftHandAlignment.Transform.SetPositionAndRotation(
                rig.HandL.position + BoardOffset(boardTransform, ScooterConfig.LeftHandOffset), rig.HandL.rotation * handRotation);
        if (rig.HandR != null)
            animation.RightHandAlignment.Transform.SetPositionAndRotation(
                rig.HandR.position + BoardOffset(boardTransform, ScooterConfig.RightHandOffset), rig.HandR.rotation * handRotation);

        // --- Coudes ---
        ApplyElbows(animation, rig, boardTransform);
    }

    /// <summary>
    /// Place les cibles de coude (position de la main + ElbowOffset dans les axes du board) et les branche sur les
    /// solveurs de bras du BipedIK (bendModifier = Goal). L'état d'origine est sauvegardé pour la descente.
    /// </summary>
    private static void ApplyElbows(SkateboardAnimation animation, ScooterRig rig, Transform boardTransform)
    {
        if (!ScooterConfig.ElbowsEnabled.Value)
        {
            RestoreElbows(rig);
            return;
        }

        if (rig.BodyIK == null)
            rig.BodyIK = animation.GetComponentInChildren<BipedIK>(true);
        if (rig.BodyIK == null)
            return;

        if (rig.ElbowL == null) rig.ElbowL = new GameObject("ElectricScooter_ElbowL").transform;
        if (rig.ElbowR == null) rig.ElbowR = new GameObject("ElectricScooter_ElbowR").transform;
        rig.ElbowL.SetParent(boardTransform, false);
        rig.ElbowR.SetParent(boardTransform, false);

        var left = rig.BodyIK.solvers.leftHand;
        var right = rig.BodyIK.solvers.rightHand;

        if (!rig.ElbowsOverridden)
        {
            rig.OrigModL = left.bendModifier; rig.OrigGoalL = left.bendGoal; rig.OrigWeightL = left.bendModifierWeight;
            rig.OrigModR = right.bendModifier; rig.OrigGoalR = right.bendGoal; rig.OrigWeightR = right.bendModifierWeight;
            rig.ElbowsOverridden = true;
        }

        Vector3 handL = animation.LeftHandAlignment.Transform.position;
        Vector3 handR = animation.RightHandAlignment.Transform.position;
        rig.ElbowL.position = handL + BoardOffset(boardTransform, ScooterConfig.LeftElbowOffset);
        rig.ElbowR.position = handR + BoardOffset(boardTransform, ScooterConfig.RightElbowOffset);

        left.bendModifier = IKSolverLimb.BendModifier.Goal;
        left.bendGoal = rig.ElbowL;
        left.bendModifierWeight = ScooterConfig.ElbowWeight.Value;
        right.bendModifier = IKSolverLimb.BendModifier.Goal;
        right.bendGoal = rig.ElbowR;
        right.bendModifierWeight = ScooterConfig.ElbowWeight.Value;
    }

    private static void RestoreElbows(ScooterRig rig)
    {
        if (rig == null || !rig.ElbowsOverridden || rig.BodyIK == null)
            return;

        var left = rig.BodyIK.solvers.leftHand;
        var right = rig.BodyIK.solvers.rightHand;
        left.bendModifier = rig.OrigModL; left.bendGoal = rig.OrigGoalL; left.bendModifierWeight = rig.OrigWeightL;
        right.bendModifier = rig.OrigModR; right.bendGoal = rig.OrigGoalR; right.bendModifierWeight = rig.OrigWeightR;
        rig.ElbowsOverridden = false;
    }

    /// <summary>Convertit un offset (X droite, Y haut, Z avant) dans les axes du board en vecteur monde.</summary>
    private static Vector3 BoardOffset(Transform board, Vector3 o)
    {
        return board.right * o.x + board.up * o.y + board.forward * o.z;
    }

    // ---------- Mode "tenue en main" (viewmodel) ----------

    public static void AttachToHeld(Skateboard_Equippable equippable)
    {
        var log = Core.Instance.LoggerInstance;
        var container = equippable != null ? equippable.ModelContainer : null;
        if (container == null)
            return;

        var prefab = ScooterAssets.Prefab;
        if (prefab == null)
            return;

        HideOriginalRenderers(container.gameObject, "held model");

        var root = UnityEngine.Object.Instantiate(prefab, container);
        root.name = "ElectricScooter_Held";
        root.SetActive(true);
        StripColliders(root);

        var t = root.transform;
        t.localPosition = ScooterConfig.HeldOffset;
        t.localRotation = Quaternion.Euler(0f, ScooterConfig.HeldYaw.Value, 0f);
        t.localScale = Vector3.one * ScooterConfig.HeldScale.Value;

        // Même traitement que Equippable_Viewmodel.Equip : couche Viewmodel, pas d'ombres
        SetLayerRecursively(root, LayerMask.NameToLayer("Viewmodel"));
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            renderer.shadowCastingMode = ShadowCastingMode.Off;

        log.Msg("Scooter held model attached.");
    }

    // ---------- Utilitaires ----------

    /// <summary>
    /// Masque tous les renderers d'origine (sauf personnage, particules et traînées). On utilise
    /// forceRenderingOff en plus de enabled=false, car un LODGroup ou un script peut réactiver un renderer.
    /// </summary>
    private static void HideOriginalRenderers(GameObject root, string label)
    {
        var log = Core.Instance.LoggerInstance;

        foreach (var lodGroup in root.GetComponentsInChildren<LODGroup>(true))
        {
            // On ne touche pas aux LODGroup qui contiennent un personnage (SkinnedMeshRenderer)
            if (lodGroup.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0)
                lodGroup.enabled = false;
        }

        int count = 0;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            // Le personnage du joueur est animé sous la hiérarchie du board : ne jamais le masquer
            if (renderer.TryCast<SkinnedMeshRenderer>() != null)
            {
                log.Msg($"Kept skinned renderer ({label}): '{GetPath(renderer.transform, root.transform)}'");
                continue;
            }

            if (renderer.TryCast<ParticleSystemRenderer>() != null
                || renderer.TryCast<TrailRenderer>() != null
                || renderer.TryCast<LineRenderer>() != null)
                continue;

            renderer.enabled = false;
            renderer.forceRenderingOff = true;
            count++;
            log.Msg($"Hidden original renderer ({label}): '{GetPath(renderer.transform, root.transform)}'");
        }

        if (count == 0)
            log.Warning($"No original renderer found to hide ({label}).");
    }

    private static string GetPath(Transform t, Transform root)
    {
        string path = t.name;
        while (t.parent != null && t.parent != root)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    private static void StripColliders(GameObject go)
    {
        foreach (var collider in go.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.Destroy(collider);
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        var t = go.transform;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i).gameObject, layer);
    }
}
