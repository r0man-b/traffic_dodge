using AkilliMum.SRP.CarPaint;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using CarPaintNew = AkilliMum.SRP.CarPaint.CarsPaint;
using CarPaintOld = AkilliMum.SRP.CarPaintOld.CarsPaint;
using BoundingBox_Old = AkilliMum.SRP.CarPaintOld.BoundingBox;
using TextureSize_Old = AkilliMum.SRP.CarPaintOld.TextureSizeType;

public class PlayerController : MonoBehaviour
{
    // Playercar variables.
    public float accel = 0.5f;
    private float accelMaxValue = 3.38f;
    public float accelTimeOffset = 0.0f;
    public int currentLane = 4;
    private int whichWay = 0; // -1 = left, 0 = straight, 1 = right.
    private float lastLaneSplitTime = -5.0f;
    public bool currentlyLaneSplitting = false;
    private Coroutine laneSplitCoroutine;
    private float lastXPosition;
    public int numlives = 0;
    public Car currentCar;
    private string currentCarType;
    private int currentCarIndex;
    public GameObject carObject;
    public GameObject sparks;
    private GameObject leftSteam;
    private GameObject rightSteam;
    private GameObject leftSteamEffect;
    private GameObject rightSteamEffect;
    private GameObject frontWheels;
    private GameObject rearWheels;
    public GameObject firstPersonSteamSource;
    private bool camberedFrontWheelsZAxis;
    private bool camberedFrontWheelsXAxis;
    private bool camberedRearWheelsZAxis;
    private bool camberedRearWheelsXAxis;
    private GameObject camberedWheelsFrontLeft;
    private GameObject camberedWheelsFrontRight;
    private GameObject camberedWheelsRearLeft;
    private GameObject camberedWheelsRearRight;
    private Quaternion defaultRot;
    private Quaternion rotLeft;
    private Quaternion rotRight;
    // Recovery flash configuration.
    private float recoverDuration = 3f;       // Total flashing time
    private float startFlashInterval = 0.25f; // Slowest interval at start (seconds)
    private float endFlashInterval = 0.02f; // Fastest interval at end (seconds)
    private bool isRecovering = false;

    // Camera variables.
    public GameObject cameraObject;
    public Camera cam;
    private Vector3 defaultCamPosition;
    private float camPosY;
    private float defaultCamPosY;
    private float camPosZ; //= -5.5f;
    private float shakeIntensity = 0.05f; // The initial intensity of the camera shake.
    private readonly float shakeIncreaseRate = 0.007f;
    private float explosionShakeIntensity = 1f; // The initial intensity of the explosion shake.
    private float minXPosition;
    private float maxXPosition;
    private float minYPosition;
    private float maxYPosition;
    private bool cameraFovLerped = false;
    public bool inSidewaysJolt = false;
    public float cameraOffsetValue1 = 0.033f;
    public float cameraOffsetValue2 = 0.4f;
    private int cameraType;
    private float cameraHeightMultiplier;
    private float cameraZMultiplier;
    private float senseOfSpeedModifier;
    Vector3 newPosition;
    Vector3 startingLocalCamPosition;
    Quaternion startingLocalCamRotation;

    // Powerup variables.
    public bool aggro = false;
    public bool tornado = false;
    public bool bullet = false;
    private bool powerupsOnStandby = true;
    private float powerupCountdown;
    public float oldAccel;
    private float oldFov;
    private float oldMotionBlur;
    private float oldVignette;
    public bool tornadoExplodeCars = false;
    public GameObject tornadoObject;
    bool isTornadoMovingRight = true;
    private float tornadoPosZ;
    private float timeSinceLastPowerup;

    // ParticleSystem variables.
    public GameObject explosionParent;
    public ParticleSystem explosion;
    public ParticleSystem rain;
    private Vector3 rainDefaultPosition;
    private Quaternion rainDefaultRotation;

    // Input variables.
    private float touchDownTime = 0;
    private float touchUpTime = 0;
    private bool isTouchDown = false;
    private bool isTouchUp = true;
    private int lastTouchDirection = -1; // 0 = left, 1 = right.

    // State variables.
    public bool raceStarted = false;
    public bool gameEnd = false;
    private bool inTrafficExplosion = false;
    private bool inTornadoExplosion = false;
    private bool inBulletExplosion = false;
    private bool hasReturnedPosteriorObjects = false;

    // System variables.
    public bool invincible;
    private float startTime = 0;
    private SoundManager soundManager;
    public UIManager uiManager;
    public GameObject pauseButton;
    public PauseMenu pauseMenu;
    private PrefabManager prefabManager;
    private PostProcessManager postProcessManager;

    // Car collection.
    [SerializeField] private CarCollection carCollection;

    // Macros.
    private const float TORNADO_CAMERA_SHAKE = 0.5f;
    private const float BULLET_CAMERA_SHAKE = 10000f;

    private int currentEnvironment;

    // Map car type name -> index in CarCollection.carTypes
    private Dictionary<string, int> carTypeIndexByName = new();

    private int GetCarTypeIndex(string typeName)
    {
        if (!carTypeIndexByName.TryGetValue(typeName, out var idx))
            throw new KeyNotFoundException($"Car type '{typeName}' not found in CarCollection.");
        return idx;
    }

    private void SetUpCar()
    {
        // Retrieve the Car instance from the collection using the string key.
        int typeIdx = GetCarTypeIndex(currentCarType);
        currentCar = (Car)carCollection.carTypes[typeIdx].items[currentCarIndex];

        currentCar.InitializeCar(currentCarType, currentCarIndex, currentCar.carModel.transform);
        carObject = Instantiate(currentCar.carModel, Vector3.zero, Quaternion.identity, this.transform);
        carObject.transform.localPosition = currentCar.raceSpawnPosition;


        accelMaxValue = currentCar.accelMaxValue;
        if (!invincible) numlives = currentCar.numlives;

        leftSteam = carObject.transform.Find("BODY").transform.Find("LeftSteam").gameObject;
        rightSteam = carObject.transform.Find("BODY").transform.Find("RightSteam").gameObject;
        leftSteamEffect = leftSteam.transform.Find("LeftSteamEffect").gameObject;
        rightSteamEffect = rightSteam.transform.Find("RightSteamEffect").gameObject;
        frontWheels = carObject.transform.Find("FRONT_WHEELS").gameObject;
        rearWheels = carObject.transform.Find("REAR_WHEELS").gameObject;

        Transform activeChild = null;
        foreach (Transform child in frontWheels.transform)
        {
            if (child.gameObject.activeInHierarchy)
            {
                activeChild = child;
                break; // exit the loop once the active child is found
            }
        }
        if (activeChild != null)
        {
            frontWheels = activeChild.gameObject;
            if (frontWheels.name == "CAMBER GOD") camberedFrontWheelsZAxis = true;
            else if (frontWheels.name == "INTERVENTION II") camberedFrontWheelsXAxis = true;
            if (camberedFrontWheelsZAxis || camberedFrontWheelsXAxis)
            {
                foreach (Transform child in frontWheels.transform)
                {
                    Debug.Log(child.name);
                    if (child.name.EndsWith("_LEFT"))
                    {
                        camberedWheelsFrontLeft = child.gameObject;
                    }
                    else if (child.name.EndsWith("_RIGHT"))
                    {
                        camberedWheelsFrontRight = child.gameObject;
                    }
                }
            }
        }

        activeChild = null;
        foreach (Transform child in rearWheels.transform)
        {
            if (child.gameObject.activeInHierarchy)
            {
                activeChild = child;
                break; // exit the loop once the active child is found
            }
        }
        if (activeChild != null)
        {
            rearWheels = activeChild.gameObject;
            if (rearWheels.name == "CAMBER GOD") camberedRearWheelsZAxis = true;
            else if (rearWheels.name == "INTERVENTION II") camberedRearWheelsXAxis = true;
            if (camberedRearWheelsZAxis || camberedRearWheelsXAxis)
            {
                foreach (Transform child in rearWheels.transform)
                {
                    if (child.name.EndsWith("_LEFT"))
                    {
                        camberedWheelsRearLeft = child.gameObject;
                    }
                    else if (child.name.EndsWith("_RIGHT"))
                    {
                        camberedWheelsRearRight = child.gameObject;
                    }
                }
            }
        }

        // Remove/disable the new CarsPaint, add the old CarsPaint, copy parameters (except TextureSize and Xth frame).
        var newCp = carObject.GetComponent<CarPaintNew>();

        // Ensure we have an old component
        var oldCp = carObject.GetComponent<CarPaintOld>();
        if (oldCp == null)
        {
            oldCp = carObject.AddComponent<CarPaintOld>();
        }

        // If the new component exists, copy matching settings over to old (except TextureSize and RunForEveryXthFrame)
        if (newCp != null)
        {
            // Core toggles
            oldCp.IsEnabled = newCp.IsEnabled;
            oldCp.IsDebug = newCp.IsDebug;

            // Bounding box (enum values match; cast via int)
            oldCp.BoundingBox = (BoundingBox_Old)(int)newCp.BoundingBox;

            // Performance / quality (exclude RunForEveryXthFrame and TextureSize here by requirement)
            oldCp.UseOcclusionCulling = newCp.UseOcclusionCulling;
            oldCp.HDR = newCp.HDR;
            oldCp.CameraLODLevel = newCp.CameraLODLevel;
            oldCp.DisablePixelLights = newCp.DisablePixelLights;
            oldCp.ShadowDistance = newCp.ShadowDistance;

            // Culling & clipping
            oldCp.ReflectLayers = newCp.ReflectLayers;
            oldCp.ClippingPlaneNear = newCp.ClippingPlaneNear;
            oldCp.ClippingPlaneFar = newCp.ClippingPlaneFar;

            // Material mixing
            oldCp._MixMultiplier = newCp._MixMultiplier;

            // Remove or disable the new component
            Destroy(newCp); // avoids SRP callbacks and cost
        }

        // Set TextureSize and Xth frame explicitly for the old component
        oldCp.TextureSize = TextureSize_Old.x256;  // per your requirement
        oldCp.RunForEveryXthFrame = 2;             // per your requirement

        // Optional brightness boost logic (your existing helper)
        BoostBrightnessIfMetallic(currentCar.primColor, 3f);
        BoostBrightnessIfMetallic(currentCar.secondColor, 3f);
        BoostBrightnessIfMetallic(currentCar.rimColor, 3f);

        // Initialize probe/renderers so it is ready on first frame
        oldCp.InitializeProperties();
    }

