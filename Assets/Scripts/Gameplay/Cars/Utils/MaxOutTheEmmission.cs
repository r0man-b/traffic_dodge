using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MaxOutTheEmmission : MonoBehaviour
{
    public Material maxThisOut;
    public bool isAnimated = false;
    public float desiredIntensity = 1.0f; // New public field for desired intensity

    private Color baseEmissionColor;
    private Color lastEmissionColor;
    private Vector2 currentOffset;
    private int currentSceneBuildIndex;
    private Renderer targetRenderer;
    private Material sourceMaterial;
    private bool autoAnimateEmissionMap;
    public float yOffsetSpeed = 0.1f;

    void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        currentSceneBuildIndex = currentScene.buildIndex;

        if (currentSceneBuildIndex != 0)
        {
            desiredIntensity /= 2;
        }

        if (maxThisOut == null)
        {
            Debug.LogError("No maxThisOut assigned!");
            return;
        }

        targetRenderer = GetComponent<Renderer>();
        sourceMaterial = maxThisOut;
        if (!BindRuntimeMaterialToRenderer())
            return;

        // Use the new desiredIntensity field instead of intensityMultiplier.
        // Normalize first because reused menu cars can already hold a previously maxed-out
        // runtime material by the time this component binds to it.
        baseEmissionColor = NormalizeEmissionBaseColor(maxThisOut.GetColor("_EmissionColor"), desiredIntensity);
        SetEmissionIntensity(baseEmissionColor, desiredIntensity);
        currentOffset = GetAnimatedTextureOffset(maxThisOut);
        //Debug.Log("The current emission intensity of " + maxThisOut.name + " is " + maxThisOut.GetColor("_EmissionColor"));
    }

    public void RefreshAfterRuntimeMaterialSwap()
    {
        if (maxThisOut == null)
            return;

        targetRenderer = GetComponent<Renderer>();

        // Menu cars are pre-placed and reused. Every time a car is displayed, Car clones
        // renderer materials again to keep project assets clean. Start() only runs once
        // for this component, so explicitly re-find the latest renderer material after
        // that swap instead of continuing to animate a stale material from the last visit.
        Material rendererMaterial = FindRendererMaterial(targetRenderer, maxThisOut);
        if (rendererMaterial == null)
            return;

        sourceMaterial = rendererMaterial;
        if (!BindRuntimeMaterialToRenderer())
            return;

        baseEmissionColor = NormalizeEmissionBaseColor(maxThisOut.GetColor("_EmissionColor"), desiredIntensity);
        SetEmissionIntensity(baseEmissionColor, desiredIntensity);
    }

    void Update()
    {
        // The garage/main-menu car setup can replace renderer.sharedMaterials after this component
        // has already run Start(). If that happens, maxThisOut is still changing, but it is no
        // longer the material the renderer draws, so scrolling/pulsing emissions look frozen.
        // Re-check the binding before every animation tick and make a fresh DontSave clone if a
        // later runtime material pass swapped the renderer out from under us.
        // Use Update plus unscaled time because garage/menu states can pause or throttle physics
        // updates while the car should still look alive on the turntable.
        if (!EnsureRuntimeMaterialStillBound())
            return;

        if (isAnimated || autoAnimateEmissionMap)
        {
            currentOffset.y += yOffsetSpeed * Time.unscaledDeltaTime;
            SetAnimatedTextureOffset(maxThisOut, currentOffset);
        }

        Color currentEmissionColor = maxThisOut.GetColor("_EmissionColor");

        // Extract RGB components of both colors.
        float currentR = currentEmissionColor.r;
        float currentG = currentEmissionColor.g;
        float currentB = currentEmissionColor.b;

        float lastR = lastEmissionColor.r;
        float lastG = lastEmissionColor.g;
        float lastB = lastEmissionColor.b;

        // Compare RGB components separately.
        if (currentR != lastR || currentG != lastG || currentB != lastB)
        {
            // If a garage color/apply pass writes a fresh color into the material, max it out.
            // If the material came from another runtime clone that was already maxed, divide
            // that previous intensity back out first so car switching does not compound bloom.
            baseEmissionColor = NormalizeEmissionBaseColor(currentEmissionColor, desiredIntensity);
            SetEmissionIntensity(baseEmissionColor, desiredIntensity);
        }
    }

    private bool EnsureRuntimeMaterialStillBound()
    {
        if (targetRenderer == null || maxThisOut == null)
            return false;

        foreach (Material material in targetRenderer.sharedMaterials)
        {
            if (material == maxThisOut)
                return true;
        }

        return BindRuntimeMaterialToRenderer();
    }

    private bool BindRuntimeMaterialToRenderer()
    {
        Material rendererMaterial = FindRendererMaterial(targetRenderer, sourceMaterial);
        if (rendererMaterial == null)
            return false;

        // This script animates emission intensity and optional texture offset every Play Mode run.
        // Clone the renderer's current material before editing it so the source .mat asset stays unchanged.
        // The renderer may already have a runtime car material assigned, so do not assume the serialized
        // maxThisOut asset is still present on the renderer by reference.
        Material runtimeMaterial = new Material(rendererMaterial)
        {
            name = StripRuntimeSuffix(rendererMaterial.name) + " (MaxOut Runtime)",
            hideFlags = HideFlags.DontSave
        };

        if (targetRenderer != null)
        {
            // Rebind only the slots that used the matched source material. Other renderer
            // materials are left alone, and all future writes go to maxThisOut's runtime clone.
            Material[] materials = targetRenderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == rendererMaterial)
                {
                    materials[i] = runtimeMaterial;
                }
            }
            targetRenderer.sharedMaterials = materials;
        }

        maxThisOut = runtimeMaterial;
        autoAnimateEmissionMap = ShouldAutoAnimateEmissionMap(maxThisOut);
        currentOffset = GetAnimatedTextureOffset(maxThisOut);
        return true;
    }

    private static Material FindRendererMaterial(Renderer renderer, Material fallback)
    {
        if (renderer == null || fallback == null) return fallback;

        Material[] materials = renderer.sharedMaterials;
        foreach (Material material in materials)
        {
            if (material == fallback) return material;
        }

        string fallbackName = StripRuntimeSuffix(fallback.name);
        foreach (Material material in materials)
        {
            if (material != null && StripRuntimeSuffix(material.name) == fallbackName)
            {
                return material;
            }
        }

        if (materials.Length == 1 && materials[0] != null)
        {
            return materials[0];
        }

        return fallback;
    }

    private static string StripRuntimeSuffix(string materialName)
    {
        const string runtimeSuffix = " (Runtime)";
        const string maxOutSuffix = " (MaxOut Runtime)";
        const string instanceSuffix = " (Instance)";
        bool removedSuffix;

        // Runtime material protection can pass the same pre-placed menu car through several
        // display cycles, producing names like "LIGHT (MaxOut Runtime) (Runtime)".
        // Strip repeatedly so material matching still resolves back to the original asset name.
        do
        {
            removedSuffix = false;

            if (materialName.EndsWith(runtimeSuffix))
            {
                materialName = materialName.Substring(0, materialName.Length - runtimeSuffix.Length);
                removedSuffix = true;
            }

            if (materialName.EndsWith(maxOutSuffix))
            {
                materialName = materialName.Substring(0, materialName.Length - maxOutSuffix.Length);
                removedSuffix = true;
            }

            if (materialName.EndsWith(instanceSuffix))
            {
                materialName = materialName.Substring(0, materialName.Length - instanceSuffix.Length);
                removedSuffix = true;
            }
        }
        while (removedSuffix);

        return materialName;
    }

    private void SetEmissionIntensity(Color color, float intensity)
    {
        Color newEmissionColor = color * intensity; // Use intensity directly.
        maxThisOut.SetColor("_EmissionColor", newEmissionColor);
        lastEmissionColor = newEmissionColor;

        maxThisOut.EnableKeyword("_EMISSION");

        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            DynamicGI.SetEmissive(renderer, newEmissionColor);
        }
    }

    private static Color NormalizeEmissionBaseColor(Color color, float intensity)
    {
        if (intensity <= 1f)
            return color;

        // Runtime menu cars are reused, so cloned materials can already contain a color that
        // this script multiplied during a previous display cycle. If we use that boosted color
        // as the new base, each car switch multiplies brightness again. Repeatedly divide by
        // the configured intensity until the RGB values look like an unboosted HDR color.
        float maxChannel = Mathf.Max(color.r, color.g, color.b);
        while (maxChannel > intensity)
        {
            color.r /= intensity;
            color.g /= intensity;
            color.b /= intensity;
            color.a /= intensity;
            maxChannel = Mathf.Max(color.r, color.g, color.b);
        }

        return color;
    }

    private static Vector2 GetAnimatedTextureOffset(Material material)
    {
        if (material == null) return Vector2.zero;

        // Many car light materials keep the visible scrolling pattern in the emission map,
        // not in the base color map. Prefer the first texture slot that actually has a
        // texture so we animate the pixels the shader is displaying.
        if (HasTexture(material, "_EmissionMap"))
            return material.GetTextureOffset("_EmissionMap");

        if (material.HasProperty("_BaseMap"))
            return material.GetTextureOffset("_BaseMap");

        if (material.HasProperty("_MainTex"))
            return material.GetTextureOffset("_MainTex");

        return Vector2.zero;
    }

    private static void SetAnimatedTextureOffset(Material material, Vector2 offset)
    {
        if (material == null) return;

        // Keep emission-map lights moving. Updating the empty base/main texture slots alone
        // does not visibly affect URP Lit materials whose animated detail lives in _EmissionMap.
        // We still write the empty base/main slots when present so the Inspector clearly shows
        // that this runtime material is being driven, regardless of which foldout is open.
        if (material.HasProperty("_EmissionMap"))
            material.SetTextureOffset("_EmissionMap", offset);

        if (material.HasProperty("_BaseMap"))
            material.SetTextureOffset("_BaseMap", offset);

        if (material.HasProperty("_MainTex"))
            material.SetTextureOffset("_MainTex", offset);
    }

    private static bool HasTexture(Material material, string propertyName)
    {
        return material.HasProperty(propertyName) && material.GetTexture(propertyName) != null;
    }

    private static bool ShouldAutoAnimateEmissionMap(Material material)
    {
        if (!HasTexture(material, "_EmissionMap"))
            return false;

        string materialName = StripRuntimeSuffix(material.name);
        return materialName.Contains("LIGHT");
    }
}
