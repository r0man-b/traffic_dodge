using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // Customization/CreditManager.cs
    public long GlobalCredits;

    // Game/UIManager.cs
    public float RecordDistance;
    public double RecordSpeed;

    // Game/SoundManager.cs
    public int LastRaceSongIndex;

    // Game/PrefabManager.cs && Menu/OptionsMenu.cs
    public int TrafficDensity = -1;
    public int CurrentEnvironment = 0;
    public bool City77EnvironmentPurchased = true;
    public bool ApocalypticWastelandEnvironmentPurchased = false;
    public bool GalacticHighwayEnvironmentPurchased = false;
    public bool TransatlanticTunnelEnvironmentPurchased = false;

    // Menu/Car.cs && Menu/CarDisplay.cs && Menu/GarageUIManager.cs && Game/PlayerController.cs
    public Dictionary<(string CarType, int CarIndex), CarData> Cars = new Dictionary<(string, int), CarData>();
    public string CurrentCarType;  // Currently displayed car type.
    public int CurrentCarIndex; // Currently displayed car index.
    public string LastOwnedCarType;
    public int LastOwnedCarIndex;
    public int NitroCount;
    public float DistanceUntilNextNitro = 100.0f;
    [System.Serializable]
    public class CarData
    {
        // Array of 13 PartData elements for each part type.
        /*  0 =>  "EXHAUSTS",
            1 =>  "FRONT_SPLITTERS",
            2 =>  "FRONT_WHEELS",
            3 =>  "REAR_SPLITTERS",
            4 =>  "REAR_WHEELS",
            5 =>  "SIDESKIRTS",
            6 =>  "SPOILERS",
            7 =>  "SUSPENSIONS",
            8 =>  "ENGINE",
            9 =>  "TRANSMISSION",
            10 => "LIVES",
            11 => "DECALS",
            12 => "LIVERIES"
        */
        public PartData[] CarParts = new PartData[13];

        // Array of 6 ColorData elements for colors, indexed by whichPartToPaint.
        /*  0 => PRIMARY_COLOR,
            1 => SECONDARY_COLOR,
            2 => RIM_COLOR,
            3 => PRIMARY_LIGHT,
            4 => SECONDARY_LIGHT,
            5 => TAIL_LIGHT
        */
        public ColorData[] Colors = new ColorData[6];

        public CarData()
        {
            for (int i = 0; i < CarParts.Length; i++)
            {
                CarParts[i] = new PartData(); // Initialize each part with a new PartData.
            }

            for (int i = 0; i < Colors.Length; i++)
            {
                Colors[i] = new ColorData(); // Initialize each color with a new ColorData.
            }
        }
    }
    [System.Serializable]
    public class PartData
    {
        public int CurrentInstalledPart = -1; // Index to keep track of the currently OWNED and INSTALLED car part in this part type.
        public Dictionary<int, bool> Ownership = new Dictionary<int, bool>();  // Dictionary to keep track of all OWNED car parts in this part type.
    }
    [System.Serializable]
    public class ColorData
    {
        public float[] BaseColor = { -1f, -1f, -1f, -1f };        // RGBA
        public float[] FresnelColor = { -1f, -1f, -1f, -1f };     // For Pearlescent colours only.
        public float[] FresnelColor2 = { -1f, -1f, -1f, -1f };    // For Pearlescent colours only.
        public float[] EmissionColor = { -1f, -1f, -1f, -1f };    // For emissive colors.
        public float MetallicMap = 0.304f;                        // For metallic colours.

        public int SelectedPaintType = -1;    // 0=Matte, 1=Gloss, 2=Pearl, 3=Emissive
        public int SelectedPresetIndex = -1;  // palette button index within that bucket
    }

    // Menu/GarageMusic.cs
    public int LastGarageSongIndex = 0; // Stores the last played song index in the garage.

    // Menu/OptionsMenu/DisplaySettings.cs
    public float GammaValue = 0.08f;  // Default gamma value (1.0 = normal brightness).
    public bool VignetteEnabled = false; // Default state of vignette effect.

    // Menu/OptionsMenu/SoundSettings.cs
    public float MusicVolume = 0.5f;   // Global volume multiplier for all music.
    public float EffectsVolumeMultiplier = 0.5f; // Global volume multiplier for all effects.

    // Menu/OptionsMenu/GraphicsSettings.cs
    // Basic Options
    public int graphicsPresetIndex = 2; // Default to Medium
    public bool isCustomGraphics = false;

    // Advanced Options
    public float renderScale = 1f;          // 100% render scale
    public int aaSettings = 2;              // 2x MSAA
    public bool UseLowPolyTraffic = false;  // High-poly traffic models
    public bool ShadowsEnabled = true;      // Enable/disable shadows
    public int frameRate = 60;              // Default application framerate
    
    // Menu/OptionsMenu/GameplaySettings.cs
    public int cameraType = 1; // 0 = first person, 1 = third person low, 2 = third person high.
    public float senseOfSpeedModifier = 0.55f;

    // Menu/OptionsMenu/UnitsSettings.cs
    public bool ImperialUnits = true;

    // Menu/OptionsMenu/LanguageSettings.cs
}