    void Awake()
    {
        // Access the SaveData instance.
        SaveData saveData = SaveManager.Instance.SaveData;
        
        // Get current environment.
        currentEnvironment = saveData.CurrentEnvironment;

        // Retrieve the current car type (string) and index from SaveData.
        currentCarType = saveData.LastOwnedCarType;
        currentCarIndex = saveData.LastOwnedCarIndex;

        // Build name -> index map once
        carTypeIndexByName.Clear();
        for (int i = 0; i < carCollection.carTypes.Count; i++)
        {
            var bucket = carCollection.carTypes[i];
            if (bucket.items == null || bucket.items.Count == 0) continue;

            // Preferred: Car ScriptableObject exposes a stable type name, e.g., public string TypeName
            var firstCar = bucket.items[0] as Car;
            if (firstCar == null)
                throw new System.InvalidOperationException($"CarCollection.carTypes[{i}] contains a non-Car asset.");

            string typeName = firstCar.car_name;      // if you have Car.TypeName
            if (string.IsNullOrWhiteSpace(typeName))
                typeName = firstCar.name;             // fallback to asset name if needed

            if (carTypeIndexByName.ContainsKey(typeName))
                throw new System.InvalidOperationException($"Duplicate car type name '{typeName}' in CarCollection.");

            carTypeIndexByName[typeName] = i;
        }

        SetUpCar();

        // Set up camera type and sense of speed values from the gameplay options.
        cameraType = saveData.cameraType;
        senseOfSpeedModifier = saveData.senseOfSpeedModifier;
        if (cameraType == 0) // Don't render car if we're in first person
        {
            cameraHeightMultiplier = 0.5f;
            cameraZMultiplier = 5;
            foreach (Transform child in carObject.transform)
            {
                child.gameObject.SetActive(false);
            }
        }
        else if (cameraType == 1)
        {
            cameraHeightMultiplier = 1;
            cameraZMultiplier = 0;
        }
        else
        {
            cameraHeightMultiplier = 1.05f;
            cameraZMultiplier = 0;
        }

        // Disable rain if we're in space or underwater.
        if (currentEnvironment == 2 || currentEnvironment == 3)
        {
            rain.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        // Start clock.
        startTime = Time.time;

        // Set camera variables.
        defaultCamPosition = currentCar.defaultCameraPosition;
        camPosY = defaultCamPosition.y * cameraHeightMultiplier;
        defaultCamPosition.y = camPosY;
        cameraObject.transform.position = new Vector3(defaultCamPosition.x, camPosY, defaultCamPosition.z);
        defaultCamPosY = camPosY;
        minYPosition = camPosY - 0.066f * cameraHeightMultiplier;
        maxYPosition = camPosY + 0.066f * cameraHeightMultiplier;
        startingLocalCamPosition = cam.transform.localPosition;
        startingLocalCamRotation = cam.transform.localRotation;

        // Set playercar variables.
        defaultRot = transform.rotation;
        rotLeft = transform.rotation * Quaternion.Euler(0, -10, 0);
        rotRight = transform.rotation * Quaternion.Euler(0, 10, 0);

        // Cache default rain transform for later restoration on recovery.
        rainDefaultPosition = rain.transform.position;
        rainDefaultRotation = rain.transform.rotation;

        // Find 'SoundManager' script.
        GameObject SoundManagerObject = GameObject.Find("SoundManager");
        soundManager = SoundManagerObject.GetComponent<SoundManager>();

        // Find 'UIManager' script.
        GameObject CanvasObject = GameObject.Find("Canvas");
        uiManager = CanvasObject.GetComponent<UIManager>();

        // Find 'PrefabManager' script.
        GameObject PrefabManagerObject = GameObject.Find("PrefabManager");
        prefabManager = PrefabManagerObject.GetComponent<PrefabManager>();

        // Find 'PostProcessManager' script.
        GameObject PostProcessingObject = GameObject.Find("PostProcessing");
        postProcessManager = PostProcessingObject.GetComponent<PostProcessManager>();

        // FOR TRAILER ONLY
        //if (autoSplit) invincible = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            uiManager.gameObject.SetActive(!uiManager.gameObject.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            if (invincible)
            {
                numlives = 25;
            }
            else
            {
                numlives = 0;
            }
            invincible = !invincible;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Cursor.visible = !Cursor.visible;
        }

        // Move the player, camera, and tornadoObject forward.
        this.transform.position += 50 * Time.deltaTime * accel * this.transform.forward;
        cameraObject.transform.position += 50 * Time.deltaTime * accel * this.transform.forward;

        tornadoPosZ = this.transform.position.z + (-0.2028745f * cam.fieldOfView + 40.61494f);
        tornadoObject.transform.position = new Vector3(tornadoObject.transform.position.x, tornadoObject.transform.position.y, tornadoPosZ);

        camPosZ = cameraObject.transform.position.z; // TODO: WTF is the point of this if it gets overwritten below?

        // Rotation speed for tornado.
        float rotationSpeed = 50000 * Time.deltaTime * accel;
        Vector3 rotationAmount = new Vector3(rotationSpeed, 0, 0); // Rotate around X-axis

        // Rotate any cambered wheels if they exist.
        if (camberedFrontWheelsZAxis)
        {
            camberedWheelsFrontLeft.transform.Rotate(new Vector3(0, 0, rotationSpeed));
            camberedWheelsFrontRight.transform.Rotate(new Vector3(0, 0, rotationSpeed));
        }
        else frontWheels.transform.Rotate(rotationAmount);
        
        if (camberedRearWheelsZAxis)
        {
            camberedWheelsRearLeft.transform.Rotate(new Vector3(0, 0, rotationSpeed));
            camberedWheelsRearRight.transform.Rotate(new Vector3(0, 0, rotationSpeed));
        }
        else rearWheels.transform.Rotate(rotationAmount);

        // Update race started variable if the song has dropped.
        if (!raceStarted && Time.time - startTime > soundManager.drop) raceStarted = true;

        // Disable powerup effect.
        if (!powerupsOnStandby && Time.time - startTime >= powerupCountdown)
        {
            powerupsOnStandby = true;
            StartCoroutine(DisableAllPowerups());
        }
        else if (Time.time - startTime >= (powerupCountdown - 0.5f) && aggro) // Start playing aggro zoom out sound if 0.5 seconds left in aggro.
        {
            if (!(soundManager.zoomoutsource.isPlaying))
                soundManager.PlayAggro(true);
        }

        // Cinematic camera intro sequence before race starts (0–3s)
        float timeSinceStart = Time.time - startTime;
        if (timeSinceStart < soundManager.drop)
        {
            Vector3 frontLeftLocalPos = new Vector3(-1.20000002f, -0.750000021f, 6.6500001f);
            Quaternion frontLeftLocalRot = new Quaternion(-0.00363085279f, 0.925662994f, 0.0088860495f, 0.378227264f);

            Vector3 rearLeftLocalPos = new Vector3(-1.15999997f, -0.7f, 1.89999998f);
            Quaternion rearLeftLocalRot = new Quaternion(-0.0368956365f, 0.341510594f, 0.013417975f, 0.939057589f);

            const float snapDuration = 0.15f;
            float t = timeSinceStart;

            if (t < 1f)
            {
                cam.transform.localPosition = frontLeftLocalPos;
                cam.transform.localRotation = frontLeftLocalRot;
            }
            else if (t < 2f)
            {
                float phaseTime = t - 1f;
                float lerpT = Mathf.Clamp01(phaseTime / snapDuration);
                cam.transform.localPosition = Vector3.Lerp(frontLeftLocalPos, rearLeftLocalPos, lerpT);
                cam.transform.localRotation = Quaternion.Slerp(frontLeftLocalRot, rearLeftLocalRot, lerpT);
                if (!hasReturnedPosteriorObjects)
                {
                    prefabManager.ReturnPosteriorObjects();
                    hasReturnedPosteriorObjects = true;
                }
            }
            else if (t < 3f)
            {
                float phaseTime = t - 2f;
                float lerpT = Mathf.Clamp01(phaseTime / snapDuration);
                cam.transform.localPosition = Vector3.Lerp(rearLeftLocalPos, startingLocalCamPosition, lerpT);
                cam.transform.localRotation = Quaternion.Slerp(rearLeftLocalRot, startingLocalCamRotation, lerpT);
            }

            return; // Prevent camera logic below from overriding during cinematic
        }

        // Increase acceleration, apply fov increase and camera shake.
        if (raceStarted)
        {
            if (Time.time - startTime > soundManager.drop + 0.1f)
            {
                if (gameEnd) newPosition.y = Mathf.Clamp(newPosition.y, 0.006f * cam.fieldOfView + 1.72f, 4.2f);
                else if (!bullet) newPosition.y = Mathf.Clamp(newPosition.y, 0.006f * cam.fieldOfView + 1.72f, 3f * cameraType);
                else newPosition.y = Mathf.Max(newPosition.y, 0.01946308724f * cam.fieldOfView);
                cameraObject.transform.localPosition = Vector3.Lerp(cameraObject.transform.localPosition, new Vector3(newPosition.x, newPosition.y, cameraObject.transform.localPosition.z), 8f * Time.deltaTime * explosionShakeIntensity);
            }
            else if (!cameraFovLerped)
            {
                cameraFovLerped = true;
                StartCoroutine(LerpCameraFOV(cam.fieldOfView, 35, 0.1f));
            }

            if (gameEnd && explosionShakeIntensity > 1)
            {
                if (shakeIntensity > 0.3f)
                {
                    float xoffset = 0.001f * shakeIntensity * 200 * explosionShakeIntensity;
                    float yoffset = 0.0075f * shakeIntensity * 10 * explosionShakeIntensity;
                    minXPosition = this.transform.position.x - xoffset;
                    maxXPosition = this.transform.position.x + xoffset;
                    minYPosition = defaultCamPosY - yoffset;
                    maxYPosition = defaultCamPosY + yoffset;
                }
                else if (shakeIntensity > 0.2f)
                {
                    float xoffset = 0.011f * shakeIntensity * 200 * explosionShakeIntensity;
                    float yoffset = 0.015f * shakeIntensity * 10 * explosionShakeIntensity;
                    minXPosition = this.transform.position.x - xoffset;
                    maxXPosition = this.transform.position.x + xoffset;
                    minYPosition = defaultCamPosY - yoffset;
                    maxYPosition = defaultCamPosY + yoffset;
                }
                else
                {
                    float xoffset = 0.011f * shakeIntensity * 200 * explosionShakeIntensity;
                    float yoffset = 0.022f * shakeIntensity * 20 * explosionShakeIntensity;
                    minXPosition = this.transform.position.x - xoffset;
                    maxXPosition = this.transform.position.x + xoffset;
                    minYPosition = defaultCamPosY - yoffset;
                    maxYPosition = defaultCamPosY + yoffset;
                }
                minXPosition = Mathf.Clamp(minXPosition, this.transform.position.x - 3f, this.transform.position.x + 3f);
                maxXPosition = Mathf.Clamp(maxXPosition, this.transform.position.x - 3f, this.transform.position.x + 3f);
            }
            else if (explosionShakeIntensity > 1)
            {
                if (shakeIntensity > 0.3f)
                {
                    float xoffset = 0.001f * shakeIntensity * 200 * accel * accel * explosionShakeIntensity / 10;
                    float yoffset = 0.0075f * shakeIntensity * 10 * accel * accel * explosionShakeIntensity / 10;
                    minXPosition = this.transform.position.x - xoffset;
                    maxXPosition = this.transform.position.x + xoffset;
                    minYPosition = defaultCamPosY - yoffset;
                    maxYPosition = defaultCamPosY + yoffset;
                }
                else if (shakeIntensity > 0.2f)
                {
                    minXPosition = this.transform.position.x - 0.011f * shakeIntensity * 200 * accel * accel * explosionShakeIntensity / 10;
                    maxXPosition = this.transform.position.x + 0.011f * shakeIntensity * 200 * accel * accel * explosionShakeIntensity / 10;
                    minYPosition = defaultCamPosY - 0.015f * shakeIntensity * 10 * accel * explosionShakeIntensity / 10;
                    maxYPosition = defaultCamPosY + 0.015f * shakeIntensity * 10 * accel * explosionShakeIntensity / 10;
                }
                else
                {
                    minXPosition = this.transform.position.x - 0.011f * shakeIntensity * 200 * accel * explosionShakeIntensity / 10;
                    maxXPosition = this.transform.position.x + 0.011f * shakeIntensity * 200 * accel * explosionShakeIntensity / 10;
                    minYPosition = defaultCamPosY - 0.022f * shakeIntensity * 20 * explosionShakeIntensity / 10;
                    maxYPosition = defaultCamPosY + 0.022f * shakeIntensity * 20 * explosionShakeIntensity / 10;
                }
                minYPosition = Mathf.Clamp(minYPosition, 2f, 4f);
                maxYPosition = Mathf.Clamp(maxYPosition, 2f, 4f);
                minXPosition = Mathf.Clamp(minXPosition, this.transform.position.x - 0.25f * accel, this.transform.position.x + 0.25f * accel);
                maxXPosition = Mathf.Clamp(maxXPosition, this.transform.position.x - 0.25f * accel, this.transform.position.x + 0.25f * accel);
            }
            else
            {
                if (shakeIntensity > 0.3f)
                {
                    float xoffset = 0.001f * shakeIntensity * 200 * accel * accel;
                    float yoffset = 0.0075f * shakeIntensity * 10 * accel * accel;
                    if (!tornado)
                    {
                        xoffset = Mathf.Clamp(xoffset, -0.5f, 0.5f);
                        yoffset = Mathf.Clamp(yoffset, -0.18f, 0.18f);
                    }
                    minXPosition = this.transform.position.x - xoffset;
                    maxXPosition = this.transform.position.x + xoffset;
                    minYPosition = defaultCamPosY - yoffset;
                    maxYPosition = defaultCamPosY + yoffset;
                }
                else if (shakeIntensity > 0.2f)
                {
                    minXPosition = this.transform.position.x - 0.011f * shakeIntensity * 200 * accel * accel;
                    maxXPosition = this.transform.position.x + 0.011f * shakeIntensity * 200 * accel * accel;
                    minYPosition = defaultCamPosY - 0.015f * shakeIntensity * 10 * accel;
                    maxYPosition = defaultCamPosY + 0.015f * shakeIntensity * 10 * accel;
                }
                else
                {
                    minXPosition = this.transform.position.x - 0.011f * shakeIntensity * 200 * accel;
                    maxXPosition = this.transform.position.x + 0.011f * shakeIntensity * 200 * accel;
                    minYPosition = defaultCamPosY - 0.022f * shakeIntensity * 20;
                    maxYPosition = defaultCamPosY + 0.022f * shakeIntensity * 20;
                }
            }
            cameraObject.transform.position = Vector3.Lerp(cameraObject.transform.position, new Vector3(cameraObject.transform.position.x, defaultCamPosition.y, camPosZ), Time.deltaTime);
            defaultCamPosition = cameraObject.transform.position;

            // Create a new random offset to simulate camera shake.
            {
                float shakeOffsetX = Random.Range(-shakeIntensity, shakeIntensity);
                float shakeOffsetY = Random.Range(-shakeIntensity, shakeIntensity);
                Vector3 shakeOffset = new(shakeOffsetX, shakeOffsetY, 0f);

                // Apply the offset to the camera & clamp y position to make sure it doesn't stray away.
                newPosition = defaultCamPosition + shakeOffset;
                newPosition.y = Mathf.Clamp(newPosition.y, minYPosition, maxYPosition);
                newPosition.x = Mathf.Clamp(newPosition.x, minXPosition, maxXPosition);

                // Continue increasing the shake intensity until time has reached 180 seconds.
                if (Time.time - (startTime + accelTimeOffset) < 180 && !aggro && !bullet && shakeIntensity < 0.4f)
                {
                    shakeIntensity += 2 * shakeIncreaseRate * senseOfSpeedModifier * Time.deltaTime;
                }
            }


            // If the game has not ended, continue increasing variables.
            if (!gameEnd)
            {
                // Update left & right rotations.
                if (bullet)
                {
                   rotLeft = defaultRot * Quaternion.Euler(0, -5, 0);
                   rotRight = defaultRot * Quaternion.Euler(0, 5, 0);
                }
                else
                {
                    rotLeft = defaultRot * Quaternion.Euler(0, -10 * (1 / accel), 0);
                    rotRight = defaultRot * Quaternion.Euler(0, 10 * (1 / accel), 0);
                }

                // Increase acceleration.
                if (accel < accelMaxValue && !aggro)
                {
                    float timeThreshold = Time.time - ((startTime + accelTimeOffset) + soundManager.drop);
                    accel = (float)System.Math.Log10(timeThreshold * 0.22f + 1) * currentCar.accelIncreaseRate + 0.5f;
                }

                // Increase camera FOV with acceleration.
                if (accel < 2.38f /*215 mph is the hard cap on FOV*/ && accel < accelMaxValue && !aggro && Time.time - startTime > soundManager.drop + 0.1f) cam.fieldOfView = 46f * (accel - 0.5f) * senseOfSpeedModifier + 33.5f;

                // Move camera closer to player as he speeds up.
                if (!bullet)
                    camPosZ = this.transform.position.z + 0.0150065f * cam.fieldOfView + currentCar.defaultCameraPosition.z + 0.9174f + cameraZMultiplier;
                else
                    camPosZ = this.transform.position.z + 0.0150065f * cam.fieldOfView + currentCar.defaultCameraPosition.z + 0.9174f - (cam.fieldOfView - oldFov) / 20 + cameraZMultiplier;
                cameraObject.transform.position = Vector3.Lerp(cameraObject.transform.position, new Vector3(cameraObject.transform.position.x, cameraObject.transform.position.y, camPosZ), 0.001f * Time.deltaTime);
            }

            // If the game has ended or player is in an explosion, apply explosion shake to the camera.
            if (explosionShakeIntensity > 1 && (gameEnd || inTrafficExplosion || inTornadoExplosion || inBulletExplosion))
            {

                if (gameEnd)
                {
                    accel = 0; // Stop the playercar.
                }
                else if (inTrafficExplosion && !inTornadoExplosion)
                {
                    //StartCoroutine(CameraJolt());
                }
                else if (inTornadoExplosion)
                {
                }
                else // The sonic boom effect at the end of the bullet powerup.
                {
                }

                // Decrease the explosion shake intensity.
                explosionShakeIntensity -= gameEnd || inTornadoExplosion? (EaseOutCubic(explosionShakeIntensity / 2.5f * (gameEnd ? 1 : accel) * Time.deltaTime)) : (explosionShakeIntensity / 0.25f * Time.deltaTime);
                explosionShakeIntensity = Mathf.Clamp(explosionShakeIntensity, 1, explosionShakeIntensity);
            }

            // Reset the shake intensity and state variables if the explosion has finished and the game has not ended.
            if (explosionShakeIntensity <= 1 && !gameEnd)
            {
                explosionShakeIntensity = 1;
                inTrafficExplosion = false;
                inTornadoExplosion = false;
                inBulletExplosion = false;
            }
        }

        // Update movement value that determines how quickly we change lanes.
        float movement_val = Time.deltaTime * accel / 5;

        // If the bullet powerup is enabled, auto lane change the player.
        if ((bullet) && (raceStarted))
        {
            int desiredDirection = prefabManager.LaneChangeForBullet(currentLane, transform.position.z, false);
            if (desiredDirection < 0 && currentLane > 0) transform.rotation = Quaternion.Lerp(transform.rotation, rotLeft, 0.01f);
            else if (desiredDirection > 0 && currentLane < 7) transform.rotation = Quaternion.Lerp(transform.rotation, rotRight, 0.01f);
            else transform.rotation = Quaternion.Lerp(transform.rotation, defaultRot, 1f);
            SetCurrentLane(currentLane + desiredDirection, movement_val);
        }

        // Lane split if two simultaneous taps are detected.
        else if (
                    (Input.touchCount > 1 || (Input.GetKey(KeyCode.RightArrow) && Input.GetKey(KeyCode.LeftArrow)))
                    && !pauseMenu.gamePaused
                    && !currentlyLaneSplitting
                    && !gameEnd
                    && !bullet
                    && raceStarted
                )
        {
            if ((Time.time - startTime) - lastLaneSplitTime >= 5) // Only initiate a lane split if the lane split cooldown has expired.
            {
                if (laneSplitCoroutine != null)
                {
                    StopCoroutine(laneSplitCoroutine);
                    laneSplitCoroutine = null;
                }

                laneSplitCoroutine = StartCoroutine(LaneSplitRoutine(0.12f, 1f / accel, lastTouchDirection));
            }
            else SnapToClosestLane(movement_val);
        }

        // Move & rotate the player right if the right side of the screen is tapped.
        else if (
                    (Input.GetKey(KeyCode.RightArrow) || (Input.touchCount > 0 && Input.GetTouch(0).position.x >= Screen.width / 2))
                    && !pauseMenu.gamePaused
                    && !currentlyLaneSplitting
                    && !gameEnd
                    && !bullet
                    && raceStarted
                )
        {
            lastTouchDirection = 1;
            if (transform.position.x < 10) // Only start moving the player if we are within track boundaries.
            {
                if (!isTouchDown)
                {
                    lastXPosition = transform.position.x;
                    touchDownTime = (Time.time - startTime);
                    isTouchDown = true;
                    isTouchUp = false;
                }
                transform.Translate(25 * movement_val, 0, 0, Space.World);
                cameraObject.transform.Translate(25 * movement_val, 0, 0, Space.World);
                transform.rotation = Quaternion.Lerp(transform.rotation, rotRight, 9 * movement_val);
                carObject.transform.SetLocalPositionAndRotation
                (
                    Vector3.Lerp(carObject.transform.localPosition, carObject.transform.localPosition, 10 * movement_val),
                    Quaternion.Lerp(carObject.transform.localRotation, defaultRot, 18 * movement_val)
                );
                whichWay = 1;
            }
            else SnapToClosestLane(movement_val);
        }

        // Move & rotate the player left if the left side of the screen is tapped.
        else if (
                    (Input.GetKey(KeyCode.LeftArrow) || (Input.touchCount > 0 && Input.GetTouch(0).position.x < Screen.width / 2))
                    && !pauseMenu.gamePaused
                    && !currentlyLaneSplitting
                    && !gameEnd
                    && !bullet
                    && raceStarted
                )
        {
            lastTouchDirection = 0;
            if (transform.position.x > -11.5f)  // Only start moving the player if we are within track boundaries.
            {
                if (!isTouchDown)
                {
                    lastXPosition = transform.position.x;
                    touchDownTime = (Time.time - startTime);
                    isTouchDown = true;
                    isTouchUp = false;
                }
                transform.Translate(-25 * movement_val, 0, 0, Space.World);
                cameraObject.transform.Translate(-25 * movement_val, 0, 0, Space.World);
                transform.rotation = Quaternion.Lerp(transform.rotation, rotLeft, 9 * movement_val);
                carObject.transform.SetLocalPositionAndRotation
                (
                    Vector3.Lerp(carObject.transform.localPosition, carObject.transform.localPosition, 10 * movement_val),
                    Quaternion.Lerp(carObject.transform.localRotation, defaultRot, 18 * movement_val)
                );
                whichWay = -1;
            }
            else SnapToClosestLane(movement_val);
        }

        // If there are no player inputs, snap the player to the closest lane.
        else SnapToClosestLane(movement_val);
    }


