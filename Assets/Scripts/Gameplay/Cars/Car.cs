using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Car", menuName = "Car Scriptable Object")]
public class Car : ScriptableObject
{
    private Color[] colorPresets = new Color[]
    {
    // --- Neutral Grays & Whites (brightened) ---
    new Color(0.7f, 0.7f, 0.7f),   // medium-light gray
    new Color(0.85f, 0.85f, 0.85f), // light silver gray
    new Color(0.95f, 0.95f, 0.95f), // bright white
    new Color(0.9f, 0.9f, 0.92f),   // cool white
    new Color(0.88f, 0.88f, 0.88f), // pearl gray-white

    // --- Blues (brightened but still car-like) ---
    new Color(0.35f, 0.55f, 0.9f),  // bright steel blue
    new Color(0.25f, 0.45f, 0.75f), // lighter navy
    new Color(0.45f, 0.65f, 0.9f),  // sky blue
    new Color(0.55f, 0.7f, 0.95f),  // light azure

    // --- Greens ---
    new Color(0.3f, 0.6f, 0.3f),    // brightened forest green
    new Color(0.4f, 0.7f, 0.5f),    // jade
    new Color(0.3f, 0.85f, 0.3f),   // light mint green

    // --- Reds ---
    new Color(0.75f, 0.2f, 0.2f),   // crimson
    new Color(0.85f, 0.25f, 0.25f), // bright red

    // --- Yellows & Golds ---
    new Color(0.95f, 0.85f, 0.4f),  // warm golden yellow
    new Color(1.0f, 0.9f, 0.5f),    // sandy yellow
    new Color(1.0f, 0.95f, 0.6f),   // bright pastel yellow

    // --- Browns / Beiges (brightened) ---
    new Color(0.75f, 0.65f, 0.5f),  // light brown
    new Color(0.85f, 0.75f, 0.6f),  // beige
    new Color(0.9f, 0.8f, 0.65f),   // sand beige

    // --- Abrasive / Bright Colors (rarities) ---
    new Color(0.0f, 0.8f, 0.0f),      // neon lime green
    new Color(1f, 0.0f, 1f),        // hot pink / magenta
    new Color(1f, 0.5f, 0.0f),      // neon orange
    new Color(1f, 1f, 0.0f),        // neon yellow
    new Color(0.2f, 1f, 1f),        // neon cyan
    new Color(0.5f, 0.0f, 1f),      // electric purple
    new Color(1f, 0.3f, 0.0f),      // safety orange
    new Color(0.0f, 0.9f, 0.9f),    // teal neon
    new Color(0.8f, 1f, 0.0f),      // acid green
    new Color(0.9f, 0.0f, 0.0f),    // firetruck red
    };

    private Color[] fresnelPresets = new Color[]
    {
        Color.black,
        Color.gray,
        Color.white,
        Color.blue,
        Color.cyan,
        Color.green,
        Color.magenta,
        Color.red,
        Color.yellow
    };

    // Primary shades that trigger fresnel palette rims if picked in the 10% bucket
    private static readonly Color[] k_PrimaryRimFresnelTriggerShades = new Color[]
    {
        new Color(0.7f, 0.7f, 0.7f),   // medium-light gray
        new Color(0.85f, 0.85f, 0.85f),// light silver gray
        new Color(0.95f, 0.95f, 0.95f),// bright white
        new Color(0.9f, 0.9f, 0.92f),  // cool white
        new Color(0.88f, 0.88f, 0.88f) // pearl gray-white
    };

    private static bool Approximately(Color a, Color b, float eps = 0.02f)
    {
        return Mathf.Abs(a.r - b.r) <= eps &&
               Mathf.Abs(a.g - b.g) <= eps &&
               Mathf.Abs(a.b - b.b) <= eps &&
               Mathf.Abs(a.a - b.a) <= eps;
    }

    private static bool IsInTriggerShades(Color c)
    {
        for (int i = 0; i < k_PrimaryRimFresnelTriggerShades.Length; i++)
        {
            if (Approximately(c, k_PrimaryRimFresnelTriggerShades[i]))
                return true;
        }
        return false;
    }

    public enum ColorType
    {
        PRIMARY_COLOR = 0,
        SECONDARY_COLOR = 1,
        RIM_COLOR = 2,
        PRIMARY_LIGHT = 3,
        SECONDARY_LIGHT = 4,
        TAIL_LIGHT = 5
    }
    private string currentCarType;
    private int currentCarIndex;
    private const float NON_METALLIC_DEFAULT = 0.304f; // nonMetallicVal
    private const float RIM_NON_METALLIC_DEFAULT = 0.001f; // nonMetallicVal for rims
    private const float METALLIC_DEFAULT = 1.0f;   // metallicVal
    private bool forceNeutralBodyColors = false;
    // URP Lit property IDs
    private static readonly int ID_Metallic = Shader.PropertyToID("_Metallic");
    private static readonly int ID_MetallicGlossMap = Shader.PropertyToID("_MetallicGlossMap");

    [Header("Soft Stats")]
    public string car_name;
    public int price;
    public string horsepower;
    public float hp;
    public string powerplant;

    [Header("Hard Stats")]
    public float accelIncreaseRate;
    public float accelMaxValue;
    public int numlives;
    public float defaultAccelIncreaseRate;
    public float defaultAccelMaxValue;
    public float defaultNumLives;

    [Header("Meshes")]
    public GameObject carModel;

    [Header("Positions")]
    public Vector3 turntablePositon;
    public Vector3 raceSpawnPosition;
    public Vector3 defaultCameraPosition;
    public float laneSplitHeight;

    [Header("Default Custom Parts")]
    public string DefaultExhaust;
    public string DefaultFrontWheels;
    public string DefaultRearSplitter;
    public string DefaultRearWheels;
    public string DefaultSpoiler;

    [Header("Default Colors")]
    public Color defaultPrimaryColor;
    public Color defaultSecondaryColor;
    public Color defaultRimColor;
    public Color defaultPrimaryLightColor;
    public Color defaultSecondaryLightColor;
    public Color defaultTailLightColor;
    public bool emmissiveDefaultSecondaryColor;

    [Header("Materials")]
    public Material primColor;
    public Material secondColor;
    public Material rimColor;
    public Material primLight;
    public Material secondLight;
    public Material tailLight;

    [Header("Liveries (Don't Change These)")]
    public Texture2D[] liveries;

