using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Car", menuName = "Car Scriptable Object")]
public class Car : ScriptableObject
{
    private Color[] colorPresets = new Color[]
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
    public enum ColorType
    {
        PRIMARY_COLOR = 0,
        SECONDARY_COLOR = 1,
        RIM_COLOR = 2,
        PRIMARY_LIGHT = 3,
        SECONDARY_LIGHT = 4,
        TAIL_LIGHT = 5
    }
    private int currentCarType;
    private int currentCarIndex;

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

        // Check if it's an emission color and return black if it's null or empty.
        /*if (isEmission && (colorData.EmissionColor == null || colorData.EmissionColor.Length == 0))
        {
            Debug.Log(Time.time + " RETURNING BLACK FOR EMMISSIVE COLOR INDEX " + colorIndex);
            return Color.black;
        }*/

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
                    break;

                case (int)ColorType.RIM_COLOR:
                    colorData.BaseColor[0] = defaultColor.r;
                    colorData.BaseColor[1] = defaultColor.g;
                    colorData.BaseColor[2] = defaultColor.b;
                    colorData.BaseColor[3] = defaultColor.a;

                    /*  UNCOMMENT WHEN RIMS GET CONVERTED TO METALLIC
                    colorData.FresnelColor[0] = defaultColor.r;
                    colorData.FresnelColor[1] = defaultColor.g;
                    colorData.FresnelColor[2] = defaultColor.b;
                    colorData.FresnelColor[3] = defaultColor.a;

                    colorData.FresnelColor[0] = defaultColor.r;
                    colorData.FresnelColor[1] = defaultColor.g;
                    colorData.FresnelColor[2] = defaultColor.b;
                    colorData.FresnelColor[3] = defaultColor.a;*/
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