    /*----------------------------------- PLAYER MOVEMENT FUNCTIONS -----------------------------------*/
    // Snaps the player to a lane based on their position.
    void SnapToClosestLane(float speed)
    {
        // Don't do anything if the player is in the process of lanesplitting.
        if (currentlyLaneSplitting) return;
        
        // Record the timestamp of the touch release.
        if (!isTouchUp)
        {
            touchUpTime = (Time.time - startTime);
            isTouchUp = true;
            isTouchDown = false;
        }

        // If the touch release & touch press was very close, move over one lane.
        if (Mathf.Abs(transform.position.x - lastXPosition) <= 3)
        {
            SetCurrentLane(currentLane + whichWay, speed / 2);
        }

        // Otherwise snap the player to the closest lane.
        else
        {
            if (transform.position.x < -10f)
                SetCurrentLane(0, speed / 2);

            else if (transform.position.x < -7f)
                SetCurrentLane(1, speed / 2);

            else if (transform.position.x < -4f)
                SetCurrentLane(2, speed / 2);

            else if (transform.position.x < -0.75f)
                SetCurrentLane(3, speed / 2);

            else if (transform.position.x < 2.5)
                SetCurrentLane(4, speed / 2);

            else if (transform.position.x < 5.5)
                SetCurrentLane(5, speed / 2);

            else if (transform.position.x < 8.5)
                SetCurrentLane(6, speed / 2);

            else
                SetCurrentLane(7, speed / 2);
        }

        // Reset player rotation & whichWay variable once lane change completed.
        transform.rotation = Quaternion.Lerp(transform.rotation, defaultRot, 18 * speed);
        whichWay = 0;
    }