    [Header("Shaders (Don't Change These)")]
    public Shader matteShader;
    public Shader glossShader;

    // Helper method to apply metallic map property to material
    private static void ApplyMetallicToMaterial(Material m, float metallic01)
    {
        if (!m) return;

        // Ensure the float slider controls metallic (no texture influencing it).
        m.SetTexture(ID_MetallicGlossMap, null);
        m.DisableKeyword("_METALLICSPECGLOSSMAP");

        m.SetFloat(ID_Metallic, Mathf.Clamp01(metallic01));
    }


    // Helper method to get the default color based on the color index.
    private Color GetDefaultColor(int colorIndex)
    {
        return colorIndex switch
        {
            (int) ColorType.PRIMARY_COLOR    => defaultPrimaryColor,
            (int) ColorType.SECONDARY_COLOR  => defaultSecondaryColor,
            (int) ColorType.RIM_COLOR        => defaultRimColor,
            (int) ColorType.PRIMARY_LIGHT    => defaultPrimaryLightColor,
            (int) ColorType.SECONDARY_LIGHT  => defaultSecondaryLightColor,
            (int) ColorType.TAIL_LIGHT       => defaultTailLightColor,
            _ => Color.black, // Fallback to black if index is out of range.
        };
    }

    public Color? LoadColorFromSaveData(SaveData.CarData carData, int colorIndex)
    {
        // Access the color data for the specified part.
        SaveData.ColorData colorData = carData.Colors[colorIndex];

        // If the base color data is missing, set and save default colors.
        if (colorData.BaseColor[0] == -1f)
        {
            // Get the default color based on the color index.
            Color defaultColor = GetDefaultColor(colorIndex);

            switch (colorIndex)
            {
                case (int)ColorType.PRIMARY_COLOR:
                    colorData.BaseColor[0] = defaultColor.r;
                    colorData.BaseColor[1] = defaultColor.g;
                    colorData.BaseColor[2] = defaultColor.b;
                    colorData.BaseColor[3] = defaultColor.a;

                    colorData.FresnelColor[0] = defaultColor.r;
                    colorData.FresnelColor[1] = defaultColor.g;
                    colorData.FresnelColor[2] = defaultColor.b;
                    colorData.FresnelColor[3] = defaultColor.a;

                    colorData.FresnelColor2[0] = defaultColor.r;
                    colorData.FresnelColor2[1] = defaultColor.g;
                    colorData.FresnelColor2[2] = defaultColor.b;
                    colorData.FresnelColor2[3] = defaultColor.a;
                    colorData.MetallicMap = NON_METALLIC_DEFAULT;
                    break;

                case (int)ColorType.SECONDARY_COLOR:
                    if (emmissiveDefaultSecondaryColor)
                    {
                        colorData.BaseColor[0] = Color.black.r;
                        colorData.BaseColor[1] = Color.black.g;
                        colorData.BaseColor[2] = Color.black.b;
                        colorData.BaseColor[3] = Color.black.a;

                        colorData.FresnelColor[0] = Color.black.r;
                        colorData.FresnelColor[1] = Color.black.g;
                        colorData.FresnelColor[2] = Color.black.b;
                        colorData.FresnelColor[3] = Color.black.a;

                        colorData.FresnelColor2[0] = Color.black.r;
                        colorData.FresnelColor2[1] = Color.black.g;
                        colorData.FresnelColor2[2] = Color.black.b;
                        colorData.FresnelColor2[3] = Color.black.a;

                        colorData.EmissionColor[0] = defaultColor.r;
                        colorData.EmissionColor[1] = defaultColor.g;
                        colorData.EmissionColor[2] = defaultColor.b;
                        colorData.EmissionColor[3] = defaultColor.a;
                    }
                    else
                    {
                        colorData.BaseColor[0] = defaultColor.r;
                        colorData.BaseColor[1] = defaultColor.g;
                        colorData.BaseColor[2] = defaultColor.b;
                        colorData.BaseColor[3] = defaultColor.a;

                        colorData.FresnelColor[0] = defaultColor.r;
                        colorData.FresnelColor[1] = defaultColor.g;
                        colorData.FresnelColor[2] = defaultColor.b;
                        colorData.FresnelColor[3] = defaultColor.a;

                        colorData.FresnelColor2[0] = defaultColor.r;
                        colorData.FresnelColor2[1] = defaultColor.g;
                        colorData.FresnelColor2[2] = defaultColor.b;
                        colorData.FresnelColor2[3] = defaultColor.a;

                        colorData.EmissionColor[0] = Color.black.r;
                        colorData.EmissionColor[1] = Color.black.g;
                        colorData.EmissionColor[2] = Color.black.b;
                        colorData.EmissionColor[3] = Color.black.a;
                    }
                    colorData.MetallicMap = NON_METALLIC_DEFAULT;
                    break;

                case (int)ColorType.RIM_COLOR:
                    colorData.BaseColor[0] = defaultColor.r;
                    colorData.BaseColor[1] = defaultColor.g;
                    colorData.BaseColor[2] = defaultColor.b;
                    colorData.BaseColor[3] = defaultColor.a;

                    colorData.FresnelColor[0] = defaultColor.r;
                    colorData.FresnelColor[1] = defaultColor.g;
                    colorData.FresnelColor[2] = defaultColor.b;
                    colorData.FresnelColor[3] = defaultColor.a;

                    colorData.FresnelColor2[0] = defaultColor.r;
                    colorData.FresnelColor2[1] = defaultColor.g;
                    colorData.FresnelColor2[2] = defaultColor.b;
                    colorData.FresnelColor2[3] = defaultColor.a;

                    colorData.MetallicMap = RIM_NON_METALLIC_DEFAULT;
                    break;

                case (int)ColorType.PRIMARY_LIGHT:
                    colorData.BaseColor[0] = Color.black.r;
                    colorData.BaseColor[1] = Color.black.g;
                    colorData.BaseColor[2] = Color.black.b;
                    colorData.BaseColor[3] = Color.black.a;

                    colorData.EmissionColor[0] = defaultColor.r;
                    colorData.EmissionColor[1] = defaultColor.g;
                    colorData.EmissionColor[2] = defaultColor.b;
                    colorData.EmissionColor[3] = defaultColor.a;

                    colorData.MetallicMap = NON_METALLIC_DEFAULT;
                    break;

                case (int)ColorType.SECONDARY_LIGHT:
                    colorData.BaseColor[0] = Color.black.r;
                    colorData.BaseColor[1] = Color.black.g;
                    colorData.BaseColor[2] = Color.black.b;
                    colorData.BaseColor[3] = Color.black.a;

                    colorData.EmissionColor[0] = defaultColor.r;
                    colorData.EmissionColor[1] = defaultColor.g;
                    colorData.EmissionColor[2] = defaultColor.b;
                    colorData.EmissionColor[3] = defaultColor.a;

                    colorData.MetallicMap = NON_METALLIC_DEFAULT;
                    break;

                case (int)ColorType.TAIL_LIGHT:
                    colorData.BaseColor[0] = Color.black.r;
                    colorData.BaseColor[1] = Color.black.g;
                    colorData.BaseColor[2] = Color.black.b;
                    colorData.BaseColor[3] = Color.black.a;

                    colorData.EmissionColor[0] = defaultColor.r;
                    colorData.EmissionColor[1] = defaultColor.g;
                    colorData.EmissionColor[2] = defaultColor.b;
                    colorData.EmissionColor[3] = defaultColor.a;

                    colorData.MetallicMap = NON_METALLIC_DEFAULT;
                    break;

                default:
                    return Color.black; // Fallback to black if index is out of range.
            }

            // Save the default color in SaveData.
            SaveManager.Instance.SaveGame();
        }

        return colorIndex switch
        {
            (int)ColorType.PRIMARY_COLOR => new Color(colorData.BaseColor[0], colorData.BaseColor[1], colorData.BaseColor[2], colorData.BaseColor[3]),
            (int)ColorType.SECONDARY_COLOR => new Color(colorData.BaseColor[0], colorData.BaseColor[1], colorData.BaseColor[2], colorData.BaseColor[3]),
            (int)ColorType.RIM_COLOR => new Color(colorData.BaseColor[0], colorData.BaseColor[1], colorData.BaseColor[2], colorData.BaseColor[3]),
            (int)ColorType.PRIMARY_LIGHT => new Color(colorData.EmissionColor[0], colorData.EmissionColor[1], colorData.EmissionColor[2], colorData.EmissionColor[3]),
            (int)ColorType.SECONDARY_LIGHT => new Color(colorData.EmissionColor[0], colorData.EmissionColor[1], colorData.EmissionColor[2], colorData.EmissionColor[3]),
            (int)ColorType.TAIL_LIGHT => new Color(colorData.EmissionColor[0], colorData.EmissionColor[1], colorData.EmissionColor[2], colorData.EmissionColor[3]),
            _ => Color.black,// Fallback to black if index is out of range.
        };
    }