        // Apply emission if needed.
        if (isEmissiveMaterial)
        {
            targetMaterial.SetColor("_EmissionColor", baseColor.Value);
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
            if (!(colorData.EmissionColor[0] == 0f && colorData.EmissionColor[1] == 0f && colorData.EmissionColor[2] == 0f && colorData.EmissionColor[3] == 0f)
                && !(colorData.EmissionColor[0] == -1f && colorData.EmissionColor[1] == -1f && colorData.EmissionColor[2] == -1f && colorData.EmissionColor[3] == -1f))
            {
                targetMaterial.color = Color.black;
                Color emissionColor = new Color(colorData.EmissionColor[0], colorData.EmissionColor[1], colorData.EmissionColor[2], colorData.EmissionColor[3]);
                targetMaterial.SetColor("_EmissionColor", emissionColor);
                targetMaterial.EnableKeyword("_EMISSION");
            }
        }
    }
    
    // Randomize colors for randomly customized cars from lootboxes.
    /*public void RandomizeColors(Material targetMaterial, string colorType, bool isEmissiveMaterial)
    {
        Color? baseColor = colorPresets[UnityEngine.Random.Range(0, colorPresets.Length)];
        Color? fresnelColor = colorPresets[UnityEngine.Random.Range(0, colorPresets.Length)];
        Color? fresnelColor2 = colorPresets[UnityEngine.Random.Range(0, colorPresets.Length)];

        // If the material is emissive, we set the emissive color property.
        // NOTE: This bool's name is misleading. It only refers to the lighting colors eg. headlight, taillight, secondary light. It does NOT refer to emissive secondary colors / emissive rims.
        if (isEmissiveMaterial)
        {
            baseColor = colorPresets[UnityEngine.Random.Range(2, colorPresets.Length)];
            if (baseColor.HasValue)
            {
                targetMaterial.SetColor("_EmissionColor", baseColor.Value);
                targetMaterial.EnableKeyword("_EMISSION");
            }
        }
        else
        {
            // If not emissive, we set the base color property.
            if (baseColor.HasValue)
            {
                targetMaterial.color = baseColor.Value;
                if (colorType == "_PRIMARY_COLOR") // Set secondary colour equal to primary.
                {
                    secondColor.color = baseColor.Value;
                }
            }

            if (targetMaterial.HasProperty("_FresnelColor") && fresnelColor.HasValue)
            {
                targetMaterial.SetColor("_FresnelColor", fresnelColor.Value);
                if (colorType == "_PRIMARY_COLOR") // Set secondary colour equal to primary.
                {
                    secondColor.SetColor("_FresnelColor", fresnelColor.Value);
                }
            }

            if (targetMaterial.HasProperty("_FresnelColor2") && fresnelColor2.HasValue)
            {
                targetMaterial.SetColor("_FresnelColor2", fresnelColor2.Value);
                if (colorType == "_PRIMARY_COLOR") // Set secondary colour equal to primary.
                {
                    secondColor.SetColor("_FresnelColor2", fresnelColor2.Value);
                }
            }

            if (colorType == "_PRIMARY_COLOR")
            {
                // 1/100 chance for metallic map value of 1, otherwise 0.304
                float metallicValue = (UnityEngine.Random.Range(0, 100) == 0) ? 1f : 0.304f;
                targetMaterial.SetFloat("_Metallic", metallicValue); // Set metallic value.
                secondColor.SetFloat("_Metallic", metallicValue);
            }
        }

        if (colorType == "_SECONDARY_COLOR")
        {
            targetMaterial.SetColor("_EmissionColor", Color.black);
            targetMaterial.EnableKeyword("_EMISSION");
        }

        if (colorType == "_RIM_COLOR")
        {
            targetMaterial.SetColor("_EmissionColor", Color.black);
            targetMaterial.EnableKeyword("_EMISSION");
        }
    }*/

    public void RandomizeColors(Material targetMaterial, ColorType colorType, bool isEmissiveMaterial)         // TODO: ADD SAVING LOGIC TO THIS FUNCTION
    {
        // Generate random base color and Fresnel colors.
        Color baseColor = colorPresets[UnityEngine.Random.Range(0, colorPresets.Length)];
        Color fresnelColor = colorPresets[UnityEngine.Random.Range(0, colorPresets.Length)];
        Color fresnelColor2 = colorPresets[UnityEngine.Random.Range(0, colorPresets.Length)];

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
                // Set secondary color to match primary color.
                secondColor.color = baseColor;
                if (secondColor.HasProperty("_FresnelColor"))
                    secondColor.SetColor("_FresnelColor", fresnelColor);
                if (secondColor.HasProperty("_FresnelColor2"))
                    secondColor.SetColor("_FresnelColor2", fresnelColor2);

                // 1/100 chance for metallic value of 1, otherwise 0.1.
                float metallicValue = (UnityEngine.Random.Range(0, 100) == 0) ? 1f : 0.1f;
                targetMaterial.SetFloat("_Metallic", metallicValue);
                secondColor.SetFloat("_Metallic", metallicValue);
            }
        }

        // Special handling for secondary color.
        if (colorType == ColorType.SECONDARY_COLOR)
        {
            targetMaterial.SetColor("_EmissionColor", Color.black);
            targetMaterial.EnableKeyword("_EMISSION");
        }

        // Special handling for rim color. ADD 1/20 CHANCE OF METALLIC RANDOMIZATION HERE
        if (colorType == ColorType.RIM_COLOR)
        {
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
            //primaryColor.mainTexture = liveries[change]; // Assuming liveries is an accessible member of this class
            primColor.SetTexture("_LiveryMap", liveries[index]); // Assuming you have a shader property named _LiveryMap
            primColor.EnableKeyword("_AKMU_CARPAINT_LIVERY");
        }
    }

    public void SetDefaultColors()
    {
        float metallicValue = 0.304f;

        primColor.color = defaultPrimaryColor;
        primColor.SetColor("_FresnelColor", defaultPrimaryColor);
        primColor.SetColor("_FresnelColor2", defaultPrimaryColor);
        primColor.SetFloat("_Metallic", metallicValue);

        if (emmissiveDefaultSecondaryColor)
        {
            secondColor.color = Color.black;
            secondColor.SetColor("_FresnelColor", Color.black);
            secondColor.SetColor("_FresnelColor2", Color.black);
            secondColor.SetColor("_EmissionColor", defaultSecondaryColor);
            secondColor.EnableKeyword("_EMISSION");
            secondColor.SetFloat("_Metallic", metallicValue);
        }
        else
        {
            secondColor.color = defaultSecondaryColor;
            secondColor.SetColor("_FresnelColor", defaultSecondaryColor);
            secondColor.SetColor("_FresnelColor2", defaultSecondaryColor);
            secondColor.SetColor("_EmissionColor", Color.black);
            secondColor.DisableKeyword("_EMISSION");
            secondColor.SetFloat("_Metallic", metallicValue);
        }

        rimColor.color = defaultRimColor;
        rimColor.SetColor("_EmissionColor", Color.black);
        rimColor.DisableKeyword("_EMISSION");
        rimColor.SetFloat("_Metallic", metallicValue);
        /*  UNCOMMENT WHEN RIMS GET CONVERTED TO METALLIC
        rimColor.SetColor("_Color", defaultRimColor);
        rimColor.SetColor("_FresnelColor", defaultRimColor);*/

        primLight.color = Color.black;
        primLight.SetColor("_EmissionColor", defaultPrimaryLightColor);

        secondLight.color = Color.black;
        secondLight.SetColor("_EmissionColor", defaultSecondaryLightColor);

        tailLight.color = Color.black;
        tailLight.SetColor("_EmissionColor", defaultTailLightColor);
    }

    public void InitializeCar(int carType, int carIndex, bool isOwned = true)
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

        // Save the updated data to disk.
        SaveManager.Instance.SaveGame();
    }

    // Spawn car with randomized customizations. Will be used for cars spawned from lootboxes.
    public void RandomizeCar(int carType, int carIndex, bool isOwned = true)
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
        int randomDecalsIndex = UnityEngine.Random.Range(0, decalsHolder.GetPartArray().Length);
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

        // Randomize the liveries.
        int randomNumber = UnityEngine.Random.Range(1, 11); // 10% chance of having car spawn with custom livery.
        PartHolder liveryHolder = carTransform.Find("LIVERIES").GetComponent<PartHolder>();
        foreach (CarPart livery in liveryHolder.GetPartArray())
        {
            livery.gameObject.SetActive(false);
        }
        if (randomNumber == 1) // Apply random livery.
        {
            int randomLiveryIndex = UnityEngine.Random.Range(0, liveryHolder.GetPartArray().Length);

            liveryHolder.ActivatePart(randomLiveryIndex);
            if (isOwned)
            {
                carData.CarParts[12].CurrentInstalledPart = randomLiveryIndex;
                carData.CarParts[12].Ownership[randomLiveryIndex] = true;               
            }
            ApplyLivery(randomLiveryIndex);
        }
        else // No random livery.
        {
            liveryHolder.ActivatePart(0);
            if (isOwned)
            {
                carData.CarParts[12].CurrentInstalledPart = 0;
                carData.CarParts[12].Ownership[0] = true;              
            }
            ApplyLivery(0);
        }

        // Randomize colors.
        RandomizeColors(primColor, ColorType.PRIMARY_COLOR, false);
        RandomizeColors(rimColor, ColorType.RIM_COLOR, false);
        RandomizeColors(primLight, ColorType.PRIMARY_LIGHT, true);
        RandomizeColors(secondLight, ColorType.SECONDARY_LIGHT, true);
        RandomizeColors(tailLight, ColorType.TAIL_LIGHT, true);

        // Save the updated data to disk.
        SaveManager.Instance.SaveGame();
    }
}