    // Manually set the player's lane.
    void SetCurrentLane(int lane, float speed)
    {
        // Snap the player to lane 0.
        if (lane == 0)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(-11.5f, transform.position.y, transform.position.z), 10 * speed);
            cameraObject.transform.position = Vector3.Lerp(cameraObject.transform.position, new Vector3(-11.5f, defaultCamPosition.y, camPosZ), 10 * speed);
            currentLane = 0;
        }

        // Snap the player to lane 1.
        else if (lane == 1)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(-8.5f, transform.position.y, transform.position.z), 10 * speed);
            cameraObject.transform.position = Vector3.Lerp(cameraObject.transform.position, new Vector3(-8.5f, defaultCamPosition.y, camPosZ), 10 * speed);
            currentLane = 1;
        }

        // Snap the player to lane 2.
        else if (lane == 2)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(-5.5f, transform.position.y, transform.position.z), 10 * speed);
            cameraObject.transform.position = Vector3.Lerp(cameraObject.transform.position, new Vector3(-5.5f, defaultCamPosition.y, camPosZ), 10 * speed);
            currentLane = 2;
        }

        // Snap the player to lane 3.
        else if (lane == 3)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(-2.5f, transform.position.y, transform.position.z), 10 * speed);
            cameraObject.transform.position = Vector3.Lerp(cameraObject.transform.position, new Vector3(-2.5f, defaultCamPosition.y, camPosZ), 10 * speed);
            currentLane = 3;
        }

        // Snap the player to lane 4. The extra code in this block ensures that the camera zoom-in present
        // at the start of the race stays consistent even if the player immediatly starts changing lanes.
        else if (lane == 4)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(1f, transform.position.y, transform.position.z), 10 * speed);

            if (Time.time - startTime > soundManager.drop + 0.1f)
                cameraObject.transform.position = Vector3.Lerp(cameraObject.transform.position, new Vector3(1f, defaultCamPosition.y, camPosZ), 10 * speed);
            else
                cameraObject.transform.position = Vector3.Lerp(cameraObject.transform.position, new Vector3(1f, defaultCamPosition.y, camPosZ), 20 * Time.deltaTime);

            currentLane = 4;
        }

        // Snap the player to lane 5.
        else if (lane == 5)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(4f, transform.position.y, transform.position.z), 10 * speed);
            cameraObject.transform.position = Vector3.Lerp(cameraObject.transform.position, new Vector3(4f, defaultCamPosition.y, camPosZ), 10 * speed);
            currentLane = 5;
        }

        // Snap the player to lane 6.
        else if (lane == 6)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(7f, transform.position.y, transform.position.z), 10 * speed);
            cameraObject.transform.position = Vector3.Lerp(cameraObject.transform.position, new Vector3(7f, defaultCamPosition.y, camPosZ), 10 * speed);
            currentLane = 6;
        }

        // Snap the player to lane 7.
        else if (lane == 7)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(10f, transform.position.y, transform.position.z), 10 * speed);
            cameraObject.transform.position = Vector3.Lerp(cameraObject.transform.position, new Vector3(10f, defaultCamPosition.y, camPosZ), 10 * speed);
            currentLane = 7;
        }
    }

    // Return the lanesplit X positions of the camera & player transforms based on which lane we are in and which direction we are lane splitting.
    (float, float) GetTransformPositionsForLaneSplitting(int lane, int direction)
    {
        if (direction == 0) // We are lane splitting to the left.
        {
            if (lane == 0)
            {
                currentLane = 0;
                return (-12.75f, -12.75f);
            }

            else if (lane == 1)
            {
                currentLane = 1;
                return (-9.66f, -9.66f);
            }

            else if (lane == 2)
            {
                currentLane = 2;
                return (-6.66f, -6.66f);
            }

            else if (lane == 3)
            {
                currentLane = 3;
                return (-3.66f, -3.66f);
            }

            else if (lane == 4)
            {
                currentLane = 4;
                return (-0.5f, -0.5f);
            }

            else if (lane == 5)
            {
                currentLane = 5;
                return (2.75f, 2.75f);
            }

            else if (lane == 6)
            {
                currentLane = 6;
                return (5.75f, 5.75f);
            }

            else if (lane == 7)
            {
                currentLane = 7;
                return (8.75f, 8.75f);
            }
        }
        else  // We are lane splitting to the right.
        {
            if (lane == 0)
            {
                currentLane = 0;
                return (-10.15f, -10.15f);
            }

            else if (lane == 1)
            {
                currentLane = 1;
                return (-7.15f, -7.15f);
            }

            else if (lane == 2)
            {
                currentLane = 2;
                return (-4.15f, -4.15f);
            }

            else if (lane == 3)
            {
                currentLane = 3;
                return (-0.9f, -0.9f);
            }

            else if (lane == 4)
            {
                currentLane = 4;
                return (2.25f, 2.25f);
            }

            else if (lane == 5)
            {
                currentLane = 5;
                return (5.25f, 5.25f);
            }

            else if (lane == 6)
            {
                currentLane = 6;
                return (8.25f, 8.25f);
            }

            else if (lane == 7)
            {
                currentLane = 7;
                return (11.35f, 11.35f);
            }
        }
        return (0, 0);
    }

    // Coroutine for lane splitting the player.
    IEnumerator LaneSplitRoutine(float speed, float laneSplitTime, int direction /*0 = left, 1 = right*/)
    {
        currentlyLaneSplitting = true;
        whichWay = 0;
        // Display the steam effect.
        if (cameraType != 0)
        {
            if (direction == 0)
            {
                rightSteam.SetActive(true); // Display the effect opposite to the side we are rotating.
                rightSteamEffect.SetActive(true);
            }
            else
            {
                leftSteam.SetActive(true);
                leftSteamEffect.SetActive(true);
            }
        }
        else
        {
            firstPersonSteamSource.SetActive(true);
        }

        // Make rotation point straight to avoid drifting away.
        transform.rotation = defaultRot;

        // Initial positions of transform & camera.
        float initialPosition = transform.position.x;
        float initialCameraPosition = cameraObject.transform.position.x;

        // Final positions which will be assigned based on which direction we are going & which lane we are in.
        float finalPosition;
        float finalCameraPosition;

        // Initial camera rotations.
        Quaternion initialCameraRotation = cameraObject.transform.rotation;

        // Assign the final positions of transform & camera.
        if (transform.position.x < -10f)
            (finalPosition, finalCameraPosition) = GetTransformPositionsForLaneSplitting(0, direction);

        else if (transform.position.x < -7f)
            (finalPosition, finalCameraPosition) = GetTransformPositionsForLaneSplitting(1, direction);

        else if (transform.position.x < -4f)
            (finalPosition, finalCameraPosition) = GetTransformPositionsForLaneSplitting(2, direction);

        else if (transform.position.x < -0.75f)
            (finalPosition, finalCameraPosition) = GetTransformPositionsForLaneSplitting(3, direction);

        else if (transform.position.x < 2.5)
            (finalPosition, finalCameraPosition) = GetTransformPositionsForLaneSplitting(4, direction);

        else if (transform.position.x < 5.5)
            (finalPosition, finalCameraPosition) = GetTransformPositionsForLaneSplitting(5, direction);

        else if (transform.position.x < 8.5)
            (finalPosition, finalCameraPosition) = GetTransformPositionsForLaneSplitting(6, direction);

        else
            (finalPosition, finalCameraPosition) = GetTransformPositionsForLaneSplitting(7, direction);

        // Initial position and rotation of car mesh.
        Vector3 initialCarPosition = carObject.transform.localPosition;
        Quaternion initialCarRotation = carObject.transform.localRotation;

        // Final position and rotation of car mesh.
        Vector3 finalCarPosition = new(initialCarPosition.x, currentCar.laneSplitHeight, initialCarPosition.z);
        Quaternion finalCarRotation = Quaternion.Euler(carObject.transform.localEulerAngles.x, carObject.transform.localEulerAngles.y, direction == 0 ? 79 : -79);

        float elapsedTime = 0;
        float lastPosition = initialPosition;
        float lastCameraPosition = initialCameraPosition;

        // Rotate car mesh & move transforms to lane marker.
        while (elapsedTime < speed)
        {
            float fraction = EaseOutCubic(elapsedTime / speed);

            float newPosition = Mathf.Lerp(initialPosition, finalPosition, fraction);
            float newCameraPosition = Mathf.Lerp(initialCameraPosition, finalCameraPosition, fraction);

            float deltaPosition = newPosition - lastPosition;
            float deltaCameraPosition = newCameraPosition - lastCameraPosition;

            Vector3 modifiedTransformDelta = new(deltaPosition, 0, 0);
            Vector3 modifiedCameraDelta = new(deltaCameraPosition, 0, 0);

            transform.position += modifiedTransformDelta;
            cameraObject.transform.position += modifiedCameraDelta;

            if (cameraType == 0)
            {
                cameraObject.transform.rotation = Quaternion.Lerp(initialCameraRotation, finalCarRotation, fraction);
            }

            carObject.transform.SetLocalPositionAndRotation
            (
                Vector3.Lerp(initialCarPosition, finalCarPosition, fraction),
                Quaternion.Lerp(initialCarRotation, finalCarRotation, fraction)
            );

            lastPosition = newPosition;
            lastCameraPosition = newCameraPosition;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        carObject.transform.SetLocalPositionAndRotation(finalCarPosition, finalCarRotation);

        leftSteamEffect.SetActive(false);
        rightSteamEffect.SetActive(false);

        // Stay rotated for a specific amount of time.
        yield return new WaitForSeconds(laneSplitTime - speed * 2);

        // Rotate down.
        elapsedTime = 0;
        while (elapsedTime < speed)
        {
            float fraction = EaseInCubic(elapsedTime / speed);

            carObject.transform.SetLocalPositionAndRotation
            (
                Vector3.Lerp(finalCarPosition, initialCarPosition, fraction),
                Quaternion.Lerp(finalCarRotation, initialCarRotation, fraction)
            );
            if (cameraType == 0)
            {
                cameraObject.transform.rotation = Quaternion.Lerp(finalCarRotation, initialCameraRotation, fraction);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Stop the steam effect.
        leftSteam.SetActive(false);
        rightSteam.SetActive(false);
        firstPersonSteamSource.SetActive(false);

        carObject.transform.SetLocalPositionAndRotation(initialCarPosition, initialCarRotation); // Make sure we get back to the original position and rotation exactly.
        lastLaneSplitTime = (Time.time - startTime);
        currentlyLaneSplitting = false;
        
        uiManager.BeginLaneSplitCooldown(); // Begin the cooldown countdown which is handled by the UIManager script.

        // Display the spark effect.
        sparks.SetActive(true);

        SetCurrentLane(currentLane, speed / 10);

        // Bounce up slightly after coming down.
        Vector3 bounceUpPosition = initialCarPosition + new Vector3(0, 0.09f, 0); // Increase the y-value to bounce up.
        Quaternion bounceUpRotation = Quaternion.Euler(carObject.transform.localEulerAngles.x, carObject.transform.localEulerAngles.y, direction == 0 ? 15f : -15f); // Increase the rotation value to bounce up.

        // Move to bounce up position.
        elapsedTime = 0;
        float bounceSpeed = speed * 1.2f;
        while (elapsedTime < bounceSpeed)
        {
            float fraction = EaseOutCubic(elapsedTime / bounceSpeed);

            carObject.transform.SetLocalPositionAndRotation
            (
                Vector3.Lerp(initialCarPosition, bounceUpPosition, fraction),
                Quaternion.Lerp(initialCarRotation, bounceUpRotation, fraction)
            );
            if (cameraType == 0)
            {
                cameraObject.transform.rotation = Quaternion.Lerp(initialCameraRotation, bounceUpRotation, fraction);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        carObject.transform.SetLocalPositionAndRotation(bounceUpPosition, bounceUpRotation); // Make sure the car reaches the bounce up position and rotation exactly.

        // Rotate down from bounce up position.
        elapsedTime = 0;
        while (elapsedTime < bounceSpeed)
        {
            float fraction = EaseInCubic(elapsedTime / bounceSpeed);

            carObject.transform.SetLocalPositionAndRotation
            (
                Vector3.Lerp(bounceUpPosition, initialCarPosition, fraction),
                Quaternion.Lerp(bounceUpRotation, initialCarRotation, fraction)
            );
            if (cameraType == 0)
            {
                cameraObject.transform.rotation = Quaternion.Lerp(bounceUpRotation, initialCameraRotation, fraction);
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        carObject.transform.SetLocalPositionAndRotation(initialCarPosition, initialCarRotation); // Make sure we get back to the original position and rotation exactly.
        yield return new WaitForSeconds(1f);
        sparks.SetActive(false);

        // Mark coroutine as finished
        laneSplitCoroutine = null;
    }


    /*--------------------------------- CAMERA MANIPULATION FUNCTIONS ---------------------------------*/
    private IEnumerator CameraJolt()
    {
        // Define the jolt direction in the X, Y, and Z axes.
        Vector3 joltDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, -0.5f), Random.Range(0.5f, 1f)).normalized;
        joltDirection.z = -Mathf.Abs(joltDirection.z); // Making sure Z always pushes forward.

        float joltMagnitude = 4f + accel;

        // Apply the jolt.
        Vector3 joltVector = joltDirection * joltMagnitude;
        Vector3 joltPosition = cam.transform.localPosition + joltVector;
        joltPosition.y = Mathf.Max(joltPosition.y, -1f);
        joltPosition.x = Mathf.Clamp(joltPosition.x, - 1f, 1f);
        cam.transform.localPosition = joltPosition;


        // Duration of the return animation.
        float duration = 0.15f;

        // Initial position after applying the jolt.
        Vector3 initialLocalPosition = cam.transform.localPosition;

        // Animate the camera back to its original local position.
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float factor = t / duration;
            Vector3 newPosition = Vector3.Lerp(initialLocalPosition, Vector3.zero, factor);
            cam.transform.localPosition = newPosition;
            yield return null;
        }

        // Ensure the camera ends up exactly in its original local position.
        cam.transform.localPosition = Vector3.zero;
    }

    private IEnumerator LerpCameraFOV(float startFov, float targetFov, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Lerp Field Of View.
            cam.fieldOfView = Mathf.Lerp(startFov, targetFov, t);

            // Calculate the desired Z position based on the lerped FOV.
            if (!bullet)
            {
                //float fov = cam.fieldOfView;
                //float adjustmentFactor = Mathf.Lerp(0, 0.4f, Mathf.InverseLerp(136f, 35f, fov));
                float targetZ = this.transform.position.z + 0.0150065f * cam.fieldOfView + currentCar.defaultCameraPosition.z + 0.9174f;

                // Adjust Z position towards the target position.
                float deltaZ = targetZ - cameraObject.transform.position.z;
                cameraObject.transform.position += new Vector3(0, 0, deltaZ);
            }

            yield return null;
        }

        cam.fieldOfView = targetFov;
    }


    /*---------------------------------- POWERUP MANAGEMENT FUNCTIONS ---------------------------------*/
    private IEnumerator DisableAllPowerups()
    {
        // Disable aggro effects.
        if (aggro)
        {
            if (!soundManager.engineScreamPlayed) soundManager.enginesounds[1].Play();
            if (soundManager.originalPitches.ContainsKey(soundManager.beepsource))
            {
                soundManager.beepsource.pitch = soundManager.originalPitches[soundManager.beepsource];
            }
            soundManager.ToggleAggroEffects();
            StartCoroutine(LerpCameraFOV(cam.fieldOfView, oldFov, 0.25f));
            StartCoroutine(postProcessManager.LerpMotionBlur(postProcessManager.motionBlur.intensity.value, oldMotionBlur, 0.25f));
            postProcessManager.vignette.intensity.value = oldVignette;
            if (!SaveManager.Instance.SaveData.VignetteEnabled) postProcessManager.vignette.active = false;
        }
        aggro = false;
        
        // Disable tornado effects.
        if (tornado)
        {
            tornadoExplodeCars = true;
            inTornadoExplosion = true;
            prefabManager.maxCarsPerLane = 4;
            shakeIntensity -= TORNADO_CAMERA_SHAKE;
            explosionShakeIntensity = 100;
            StartCoroutine(CameraJolt());
            tornadoObject.SetActive(false);
        }
        tornado = false;
        
        // Disable bullet effects.
        if (bullet)
        {
            float movement_val = Time.deltaTime * accel / 2;
            inBulletExplosion = true;
            if (!soundManager.engineScreamPlayed) soundManager.enginesounds[1].Play();
            soundManager.ToggleBulletSound(accel);
            accel = oldAccel;
            shakeIntensity -= BULLET_CAMERA_SHAKE;
            explosionShakeIntensity = 50;
            StartCoroutine(LerpCameraFOV(cam.fieldOfView, oldFov, 0.1f));
        }
        bullet = false;

        timeSinceLastPowerup = Time.time - startTime;

        // Wait 15 seconds before spawning a new powerup.
        yield return new WaitForSeconds(15);

        prefabManager.powerUpAllowedToSpawn = true;
    }

    IEnumerator MoveTornado()
    {
        float elapsedTime = 0; // Track the total time the tornado has been moving.

        while (elapsedTime < 15)
        {
            float moveSpeed = 5; // Calculated to ensure it completes movement in desired time.

            if (isTornadoMovingRight)
            {
                tornadoObject.transform.Translate(Vector3.right * moveSpeed * Time.deltaTime, Space.World);

                if (tornadoObject.transform.position.x >= 11.5f)
                {
                    isTornadoMovingRight = false;
                }
            }
            else
            {
                tornadoObject.transform.Translate(Vector3.left * moveSpeed * Time.deltaTime, Space.World);

                if (tornadoObject.transform.position.x <= -13.5f)
                {
                    isTornadoMovingRight = true;
                }
            }

            elapsedTime += Time.deltaTime; // Increment elapsed time.
            yield return null;
        }
    }


    /*----------------------------------------- OTHER FUNCTIONS ---------------------------------------*/
    // Handle collisions with traffic cars & powerups.
    private void OnTriggerEnter(Collider other)
    {

        if (invincible || isRecovering || tornado || bullet || other.CompareTag("TrafficBoundingBox")) return;
        if (other.gameObject.name.StartsWith("Powerup") && powerupsOnStandby) // Checks if we have collided with a powerup.
        {
            powerupCountdown = Time.time - startTime;
            powerupsOnStandby = false;
            prefabManager.powerUpAllowedToSpawn = false;

            if (other.gameObject.name.EndsWith("0")) // We have collided with the 'Lives' powerup.
            {
                numlives += 1;
                soundManager.PlayLivesPowerupSound();
            }

            else if (other.gameObject.name.EndsWith("1")) // We have collided with the 'Aggro' powerup.
            {
                aggro = true;
                powerupCountdown += 15;
                accelTimeOffset += 15;
                if (!soundManager.engineScreamPlayed) soundManager.enginesounds[1].Pause();
                soundManager.PlayAggro(false);
                soundManager.ToggleAggroEffects();
                oldVignette = postProcessManager.vignette.intensity.value;
                postProcessManager.vignette.intensity.value = 0.4f;
                if (!SaveManager.Instance.SaveData.VignetteEnabled) postProcessManager.vignette.active = true;
                StartCoroutine(postProcessManager.ColorScreen(Color.red, 15f));
                oldFov = cam.fieldOfView;
                StartCoroutine(LerpCameraFOV(cam.fieldOfView, 45, 0.25f));
                oldMotionBlur = postProcessManager.motionBlur.intensity.value;
                StartCoroutine(postProcessManager.LerpMotionBlur(postProcessManager.motionBlur.intensity.value, 0, 0.25f));
            }

            else if (other.gameObject.name.EndsWith("2")) // We have collided with the 'Tornado' powerup.
            {
                tornado = true;
                powerupCountdown += 15;
                StartCoroutine(postProcessManager.FlashScreen());
                StartCoroutine(postProcessManager.ColorScreen(Color.white, 15f));
                prefabManager.maxCarsPerLane = 5;
                tornadoObject.transform.position = new(0, 2.5f, transform.position.z + 30);
                StartCoroutine(MoveTornado());
                shakeIntensity += TORNADO_CAMERA_SHAKE;
                tornadoObject.SetActive(true);
            }

            else if (other.gameObject.name.EndsWith("3")) // We have collided with the 'Bullet' powerup.
            {
                bullet = true;
                uiManager.speedo.text = "ERROR";
                if (uiManager.bulletFlashCounter % 2 != 0) uiManager.bulletFlashCounter += 1;
                if (!soundManager.engineScreamPlayed) soundManager.enginesounds[1].Pause();
                soundManager.ToggleBulletSound(accel);
                powerupCountdown += 5;
                accelTimeOffset += 5;
                oldAccel = accel;
                oldFov = cam.fieldOfView;
                shakeIntensity += BULLET_CAMERA_SHAKE;
                accel *= 3;
                StartCoroutine(LerpCameraFOV(cam.fieldOfView * 1.1f, 149, 5f));
                StartCoroutine(postProcessManager.ColorScreen(Color.blue, 5f));
            }

            return;
        }

        if (aggro && !currentlyLaneSplitting) // If we are in aggro, launch the traffic car.
        {
            soundManager.PlayCrash();
            StartCoroutine(CameraJolt());
            StartCoroutine(prefabManager.LaunchTraffic(other.transform));
        }

        else if (numlives > 0 && !currentlyLaneSplitting && !bullet) // Handle traffic collisions.
        {
            if ((Time.time - startTime) - timeSinceLastPowerup > 2)
                numlives--;
            inTrafficExplosion = true;
            StartCoroutine(prefabManager.ExplodeTraffic(explosionParent.transform, transform, false));
            other.gameObject.SetActive(false);
            explosionShakeIntensity = 100f * accel;
        }
                
        else if (!currentlyLaneSplitting && !bullet && !tornado && !gameEnd && !invincible) // Handle final collision.
        {
            inTrafficExplosion = true;
            StartCoroutine(prefabManager.ExplodeTraffic(explosionParent.transform, transform, false));
            other.gameObject.SetActive(false);

            // There is a 2 second post powerup invincibility, end the game if it has been over 2 seconds since the last powerup ended.
            if (((Time.time - startTime) - timeSinceLastPowerup > 2))
            {
                // Ensure lane split coroutine is stopped if it is still running
                if (laneSplitCoroutine != null)
                {
                    StopCoroutine(laneSplitCoroutine);
                    laneSplitCoroutine = null;
                }

                // Update state variables.
                gameEnd = true;
                inTrafficExplosion = false;
                inTornadoExplosion = false;
                inBulletExplosion = false;

                // Increase explosion intensity.
                shakeIntensity = 1.25f * accel;
                explosionShakeIntensity = 100f;

                // Stop player car, erase player car & car collided with, play the explosion animation.
                accel = 0;
                carObject.SetActive(false);
                explosion.Play();

                // UI
                pauseButton.SetActive(false);

                // Make the rain appear verticle.
                Vector3 currentRainPosition = rain.transform.position;
                Quaternion currentRainRotation = rain.transform.rotation;
                currentRainPosition.y = 20f;
                currentRainPosition.z += -0.038f * cam.fieldOfView - 12f;
                currentRainRotation *= Quaternion.Euler(-60, 0, 0);
                rain.transform.SetPositionAndRotation(currentRainPosition, currentRainRotation);
            }
        }
    }

    public void RecoverAndRestart()
    {
        // Reset explosion/collision state
        inTrafficExplosion = false;
        inTornadoExplosion = false;
        inBulletExplosion = false;
        gameEnd = false;

        // Reset camera & motion/shake state
        cameraFovLerped = false;
        explosionShakeIntensity = 1f;
        shakeIntensity = 0.05f;
        inSidewaysJolt = false;
        whichWay = 0;

        // Reset powerups
        aggro = false;
        tornado = false;
        bullet = false;
        tornadoExplodeCars = false;
        tornadoObject.SetActive(false);

        // Reset audio side-effects (aggro, bullet, wind, engine flags)
        soundManager.ResetAudioOnRecovery();

        // Reset car & lives
        accel = 0.5f;
        accelTimeOffset = 0f;
        transform.rotation = defaultRot;
        carObject.transform.rotation = Quaternion.identity;
        carObject.SetActive(true);
        soundManager.PlayEngineSound();   // engine persists through flashing, destroy, respawn
        if (!invincible) numlives = currentCar.numlives;

        // UI
        pauseButton.SetActive(true);

        // Restore rain orientation
        rain.transform.SetPositionAndRotation(rainDefaultPosition, rainDefaultRotation);

        // Reset countdown/race timing
        startTime = Time.time - 3;
        lastLaneSplitTime = 1; // Set to 1 for lane splitting to be enabled upon exit of recovery animation
        timeSinceLastPowerup = 0;

        // Start car flashing animation
        StartCoroutine(RecoverFlashAndGhost(recoverDuration, startFlashInterval, endFlashInterval));
    }

    private IEnumerator RecoverFlashAndGhost(float totalDuration, float intervalStart, float intervalEnd)
    {
        isRecovering = true;

        // Ensure the visual starts visible
        if (!carObject.activeSelf) carObject.SetActive(true);

        float elapsed = 0f;
        bool visible = true;

        // Flip repeatedly; the wait interval decreases linearly from start -> end
        while (elapsed < totalDuration)
        {
            // Compute current interval based on progress (higher frequency as time passes)
            float t = Mathf.Clamp01(elapsed / totalDuration);
            float currentInterval = Mathf.Lerp(intervalStart, intervalEnd, t);

            // Toggle visibility of the rendered car only (not the controller/root)
            visible = !visible;
            carObject.SetActive(visible);

            // Wait, advance time
            yield return new WaitForSeconds(currentInterval);
            elapsed += currentInterval;
        }

        // Guarantee we end visible and exit recovering state
        if (!carObject.activeSelf) carObject.SetActive(true);
        isRecovering = false;

        Destroy(carObject);
        SetUpCar();

        // Rebind and re-register lane split sounds as a new car object has been spawned
        soundManager.SetUpLaneSplitSounds(true);
    }

    // --- add inside PlayerController (e.g., under fields or at the end of the class) ---
    static bool IsMetallic(Material m)
    {
        if (!m) return false;

        // Slider-based metallic
        float slider = m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f;
        bool sliderSaysMetal = slider >= 0.9f;     // your metals are set to 1, non-metal ≈ 0.304

        // Texture-based metallic (keyword + texture)
        bool hasMetalMap =
            m.HasProperty("_MetallicGlossMap") &&
            m.GetTexture("_MetallicGlossMap") != null &&
            m.IsKeywordEnabled("_METALLICSPECGLOSSMAP");

        return sliderSaysMetal || hasMetalMap;
    }

    static void BoostBrightnessIfMetallic(Material m, float factor)
    {
        if (!m || !m.HasProperty("_Brightness")) return;
        if (IsMetallic(m))
            m.SetFloat("_Brightness", m.GetFloat("_Brightness") * factor);
    }

    private float EaseOutCubic(float x)
    {
        return 1 - Mathf.Pow(1 - x, 3);
    }

    private float EaseInCubic(float x)
    {
        return Mathf.Pow(x, 3);
    }

}