    public void ApplySavedColors(Material targetMaterial, ColorType colorType, bool isEmissiveMaterial, Color defaultColor)
    {
        // Access the current car's data.
        SaveData saveData = SaveManager.Instance.SaveData;
        if (!saveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
        {
            Debug.LogError($"Car data not found for CarType: {currentCarType}, CarIndex: {currentCarIndex}");
            return;
        }

        int colorIndex = (int)colorType;

        // Load the base color.
        Color? baseColor = LoadColorFromSaveData(carData, colorIndex) ?? defaultColor;

        if (isEmissiveMaterial) // Lighting materials only.
        {
            Color emissionColor = baseColor.Value * 3f; // 3x intensity
            targetMaterial.SetColor("_EmissionColor", emissionColor);
            targetMaterial.EnableKeyword("_EMISSION");
        }
        else
        {
            targetMaterial.color = baseColor.Value;
            targetMaterial.SetColor("_EmissionColor", Color.black);
            targetMaterial.DisableKeyword("_EMISSION");
        }

        // Handle Fresnel colors (if applicable).
        SaveData.ColorData colorData = carData.Colors[colorIndex];
        if (targetMaterial.HasProperty("_FresnelColor") && colorData.FresnelColor != null)
        {
            targetMaterial.SetColor("_FresnelColor", new Color(
                colorData.FresnelColor[0],
                colorData.FresnelColor[1],
                colorData.FresnelColor[2],
                colorData.FresnelColor[3]
            ));
        }
        if (targetMaterial.HasProperty("_FresnelColor2") && colorData.FresnelColor2 != null)
        {
            targetMaterial.SetColor("_FresnelColor2", new Color(
                colorData.FresnelColor2[0],
                colorData.FresnelColor2[1],
                colorData.FresnelColor2[2],
                colorData.FresnelColor2[3]
            ));
        }

        // Handle emissive rims & secondary colors
        if (colorType == ColorType.SECONDARY_COLOR || colorType == ColorType.RIM_COLOR)
        {
            if (!(colorData.EmissionColor[0] == 0f && colorData.EmissionColor[1] == 0f && colorData.EmissionColor[2] == 0f)
                && !(colorData.EmissionColor[0] == -1f && colorData.EmissionColor[1] == -1f && colorData.EmissionColor[2] == -1f && colorData.EmissionColor[3] == -1f))
            {
                targetMaterial.color = Color.black;
                Color emissionColor = new Color(colorData.EmissionColor[0], colorData.EmissionColor[1], colorData.EmissionColor[2], colorData.EmissionColor[3]) * 2f;
                targetMaterial.SetColor("_EmissionColor", emissionColor);
                targetMaterial.EnableKeyword("_EMISSION");
            }
        }

        // Apply metallic or non-metallic property
        float metallic = Mathf.Clamp01(colorData.MetallicMap);
        ApplyMetallicToMaterial(targetMaterial, metallic);
    }

    public void RandomizeColors(Material targetMaterial, ColorType colorType, bool isEmissiveMaterial)         // TODO: ADD SAVING LOGIC TO THIS FUNCTION
    {
        Color baseColor;
        Color fresnelColor;
        Color fresnelColor2;

        // Generate random base color and Fresnel colors.
        if (colorType == ColorType.RIM_COLOR)
        {
            // 30% black, 30% white, 30% gray, 10% match current primary
            float p = UnityEngine.Random.value;

            if (p < 0.3f)
            {
                baseColor = Color.black;
                fresnelColor = Color.black;
                fresnelColor2 = Color.black;
            }
            else if (p < 0.6f)
            {
                baseColor = Color.white;
                fresnelColor = Color.white;
                fresnelColor2 = Color.white;
            }
            else if (p < 0.9f)
            {
                baseColor = Color.gray;
                fresnelColor = Color.gray;
                fresnelColor2 = Color.gray;
            }
            else
            {
                Color primary = (primColor != null) ? primColor.color
                                                    : colorPresets[UnityEngine.Random.Range(0, colorPresets.Length)];

                if (IsInTriggerShades(primary))
                {
                    baseColor = fresnelPresets[UnityEngine.Random.Range(3, fresnelPresets.Length)];
                    fresnelColor = fresnelPresets[UnityEngine.Random.Range(3, fresnelPresets.Length)];
                    fresnelColor2 = fresnelPresets[UnityEngine.Random.Range(3, fresnelPresets.Length)];
                }
                else
                {
                    // If not in trigger set, just mirror primary as a sensible fallback.
                    baseColor = primary;
                    fresnelColor = primary;
                    fresnelColor2 = primary;
                }
            }
        }
        else
        {
            // If decals are applied, lock body colors (primary/secondary) to neutral white/gray.
            bool isBodyPaint = (colorType == ColorType.PRIMARY_COLOR || colorType == ColorType.SECONDARY_COLOR);

            if (forceNeutralBodyColors && isBodyPaint)
            {
                // Pick from your neutral trigger shades, and bypass any neon/fresnel overrides.
                Color c = k_PrimaryRimFresnelTriggerShades[UnityEngine.Random.Range(0, k_PrimaryRimFresnelTriggerShades.Length)];
                baseColor = c;
                fresnelColor = c;
                fresnelColor2 = c;
            }
            else
            {
                // Default: pull from main palette
                Color c = colorPresets[UnityEngine.Random.Range(0, colorPresets.Length)];
                baseColor = c;
                fresnelColor = c;
                fresnelColor2 = c;

                // 10% chance to override all three with a fresnelPresets color
                if (UnityEngine.Random.value < 0.10f)
                {
                    baseColor = fresnelPresets[UnityEngine.Random.Range(0, fresnelPresets.Length)];
                    fresnelColor = fresnelPresets[UnityEngine.Random.Range(0, fresnelPresets.Length)];
                    fresnelColor2 = fresnelPresets[UnityEngine.Random.Range(0, fresnelPresets.Length)];
                }
            }
        }

        // If the material is emissive, we set the emissive color property.
        // NOTE: This bool's name is misleading. It only refers to the lighting colors eg. headlight, taillight, secondary light. It does NOT refer to emissive secondary colors / emissive rims.
        if (isEmissiveMaterial)
        {
            baseColor = colorPresets[UnityEngine.Random.Range(2, colorPresets.Length)];
            targetMaterial.SetColor("_EmissionColor", baseColor);
            targetMaterial.EnableKeyword("_EMISSION");
        }
        else
        {
            // Apply the base color to the material.
            targetMaterial.color = baseColor;

            // Apply Fresnel colors if applicable.
            if (targetMaterial.HasProperty("_FresnelColor"))
            {
                targetMaterial.SetColor("_FresnelColor", fresnelColor);
            }

            if (targetMaterial.HasProperty("_FresnelColor2"))
            {
                targetMaterial.SetColor("_FresnelColor2", fresnelColor2);
            }

            // Special handling for primary color.
            if (colorType == ColorType.PRIMARY_COLOR)
            {
                // 1/100 chance for metallic value of 1.
                float metallicValue = (UnityEngine.Random.Range(0, 100) == 0) ? 1f : NON_METALLIC_DEFAULT;
                targetMaterial.SetFloat("_Metallic", metallicValue);

                // Set secondary color to match primary color.
                secondColor.color = baseColor;
                if (secondColor.HasProperty("_FresnelColor"))
                    secondColor.SetColor("_FresnelColor", fresnelColor);
                if (secondColor.HasProperty("_FresnelColor2"))
                    secondColor.SetColor("_FresnelColor2", fresnelColor2);
                secondColor.SetFloat("_Metallic", metallicValue);

                secondColor.SetColor("_EmissionColor", Color.black);
                secondColor.DisableKeyword("_EMISSION");
            }
        }

        // Special handling for rim color. ADD 1/20 CHANCE OF METALLIC RANDOMIZATION HERE
        if (colorType == ColorType.RIM_COLOR)
        {
            float metallicValue = (UnityEngine.Random.Range(0, 5) == 0) ? 1f : RIM_NON_METALLIC_DEFAULT;
            targetMaterial.SetFloat("_Metallic", metallicValue);

            targetMaterial.SetColor("_EmissionColor", Color.black);
            targetMaterial.EnableKeyword("_EMISSION");
        }
    }

    public void ApplyLivery(int index)
    {
        if (index == 0)
        {
            primColor.SetTexture("_LiveryMap", null);
            primColor.DisableKeyword("_AKMU_CARPAINT_LIVERY");
        }
        else
        {
            primColor.SetTexture("_LiveryMap", liveries[index]); // Assuming you have a shader property named _LiveryMap
            primColor.EnableKeyword("_AKMU_CARPAINT_LIVERY");
        }
    }

    public void SetDefaultColors()
    {
        primColor.color = defaultPrimaryColor;
        primColor.SetColor("_FresnelColor", defaultPrimaryColor);
        primColor.SetColor("_FresnelColor2", defaultPrimaryColor);
        ApplyMetallicToMaterial(primColor, NON_METALLIC_DEFAULT);

        if (emmissiveDefaultSecondaryColor)
        {
            secondColor.color = Color.black;
            secondColor.SetColor("_FresnelColor", Color.black);
            secondColor.SetColor("_FresnelColor2", Color.black);
            secondColor.SetColor("_EmissionColor", defaultSecondaryColor);
            secondColor.EnableKeyword("_EMISSION");
            ApplyMetallicToMaterial(secondColor, NON_METALLIC_DEFAULT);
        }
        else
        {
            secondColor.color = defaultSecondaryColor;
            secondColor.SetColor("_FresnelColor", defaultSecondaryColor);
            secondColor.SetColor("_FresnelColor2", defaultSecondaryColor);
            secondColor.SetColor("_EmissionColor", Color.black);
            secondColor.DisableKeyword("_EMISSION");
            ApplyMetallicToMaterial(secondColor, NON_METALLIC_DEFAULT);
        }

        rimColor.color = defaultRimColor;
        rimColor.SetColor("_EmissionColor", Color.black);
        rimColor.DisableKeyword("_EMISSION");

        rimColor.SetColor("_Color", defaultRimColor);
        rimColor.SetColor("_FresnelColor", defaultRimColor);
        rimColor.SetColor("_FresnelColor2", defaultRimColor);
        ApplyMetallicToMaterial(rimColor, RIM_NON_METALLIC_DEFAULT);

        primLight.color = Color.black;
        primLight.SetColor("_EmissionColor", defaultPrimaryLightColor);
        ApplyMetallicToMaterial(primLight, NON_METALLIC_DEFAULT);

        secondLight.color = Color.black;
        secondLight.SetColor("_EmissionColor", defaultSecondaryLightColor);
        ApplyMetallicToMaterial(secondLight, NON_METALLIC_DEFAULT);

        tailLight.color = Color.black;
        tailLight.SetColor("_EmissionColor", defaultTailLightColor);
        ApplyMetallicToMaterial(tailLight, NON_METALLIC_DEFAULT);
    }

    public void InitializeCar(string carType, int carIndex, bool isOwned = true)
    {
        currentCarType = carType;
        currentCarIndex = carIndex;

        Transform carTransform = carModel.transform.Find("BODY");

        // Access the SaveData instance and retrieve the car's data.
        SaveData saveData = SaveManager.Instance.SaveData;
        if (!saveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
        {
            SetDefaultColors();
        }

        // Activate the currently installed exhaust.
        PartHolder exhaustHolder = carTransform.Find("EXHAUSTS").GetComponent<PartHolder>();
        int savedExhaustIndex = isOwned ? carData.CarParts[0].CurrentInstalledPart : -1; // Index 0 corresponds to "EXHAUSTS"
        int index = 0;
        foreach (CarPart exhaust in exhaustHolder.GetPartArray())
        {
            if (savedExhaustIndex == -1 && exhaust.name == DefaultExhaust)
            {
                exhaust.gameObject.SetActive(true);

                // Set the current exhausts to the default exhausts & mark the default exhausts as being owned.
                if (isOwned)
                {
                    carData.CarParts[0].CurrentInstalledPart = index;
                    carData.CarParts[0].Ownership[index] = true;
                }
            }
            else
            {
                exhaust.gameObject.SetActive(false);
            }
            index++;
        }
        if (savedExhaustIndex != -1)
        {
            exhaustHolder.ActivatePart(savedExhaustIndex);
        }


        // Activate the currently installed front splitter.
        PartHolder frontSplitterHolder = carTransform.Find("FRONT_SPLITTERS").GetComponent<PartHolder>();
        int savedFrontSplitterIndex = isOwned ? carData.CarParts[1].CurrentInstalledPart : -1; // Index 1 corresponds to FRONT_SPLITTERS.
        if (savedFrontSplitterIndex == -1) // No splitter installed, handle default logic.
        {
            savedFrontSplitterIndex = 0; // 'None' front splitter.
            if (isOwned)
            {
                carData.CarParts[1].CurrentInstalledPart = savedFrontSplitterIndex;
                carData.CarParts[1].Ownership[savedFrontSplitterIndex] = true; // Mark the default part as owned.
            }
        }
        foreach (CarPart frontSplitter in frontSplitterHolder.GetPartArray())
        {
            frontSplitter.gameObject.SetActive(false);
        }
        frontSplitterHolder.ActivatePart(savedFrontSplitterIndex);

        // Activate the currently installed front wheels.
        PartHolder frontWheelHolder = carModel.transform.Find("FRONT_WHEELS").GetComponent<PartHolder>();
        int savedFrontWheelIndex = isOwned ? carData.CarParts[2].CurrentInstalledPart : -1; // 2 = FRONT_WHEELS
        index = 0;
        foreach (CarPart frontWheel in frontWheelHolder.GetPartArray())
        {
            if (savedFrontWheelIndex == -1 && frontWheel.name == DefaultFrontWheels)
            {
                frontWheel.gameObject.SetActive(true);

                // Set the current front wheels to the default front wheels & nark the default front wheels as being owned.
                if (isOwned)
                {
                    carData.CarParts[2].CurrentInstalledPart = index;
                    carData.CarParts[2].Ownership[index] = true;
                }
            }
            else
            {
                frontWheel.gameObject.SetActive(false);
            }
            index++;
        }
        if (savedFrontWheelIndex != -1)
        {
            frontWheelHolder.ActivatePart(savedFrontWheelIndex);
        }


        // Activate the currently installed rear diffuser.
        PartHolder rearSplitterHolder = carTransform.Find("REAR_SPLITTERS").GetComponent<PartHolder>();
        int savedRearSplitterIndex = isOwned ? carData.CarParts[3].CurrentInstalledPart : -1; // 3 = REAR_SPLITTERS
        index = 0;
        foreach (CarPart rearSplitter in rearSplitterHolder.GetPartArray())
        {
            if (savedRearSplitterIndex == -1 && rearSplitter.name == DefaultRearSplitter)
            {
                rearSplitter.gameObject.SetActive(true);

                // Set the current rear splitter to the default rear splitter & mark the default rear splitter as being owned.
                if (isOwned)
                {
                    carData.CarParts[3].CurrentInstalledPart = index;
                    carData.CarParts[3].Ownership[index] = true;
                }
            }
            else
            {
                rearSplitter.gameObject.SetActive(false);
            }
            index++;
        }
        if (savedRearSplitterIndex != -1)
        {
            rearSplitterHolder.ActivatePart(savedRearSplitterIndex);
        }


        // Activate the currently installed rear wheels.
        PartHolder rearWheelHolder = carModel.transform.Find("REAR_WHEELS").GetComponent<PartHolder>();
        int savedRearWheelIndex = isOwned ? carData.CarParts[4].CurrentInstalledPart : -1; // 4 = REAR_WHEELS
        index = 0;
        foreach (CarPart rearWheel in rearWheelHolder.GetPartArray())
        {
            if (savedRearWheelIndex == -1 && rearWheel.name == DefaultRearWheels)
            {
                rearWheel.gameObject.SetActive(true);

                // Set the current rear wheels to the default rear wheels & mark the default rear wheels as being owned.
                if (isOwned)
                {
                    carData.CarParts[4].CurrentInstalledPart = index;
                    carData.CarParts[4].Ownership[index] = true;
                }
            }
            else
            {
                rearWheel.gameObject.SetActive(false);
            }
            index++;
        }
        if (savedRearWheelIndex != -1)
        {
            rearWheelHolder.ActivatePart(savedRearWheelIndex);
        }


        // Activate the currently installed sideskirts.
        PartHolder sideskirtsHolder = carTransform.Find("SIDESKIRTS").GetComponent<PartHolder>();
        int savedSideskirtsIndex = isOwned ? carData.CarParts[5].CurrentInstalledPart : -1; // 5 = SIDESKIRTS
        if (savedSideskirtsIndex == -1) // No sideskirt installed, handle default logic.
        {
            savedSideskirtsIndex = 0; // 'None' sideskirt.
            if (isOwned)
            {
                carData.CarParts[5].CurrentInstalledPart = savedSideskirtsIndex;
                carData.CarParts[5].Ownership[savedSideskirtsIndex] = true; // Mark the default part as owned.
            }
        }
        foreach (CarPart sideskirt in sideskirtsHolder.GetPartArray())
        {
            sideskirt.gameObject.SetActive(false);
        }
        sideskirtsHolder.ActivatePart(savedSideskirtsIndex);


        // Activate the currently installed spoiler.
        PartHolder spoilersHolder = carTransform.Find("SPOILERS").GetComponent<PartHolder>();
        int savedSpoilersIndex = isOwned ? carData.CarParts[6].CurrentInstalledPart : -1; // 6 = SPOILERS
        index = 0;
        foreach (CarPart spoiler in spoilersHolder.GetPartArray())
        {
            if (savedSpoilersIndex == -1 && spoiler.name == DefaultSpoiler)
            {
                spoiler.gameObject.SetActive(true);

                // Set the current spoiler to the default spoiler & mark the default spoiler as being owned..
                if (isOwned)
                {
                    carData.CarParts[6].CurrentInstalledPart = index;
                    carData.CarParts[6].Ownership[index] = true;
                }
            }
            else
            {
                spoiler.gameObject.SetActive(false);
            }
            index++;
        }
        if (savedSpoilersIndex != -1)
        {
            spoilersHolder.ActivatePart(savedSpoilersIndex);
        }


        // Lower the car to the currently installed suspension height.
        PartHolder suspensionsHolder = carTransform.GetComponent<PartHolder>();
        int savedSuspensionsIndex = isOwned ? carData.CarParts[7].CurrentInstalledPart : -1; // 7 = SUSPENSIONS
        if (savedSuspensionsIndex == -1) // No suspension installed, handle default logic.
        {
            savedSuspensionsIndex = 0; // 'None' sideskirt.
            if (isOwned)
            {
                carData.CarParts[7].CurrentInstalledPart = savedSuspensionsIndex;
                carData.CarParts[7].Ownership[savedSuspensionsIndex] = true; // Mark the default part as owned.
            }
        }
        foreach (CarPart suspension in suspensionsHolder.GetPartArray())
        {
            suspension.gameObject.SetActive(false);
        }
        suspensionsHolder.ActivatePart(savedSuspensionsIndex);
        suspensionsHolder.SetSuspensionHeight(savedSuspensionsIndex);


        Transform performanceParts = carTransform.Find("PERFORMANCE_PARTS").transform;

        // Set the top speed of the car based on currently installed engine part.
        PartHolder engineHolder = performanceParts.Find("ENGINE").GetComponent<PartHolder>();
        int savedEngineIndex = isOwned ? carData.CarParts[8].CurrentInstalledPart : -1; // 8 = ENGINE
        index = 0;
        if (savedEngineIndex == -1) // No engine parts installed, handle default logic.
        {
            savedEngineIndex = 0; // 'Stock' engine.
            if (isOwned)
            {
                carData.CarParts[8].CurrentInstalledPart = savedEngineIndex;
                carData.CarParts[8].Ownership[savedEngineIndex] = true; // Mark the default part as owned.
            }
        }
        foreach (CarPart engine in engineHolder.GetPartArray())
        {
            if (index == savedEngineIndex)
                accelMaxValue = defaultAccelMaxValue * engine.accelMaxValueUpgrade;
            engine.gameObject.SetActive(false);
            index++;
        }
        engineHolder.ActivatePart(savedEngineIndex);


        // Set the acceleration of the car based on currently installed transmission part.
        PartHolder transmissionHolder = performanceParts.Find("TRANSMISSION").GetComponent<PartHolder>();
        int savedTransmissionIndex = isOwned ? carData.CarParts[9].CurrentInstalledPart : -1; // 9 = TRANSMISSION
        index = 0;
        if (savedTransmissionIndex == -1) // No transmission parts installed, handle default logic.
        {
            savedTransmissionIndex = 0; // 'Stock' transmission.
            if (isOwned)
            {
                carData.CarParts[9].CurrentInstalledPart = savedTransmissionIndex;
                carData.CarParts[9].Ownership[savedTransmissionIndex] = true; // Mark the default part as owned.
            }
        }
        foreach (CarPart transmission in transmissionHolder.GetPartArray())
        {
            if (index == savedTransmissionIndex)
                accelIncreaseRate = defaultAccelIncreaseRate * transmission.accelIncreaseRateUpgrade;
            transmission.gameObject.SetActive(false);
            index++;
        }
        transmissionHolder.ActivatePart(savedTransmissionIndex);


        // Set the number of lives of the car based on the currently installed max lives part.
        PartHolder livesHolder = performanceParts.Find("LIVES").GetComponent<PartHolder>();
        int savedLivesIndex = isOwned ? carData.CarParts[10].CurrentInstalledPart : -1; // 10 = LIVES
        index = 0;
        if (savedLivesIndex == -1) // No lives parts installed, handle default logic.
        {
            savedLivesIndex = 0; // 'Stock' lives part.
            if (isOwned)
            {
                carData.CarParts[10].CurrentInstalledPart = savedLivesIndex;
                carData.CarParts[10].Ownership[savedLivesIndex] = true; // Mark the default part as owned.
            }
        }
        foreach (CarPart lives in livesHolder.GetPartArray())
        {
            if (index == savedLivesIndex)
                numlives = lives.maxLives;
            lives.gameObject.SetActive(false);
            index++;
        }
        livesHolder.ActivatePart(savedLivesIndex);


        // Set the currently installed decal set onto the car.
        PartHolder decalsHolder = carTransform.Find("DECALS").GetComponent<PartHolder>();
        int savedDecalsIndex = isOwned ? carData.CarParts[11].CurrentInstalledPart : -1; // 11 = DECALS
        if (savedDecalsIndex == -1) // No decals installed, handle default logic.
        {
            savedDecalsIndex = 0; // 'None' decals.
            if (isOwned)
            {
                carData.CarParts[11].CurrentInstalledPart = savedDecalsIndex;
                carData.CarParts[11].Ownership[savedDecalsIndex] = true; // Mark the default part as owned.
            }
        }
        foreach (CarPart decal in decalsHolder.GetPartArray())
        {
            decal.gameObject.SetActive(false);
        }
        decalsHolder.ActivatePart(savedDecalsIndex);


        // Set the currently installed livery onto the car.
        PartHolder liveryHolder = carTransform.Find("LIVERIES").GetComponent<PartHolder>();
        int savedLiveryIndex = isOwned ? carData.CarParts[12].CurrentInstalledPart : -1; // 12 = LIVERIES
        if (savedLiveryIndex == -1) // No livery installed, handle default logic.
        {
            savedLiveryIndex = 0; // 'None' livery.
            if (isOwned)
            {
                carData.CarParts[12].CurrentInstalledPart = savedLiveryIndex;
                carData.CarParts[12].Ownership[savedLiveryIndex] = true; // Mark the default part as owned.
            }
        }
        foreach (CarPart livery in liveryHolder.GetPartArray())
        {
            livery.gameObject.SetActive(false);
        }
        liveryHolder.ActivatePart(savedLiveryIndex);
        ApplyLivery(savedLiveryIndex);

        if (isOwned)
        {
            ApplySavedColors(primColor, ColorType.PRIMARY_COLOR, false, defaultPrimaryColor);
            ApplySavedColors(secondColor, ColorType.SECONDARY_COLOR, false, defaultSecondaryColor);
            ApplySavedColors(rimColor, ColorType.RIM_COLOR, false, defaultRimColor);
            ApplySavedColors(primLight, ColorType.PRIMARY_LIGHT, true, defaultPrimaryLightColor);
            ApplySavedColors(secondLight, ColorType.SECONDARY_LIGHT, true, defaultSecondaryLightColor);
            ApplySavedColors(tailLight, ColorType.TAIL_LIGHT, true, defaultTailLightColor);
        }

        primColor.SetFloat("_Brightness", 1.21f);
        secondColor.SetFloat("_Brightness", 1.21f);
        rimColor.SetFloat("_Brightness", 1.21f);

        // Save the updated data to disk.
        SaveManager.Instance.SaveGame();
    }

    // Spawn car with randomized customizations. Will be used for cars spawned from lootboxes.
    public void RandomizeCar(string carType, int carIndex, bool isOwned = true)
    {
        currentCarType = carType;
        currentCarIndex = carIndex;

        // Access the SaveData instance and current car's data.
        SaveData saveData = SaveManager.Instance.SaveData;
        if (!saveData.Cars.TryGetValue((currentCarType, currentCarIndex), out SaveData.CarData carData))
        {
            // Do nothing.
        }

        Transform carTransform = carModel.transform.Find("BODY");

        // Randomize the exhaust.
        PartHolder exhaustHolder = carTransform.Find("EXHAUSTS").GetComponent<PartHolder>();
        int randomExhaustIndex = UnityEngine.Random.Range(0, exhaustHolder.GetPartArray().Length);
        foreach (CarPart exhaust in exhaustHolder.GetPartArray())
        {
            exhaust.gameObject.SetActive(false);
        }
        exhaustHolder.ActivatePart(randomExhaustIndex);
        if (isOwned)
        {
            carData.CarParts[0].CurrentInstalledPart = randomExhaustIndex;
            carData.CarParts[0].Ownership[randomExhaustIndex] = true;
        }

        // Randomize the front splitter.
        PartHolder frontSplitterHolder = carTransform.Find("FRONT_SPLITTERS").GetComponent<PartHolder>();
        int randomFrontSplitterIndex = UnityEngine.Random.Range(0, frontSplitterHolder.GetPartArray().Length);
        foreach (CarPart frontSplitter in frontSplitterHolder.GetPartArray())
        {
            frontSplitter.gameObject.SetActive(false);
        }
        frontSplitterHolder.ActivatePart(randomFrontSplitterIndex);
        if (isOwned)
        {
            carData.CarParts[1].CurrentInstalledPart = randomFrontSplitterIndex;
            carData.CarParts[1].Ownership[randomFrontSplitterIndex] = true;
        }

        // Randomize the front wheels.
        PartHolder frontWheelHolder = carModel.transform.Find("FRONT_WHEELS").GetComponent<PartHolder>();
        int randomFrontWheelIndex = UnityEngine.Random.Range(0, frontWheelHolder.GetPartArray().Length);
        foreach (CarPart frontWheel in frontWheelHolder.GetPartArray())
        {
            frontWheel.gameObject.SetActive(false);
        }
        frontWheelHolder.ActivatePart(randomFrontWheelIndex);
        if (isOwned)
        {
            carData.CarParts[2].CurrentInstalledPart = randomFrontWheelIndex;
            carData.CarParts[2].Ownership[randomFrontWheelIndex] = true;
        }

        // Randomize the rear splitter.
        PartHolder rearSplitterHolder = carTransform.Find("REAR_SPLITTERS").GetComponent<PartHolder>();
        int randomRearSplitterIndex = UnityEngine.Random.Range(0, rearSplitterHolder.GetPartArray().Length);
        foreach (CarPart rearSplitter in rearSplitterHolder.GetPartArray())
        {
            rearSplitter.gameObject.SetActive(false);
        }
        rearSplitterHolder.ActivatePart(randomRearSplitterIndex);
        if (isOwned)
        {
            carData.CarParts[3].CurrentInstalledPart = randomRearSplitterIndex;
            carData.CarParts[3].Ownership[randomRearSplitterIndex] = true;
        }

        // Randomize the rear wheels.
        PartHolder rearWheelHolder = carModel.transform.Find("REAR_WHEELS").GetComponent<PartHolder>();
        int randomRearWheelIndex = UnityEngine.Random.Range(0, rearWheelHolder.GetPartArray().Length);
        foreach (CarPart rearWheel in rearWheelHolder.GetPartArray())
        {
            rearWheel.gameObject.SetActive(false);
        }
        rearWheelHolder.ActivatePart(randomRearWheelIndex);
        if (isOwned)
        {
            carData.CarParts[4].CurrentInstalledPart = randomRearWheelIndex;
            carData.CarParts[4].Ownership[randomRearWheelIndex] = true;
        }

        // Randomize the sideskirts.
        PartHolder sideskirtsHolder = carTransform.Find("SIDESKIRTS").GetComponent<PartHolder>();
        int randomSideskirtsIndex = UnityEngine.Random.Range(0, sideskirtsHolder.GetPartArray().Length);
        foreach (CarPart sideskirt in sideskirtsHolder.GetPartArray())
        {
            sideskirt.gameObject.SetActive(false);
        }
        sideskirtsHolder.ActivatePart(randomSideskirtsIndex);
        if (isOwned)
        {
            carData.CarParts[5].CurrentInstalledPart = randomSideskirtsIndex;
            carData.CarParts[5].Ownership[randomSideskirtsIndex] = true;
        }

        // Randomize the spoiler.
        PartHolder spoilersHolder = carTransform.Find("SPOILERS").GetComponent<PartHolder>();
        int randomSpoilersIndex = UnityEngine.Random.Range(0, spoilersHolder.GetPartArray().Length);
        foreach (CarPart spoiler in spoilersHolder.GetPartArray())
        {
            spoiler.gameObject.SetActive(false);
        }
        spoilersHolder.ActivatePart(randomSpoilersIndex);
        if (isOwned)
        {
            carData.CarParts[6].CurrentInstalledPart = randomSpoilersIndex;
            carData.CarParts[6].Ownership[randomSpoilersIndex] = true;
        }

        // Randomize the suspension.
        PartHolder suspensionsHolder = carTransform.GetComponent<PartHolder>();
        int randomSuspensionsIndex = UnityEngine.Random.Range(0, suspensionsHolder.GetPartArray().Length);
        foreach (CarPart suspension in suspensionsHolder.GetPartArray())
        {
            suspension.gameObject.SetActive(false);
        }
        suspensionsHolder.ActivatePart(randomSuspensionsIndex);
        if (isOwned)
        {
            carData.CarParts[7].CurrentInstalledPart = randomSuspensionsIndex;
            carData.CarParts[7].Ownership[randomSuspensionsIndex] = true;     
        }
        suspensionsHolder.SetSuspensionHeight(randomSuspensionsIndex);

        // Randomize the engine.
        PartHolder engineHolder = carTransform.Find("PERFORMANCE_PARTS/ENGINE").GetComponent<PartHolder>();
        int randomEngineIndex = UnityEngine.Random.Range(0, engineHolder.GetPartArray().Length);
        foreach (CarPart engine in engineHolder.GetPartArray())
        {
            engine.gameObject.SetActive(false);
        }
        engineHolder.ActivatePart(randomEngineIndex);
        if (isOwned)
        {
            carData.CarParts[8].CurrentInstalledPart = randomEngineIndex;
            carData.CarParts[8].Ownership[randomEngineIndex] = true;
        }

        // Randomize the transmission.
        PartHolder transmissionHolder = carTransform.Find("PERFORMANCE_PARTS/TRANSMISSION").GetComponent<PartHolder>();
        int randomTransmissionIndex = UnityEngine.Random.Range(0, transmissionHolder.GetPartArray().Length);
        foreach (CarPart transmission in transmissionHolder.GetPartArray())
        {
            transmission.gameObject.SetActive(false);
        }
        transmissionHolder.ActivatePart(randomTransmissionIndex);
        if (isOwned)
        {
            carData.CarParts[9].CurrentInstalledPart = randomTransmissionIndex;
            carData.CarParts[9].Ownership[randomTransmissionIndex] = true;
        }

        // Randomize the lives.
        PartHolder livesHolder = carTransform.Find("PERFORMANCE_PARTS/LIVES").GetComponent<PartHolder>();
        int randomLivesIndex = UnityEngine.Random.Range(0, livesHolder.GetPartArray().Length);
        foreach (CarPart lives in livesHolder.GetPartArray())
        {
            lives.gameObject.SetActive(false);
        }
        livesHolder.ActivatePart(randomLivesIndex);
        if (isOwned)
        {
            carData.CarParts[10].CurrentInstalledPart = randomLivesIndex;
            carData.CarParts[10].Ownership[randomLivesIndex] = true;
        }

        // Randomize the decals.
        PartHolder decalsHolder = carTransform.Find("DECALS").GetComponent<PartHolder>();
        int randomDecalValue = UnityEngine.Random.Range(1, 11);
        int randomDecalsIndex;
        if (randomDecalValue == 1) randomDecalsIndex = UnityEngine.Random.Range(0, decalsHolder.GetPartArray().Length);
        else randomDecalsIndex = 0;

        foreach (CarPart decal in decalsHolder.GetPartArray())
        {
            decal.gameObject.SetActive(false);
        }
        decalsHolder.ActivatePart(randomDecalsIndex);
        if (isOwned)
        {
            carData.CarParts[11].CurrentInstalledPart = randomDecalsIndex;
            carData.CarParts[11].Ownership[randomDecalsIndex] = true;
        }
        forceNeutralBodyColors = (randomDecalsIndex != 0); // If decals are set, we only randomize the car's colors to neutral grays/whites to prevent hideousness.

        // Don't randomize the liveries
        PartHolder liveryHolder = carTransform.Find("LIVERIES").GetComponent<PartHolder>();
        foreach (CarPart livery in liveryHolder.GetPartArray())
        {
            livery.gameObject.SetActive(false);
        }
        liveryHolder.ActivatePart(0);
        if (isOwned)
        {
            carData.CarParts[12].CurrentInstalledPart = 0;
            carData.CarParts[12].Ownership[0] = true;
        }
        ApplyLivery(0);

        // Randomize colors.
        RandomizeColors(primColor, ColorType.PRIMARY_COLOR, false);
        RandomizeColors(rimColor, ColorType.RIM_COLOR, false);
        RandomizeColors(primLight, ColorType.PRIMARY_LIGHT, true);
        RandomizeColors(secondLight, ColorType.SECONDARY_LIGHT, true);
        RandomizeColors(tailLight, ColorType.TAIL_LIGHT, true);

        primColor.SetFloat("_Brightness", 1.21f);

        // Save the updated data to disk.
        SaveManager.Instance.SaveGame();
    }
}
