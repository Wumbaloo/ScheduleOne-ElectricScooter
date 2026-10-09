using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace ElectricScooterMod;

/// <summary>
/// Charge le AssetBundle (embarqué dans la DLL sous le nom "scooter_bundle") et expose le prefab.
/// </summary>
public static class ScooterAssets
{
    public const string BundleResourceName = "scooter_bundle";
    public const string PrefabName = "ScooterPrefab";
    private const string UrpLitName = "Universal Render Pipeline/Lit";

    private static AssetBundle _bundle;
    private static GameObject _prefab;
    private static bool _loadAttempted;

    public static GameObject Prefab
    {
        get
        {
            if (_prefab == null && !_loadAttempted)
                Load();
            return _prefab;
        }
    }

    private static void Load()
    {
        _loadAttempted = true;
        var log = Core.Instance.LoggerInstance;

        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream(BundleResourceName);
            if (stream == null)
            {
                log.Error($"Embedded bundle '{BundleResourceName}' not found. Embedded resources: [{string.Join(", ", asm.GetManifestResourceNames())}]. Did you build the AssetBundle in Unity (Assets > Build AssetBundles) before building the mod?");
                return;
            }

            var bytes = new byte[stream.Length];
            int read = 0;
            while (read < bytes.Length)
            {
                int n = stream.Read(bytes, read, bytes.Length - read);
                if (n <= 0) break;
                read += n;
            }

            _bundle = AssetBundle.LoadFromMemory(new Il2CppStructArray<byte>(bytes));
            if (_bundle == null)
            {
                log.Error("AssetBundle.LoadFromMemory returned null (bundle built with another Unity version?).");
                return;
            }
            _bundle.hideFlags = HideFlags.DontUnloadUnusedAsset;

            _prefab = _bundle.LoadAsset<GameObject>(PrefabName);
            if (_prefab == null)
            {
                var names = new List<string>();
                foreach (var n in _bundle.GetAllAssetNames())
                    names.Add(n);
                log.Error($"Prefab '{PrefabName}' not found in bundle. Assets in bundle: [{string.Join(", ", names)}]");
                return;
            }

            _prefab.hideFlags = HideFlags.DontUnloadUnusedAsset;
            FixMaterials(_prefab);
            log.Msg("Scooter prefab loaded from AssetBundle.");
        }
        catch (Exception ex)
        {
            log.Error($"Failed to load scooter bundle: {ex}");
        }
    }

    /// <summary>
    /// Les matériaux du bundle utilisent un shader léger (Unlit) pour que le build du bundle reste rapide.
    /// Au chargement on les bascule sur le shader URP/Lit du jeu (éclairage normal) en reprenant
    /// texture, normal map et couleur.
    /// </summary>
    private static void FixMaterials(GameObject prefab)
    {
        var log = Core.Instance.LoggerInstance;
        var urpLit = Shader.Find(UrpLitName);
        if (urpLit == null)
        {
            log.Warning($"Shader '{UrpLitName}' not found in the game, keeping the bundle shaders.");
            return;
        }

        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var mat = materials[i];
                if (mat == null)
                    continue;

                string shaderName = mat.shader != null ? mat.shader.name : "<null>";
                if (shaderName == UrpLitName)
                    continue;

                Texture baseMap = GetTexture(mat, "_BaseMap");
                if (baseMap == null)
                    baseMap = GetTexture(mat, "_MainTex");
                Texture normalMap = GetTexture(mat, "_BumpMap");

                Color color = Color.white;
                if (mat.HasProperty("_BaseColor"))
                    color = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color"))
                    color = mat.GetColor("_Color");

                mat.shader = urpLit;
                if (baseMap != null)
                    mat.SetTexture("_BaseMap", baseMap);
                mat.SetColor("_BaseColor", color);
                if (normalMap != null)
                {
                    mat.SetTexture("_BumpMap", normalMap);
                    mat.EnableKeyword("_NORMALMAP");
                }

                log.Msg($"Scooter material '{mat.name}': '{shaderName}' -> '{UrpLitName}' (texture: {(baseMap != null ? baseMap.name : "none")}).");
            }
        }
    }

    private static Texture GetTexture(Material mat, string property)
    {
        return mat.HasProperty(property) ? mat.GetTexture(property) : null;
    }
}
