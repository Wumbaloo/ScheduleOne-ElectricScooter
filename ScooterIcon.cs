using UnityEngine;

namespace ElectricScooterMod;

/// <summary>
/// Génère l'icône d'inventaire de la trottinette en rendant le prefab dans une texture
/// (caméra temporaire, fond transparent, lumière temporaire), puis en créant un Sprite.
/// </summary>
public static class ScooterIcon
{
    private const int Size = 256;

    /// <summary>Retourne le sprite, ou null en cas d'échec (l'icône du skate cloné est alors conservée).</summary>
    public static Sprite Create(GameObject prefab)
    {
        var log = Core.Instance.LoggerInstance;
        if (prefab == null)
            return null;

        GameObject model = null;
        GameObject cameraGo = null;
        GameObject lightGo = null;
        Camera camera = null;
        RenderTexture renderTexture = null;

        try
        {
            // Hors de la carte, pour ne rien voir d'autre dans le cadre
            Vector3 origin = new Vector3(5000f, 5000f, 5000f);

            model = UnityEngine.Object.Instantiate(prefab);
            model.name = "ElectricScooter_IconModel";
            model.transform.position = origin;
            // Vue 3/4 : le devant du modèle (-Z) tourné vers la caméra et légèrement de côté
            model.transform.rotation = Quaternion.Euler(0f, -35f, 0f);

            // Cadrage à partir des bounds de tous les renderers
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                log.Warning("Icon: no renderer in the scooter prefab.");
                return null;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            lightGo = new GameObject("ElectricScooter_IconLight");
            lightGo.transform.rotation = Quaternion.Euler(40f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.shadows = LightShadows.None;

            cameraGo = new GameObject("ElectricScooter_IconCamera");
            camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 50f;
            camera.cullingMask = ~0;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            cameraGo.transform.position = bounds.center + new Vector3(0f, 0f, -20f);
            cameraGo.transform.rotation = Quaternion.identity;

            renderTexture = RenderTexture.GetTemporary(Size, Size, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;
            camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, Size, Size), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;

            texture.name = "ElectricScooter_Icon";
            texture.hideFlags = HideFlags.DontUnloadUnusedAsset;

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "ElectricScooter_Icon";
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;

            log.Msg("Scooter icon generated.");
            return sprite;
        }
        catch (Exception ex)
        {
            log.Warning($"Could not generate the scooter icon, keeping the skateboard icon: {ex.Message}");
            return null;
        }
        finally
        {
            // La caméra ne doit plus rendre vers la texture libérée (Destroy est différé à la fin de la frame)
            if (camera != null)
            {
                camera.enabled = false;
                camera.targetTexture = null;
            }
            if (renderTexture != null)
            {
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
            if (cameraGo != null) UnityEngine.Object.Destroy(cameraGo);
            if (lightGo != null) UnityEngine.Object.Destroy(lightGo);
            if (model != null) UnityEngine.Object.Destroy(model);
        }
    }
}
