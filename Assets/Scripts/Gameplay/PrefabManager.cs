using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabManager : MonoBehaviour
{
    // Road gameObject.
    public GameObject[] roads = new GameObject[4];

    // Traffic spawning classes.
    [System.Serializable]
    public class BaseTrafficList
    {
        public List<GameObject> trafficVariations;
    }

    [System.Serializable]
    public class TrafficList : BaseTrafficList { }  // This hack allows the array of lists to show up in the inspector window.

    // Array of lists for spawning lowpoly traffic.
    [System.Serializable]
    public class TrafficListLowPoly : BaseTrafficList { } // This hack allows the array of lists to show up in the inspector window.

    public TrafficList[] trafficPrefabs = new TrafficList[7];
    public TrafficListLowPoly[] trafficPrefabsLowPoly = new TrafficListLowPoly[7];

    // Array of lists for spawning scifi buildings. (City 77)
    [System.Serializable]
    public class BuildingList
    {
        public List<GameObject> buildingRotations;
    }
    public BuildingList[] leftBuildingPrefabs = new BuildingList[12];
    public BuildingList[] rightBuildingPrefabs = new BuildingList[12];

    // Array of lists for spawning wasteland environment objects. (Wasteland)
    [System.Serializable]
    public class WastelandBuildingList
    {
        public List<WastelandBuilding> buildingTypes;
    }
    public WastelandBuildingList[] leftWastelandBuildings = new WastelandBuildingList[2];
    public WastelandBuildingList[] rightWastelandBuildings = new WastelandBuildingList[2];

    public List<GameObject> leftRocks;
    public List<GameObject> rightRocks;

    public List<GameObject> leftGrass;
    public List<GameObject> rightGrass;

    public GameObject atlantis;

    // List for spawning space tunnels. (Transgalactic Highway)
    public List<GameObject> spaceTunnels;

    // Array to randomly select traffic vehicles. 0 = bigrig, 1 = boxtruck, 2 = compact_car, 3 = flying_car, 4 = hatchback, 5 = sports_car, 6 = urban_pod.
    readonly int[] vehicle_ids = new int[] { 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 4, 4, 4, 4, 5, 5, 6, 6, 6 };

    // Array for returning active traffic vehicles back to the object pool in the correct lane (x) position.
    readonly float[] lane_positions = new float[] { 10, 7, 4, 1, -2.5f, -5.5f, -8.5f, -11.5f };

    // Array for returning active traffic vehicles to their Y position (in case they get aggro'd).
    readonly float[] original_traffic_y_positions = new float[] { 1.77f, 1.756f, 1.813f, 2.038f, 1.778f, 2.272f, 1.787f };

    // Array of tuples that dictates the maximum / minimum left & right X spawn position for buildings to prevent buildings from spawning inside the road.
    readonly System.Tuple<float, float>[,] building_x_positions = new System.Tuple<float, float>[12, 4]
    {
        // Rotation 1                                      // Rotation 2                                    // Rotation 3                                    // Rotation 4
        { new System.Tuple<float, float>(-124.5f, 139.3f), new System.Tuple<float, float>(-131.6f, 129.9f), new System.Tuple<float, float>(-139f, 98.5f),    new System.Tuple<float, float>(-97.5f, 115.6f)  }, // B001
        { new System.Tuple<float, float>(-88.5f, 82.9f),   new System.Tuple<float, float>(-147.8f, 87f),    new System.Tuple<float, float>(-85.1f, 70.8f),   new System.Tuple<float, float>(-71.2f, 145f)    }, // B002
        { new System.Tuple<float, float>(-114.5f, 97.5f),  new System.Tuple<float, float>(-114.4f, 109.7f), new System.Tuple<float, float>(-94.5f, 115.4f),  new System.Tuple<float, float>(-116.7f, 101.9f) }, // B003
        { new System.Tuple<float, float>(-119.8f, 79.3f),  new System.Tuple<float, float>(-131.5f, 119.3f), new System.Tuple<float, float>(-75.5f, 86.8f),   new System.Tuple<float, float>(-93.2f, 131.4f)  }, // B004
        { new System.Tuple<float, float>(-85.6f, 133.2f),  new System.Tuple<float, float>(-115.6f, 91.6f),  new System.Tuple<float, float>(-133.3f, 128.5f), new System.Tuple<float, float>(-130.5f, 112.1f) }, // B005
        { new System.Tuple<float, float>(-152.8f, 207.8f), new System.Tuple<float, float>(-88.4f, 148.5f),  new System.Tuple<float, float>(-133.3f, 130.1f), new System.Tuple<float, float>(-131.8f, 93.4f)  }, // B006
        { new System.Tuple<float, float>(-149.9f, 103.3f), new System.Tuple<float, float>(-140.5f, 124.6f), new System.Tuple<float, float>(-105.8f, 97.3f),  new System.Tuple<float, float>(-101f, 137.6f)   }, // B007
        { new System.Tuple<float, float>(-124.4f, 131.3f), new System.Tuple<float, float>(-128.9f, 129.7f), new System.Tuple<float, float>(-130.4f, 110.5f), new System.Tuple<float, float>(-109.4f, 126f)   }, // B008
        { new System.Tuple<float, float>(-120.8f, 122.6f), new System.Tuple<float, float>(-129.4f, 115.7f), new System.Tuple<float, float>(-124, 115.2f),    new System.Tuple<float, float>(-113.6f, 120.5f) }, // B009
        { new System.Tuple<float, float>(-115.5f, 178.2f), new System.Tuple<float, float>(-101.4f, 108.3f), new System.Tuple<float, float>(-153.3f, 96.3f),  new System.Tuple<float, float>(-94.6f, 99.4f)   }, // B010
        { new System.Tuple<float, float>(-133.9f, 135.5f), new System.Tuple<float, float>(-119.6f, 142.9f), new System.Tuple<float, float>(-137.1f, 103.3f), new System.Tuple<float, float>(-104.3f, 113.9f) }, // B011
        { new System.Tuple<float, float>(-128.3f, 122.1f), new System.Tuple<float, float>(-196.8f, 122.8f), new System.Tuple<float, float>(-120.8f, 106.7f), new System.Tuple<float, float>(-108.3f, 195.5f) }  // B012
    };

    // Array and vars to manage traffic explosions.
    public ParticleSystem[] trafficExplosions = new ParticleSystem[5];
    public Transform trafficExplosionParent;
    int trafficExplosionIndex = 0;

    // Array and vars to manage aggro explosions. (Different colour and size from traffic explosions)
    public ParticleSystem[] aggroExplosions = new ParticleSystem[5];
    int aggroExplosionIndex = 0;

    // Array and vars to manage powerups.
    readonly int[] powerup_probabilities = new int[] { 0, 0, 1, 2, 3 };
    public ParticleSystem[] powerups = new ParticleSystem[4];
    public float powerUpSpawnTime;
    public bool powerUpAllowedToSpawn = true;

    // Vars for spawning roads & environment objects.
    public float roadLength = 99.9f;
    public float spaceTunnelLength = 540f;
    public int maxRoads = 5;
    public int maxBuildingPairs = 15;
    public int maxSpaceTunnels = 2;
    public int maxUnderwaterTunnels = 12;
    public int maxRocksPerSide = 5;
    public int maxGrassPerSide = 100;
    private float scifiPosteriorObjectMoveAmount = 3000f;
    private float wastelandPosteriorObjectMoveAmount = 1000f;
    private float spacePosteriorObjectMoveAmount = 540f * 3;
    private float underwaterPosteriorObjectMoveAmount = 400f;
    private int leftBuildingIndex;
    private int rightBuildingIndex;

    //  Vars for spawning traffic.
    public int maxCarsPerLane = 4;
    public float trafficDensity = 2;
    private float originalTrafficDensity;
    private bool useLowPolyTraffic = false;
    public Transform[] originalParent;

    // Vars for the tornado.
    private float attractionSpeed = 1f; // Speed at which cars are attracted to the tornado.
    private readonly float swirlRadius = 3f;
    Vector3 targetPosition = new(0, 3, 0); // The (local) postion inside the tornado that the cars get attracted to.
    private float maxSwirlTime = 2.0f; // The time it takes to do one loop around the tornado.
    private float elapsedSwirlTime = 0f;
    private float startSwirlSpeed = 0;  // The initial speed at which cars will swirl around the tornado.
    private float endSwirlSpeed = 100f;

    // List to keep track of active roads.
    private List<GameObject> activeRoads = new List<GameObject>();

    // Lists to keep track of active scifi buildings.
    private List<GameObject> activeLeftBuildings = new List<GameObject>();
    private List<GameObject> activeRightBuildings = new List<GameObject>();

    // Lists to keep track of active wasteland buildings.
    private List<WastelandBuilding> activeLeftWastelandBuildings = new List<WastelandBuilding>();
    private List<WastelandBuilding> activeRightWastelandBuildings = new List<WastelandBuilding>();

    // Lists to keep track of active rocks.
    private List<GameObject> activeLeftRocks = new List<GameObject>();
    private List<GameObject> activeRightRocks = new List<GameObject>();

    // Lists to keep track of active grass.
    private List<GameObject> activeLeftGrass = new List<GameObject>();
    private List<GameObject> activeRightGrass = new List<GameObject>();

    // Lists to keep track of active space tunnels.
    private List<GameObject> activeSpaceTunnels = new List<GameObject>();

    // Lists to keep track of active traffic & traffic that is stuck in the tornado.
    private List<List<GameObject>> allLaneTraffic = new List<List<GameObject>>();
    private List<GameObject> tornadoTraffic = new List<GameObject>();

    // Lists to keep track of oncoming traffic.
    private List<GameObject> lane0traffic = new List<GameObject>();
    private List<GameObject> lane1traffic = new List<GameObject>();
    private List<GameObject> lane2traffic = new List<GameObject>();
    private List<GameObject> lane3traffic = new List<GameObject>();

    // Lists to keep track of sameway traffic.
    private List<GameObject> lane4traffic = new List<GameObject>();
    private List<GameObject> lane5traffic = new List<GameObject>();
    private List<GameObject> lane6traffic = new List<GameObject>();
    private List<GameObject> lane7traffic = new List<GameObject>();

    // System variables.
    private float isGameEnded = 1; // 1 = game not ended, -1 = game ended.
    private bool gameEndSet = false;
    private float playerPosZ;
    private float startTime;
    private PlayerController playerController;
    private SoundManager soundManager;
    private GameObject leftCar;
    private GameObject rightCar;

    // FOR TRAILER ONLY
    //public bool dontDespawn;

    private int currentEnvironment;
    private int currentWastelandBuildingType = 0;
    private int wasteLandBuildingCount = 0;
    private int wasteLandSkyscraperCount = 0;
    void Start()
    {
        // Get current environment.
        currentEnvironment = SaveManager.Instance.SaveData.CurrentEnvironment;

        // Start the clock.
        startTime = Time.time;

        // Find 'PlayerController' script.
        GameObject PlayerCarObject = GameObject.Find("PlayerCar");
        playerController= PlayerCarObject.GetComponent<PlayerController>();

        // Find 'SoundManager' script.
        GameObject SoundManagerObject = GameObject.Find("SoundManager");
        soundManager = SoundManagerObject.GetComponent<SoundManager>();

        // Set the power up spawn time.
        powerUpSpawnTime = Random.Range(15, 30);

        // Set low poly traffic state flag
        useLowPolyTraffic = SaveManager.Instance.SaveData.UseLowPolyTraffic;

        // Intialize list.
        allLaneTraffic.Add(lane0traffic);
        allLaneTraffic.Add(lane1traffic);
        allLaneTraffic.Add(lane2traffic);
        allLaneTraffic.Add(lane3traffic);
        allLaneTraffic.Add(lane4traffic);
        allLaneTraffic.Add(lane5traffic);
        allLaneTraffic.Add(lane6traffic);
        allLaneTraffic.Add(lane7traffic);

        // Spawn initial roads.
        if (currentEnvironment == 2) maxRoads *= 2;
        for (int i = 0; i < maxRoads; i++) SpawnRoad();

        // Spawn initial environment objects.
        switch (currentEnvironment)
        {
            case 0: // Spawn sci fi buildings (City 77)
                leftBuildingIndex = Random.Range(0, 12);
                rightBuildingIndex = (leftBuildingIndex + 6) % 12;
                for (int i = 0; i < maxBuildingPairs; i++)
                {
                    SpawnSciFiBuilding(1);
                    SpawnSciFiBuilding(0);
                }
                break;
            case 1: // Spawn wasteland buildings (Nuclear Wasteland)
                leftBuildingIndex = Random.Range(0, leftWastelandBuildings[currentWastelandBuildingType].buildingTypes.Count);
                rightBuildingIndex = (leftBuildingIndex + 16) % rightWastelandBuildings[currentWastelandBuildingType].buildingTypes.Count;
                for (int i = 0; i < maxBuildingPairs; i++)
                {
                    SpawnWastelandBuilding(1);
                    SpawnWastelandBuilding(0);
                }
                break;
            case 2:
                for (int i = 0; i < maxSpaceTunnels; i++) SpawnSpaceTunnel();
                break;
            case 3:
                for (int i = 0; i < maxRocksPerSide; i++) SpawnRock(0);
                for (int i = 0; i < maxRocksPerSide; i++) SpawnRock(1);
                for (int i = 0; i < maxGrassPerSide; i++) SpawnGrass(0);
                for (int i = 0; i < maxGrassPerSide; i++) SpawnGrass(1);
                break;
            default:
                break;
        }

        // Spawn temporary environment objects behind the player (Used for the reversed camera view at the beginning of the pre-race cinematic).
        SpawnPosteriorRoads();
        CreatePosteriorObjects();

        // Spawn initial traffic cars.
        for (int i = 0; i < maxCarsPerLane; i++)
        {
            SpawnTraffic(0);
            SpawnTraffic(1);
            SpawnTraffic(2);
            SpawnTraffic(3);
            SpawnTraffic(4);
            SpawnTraffic(5);
            SpawnTraffic(6);
            SpawnTraffic(7);
        }
        PutTrafficBehindPlayer();

        trafficDensity = Mathf.Max(1f, SaveManager.Instance.SaveData.TrafficDensity * 0.75f);
        if (trafficDensity < 0)
        {
            trafficDensity = 3;
            SaveManager.Instance.SaveData.TrafficDensity = 3;
            SaveManager.Instance.SaveGame();
        }
        originalTrafficDensity = trafficDensity;
    }

    void Update()
    {
        if (trafficDensity > 1)
        {
            trafficDensity -= (Time.deltaTime * originalTrafficDensity) / 1000;
        }
        
        // Update vars for traffic car movement, current player position, and tornado force.
        float adjustedSpeed = 25f * Time.deltaTime;
        playerPosZ = playerController.carObject.transform.position.z;
        attractionSpeed = 25 * playerController.accel;
        startSwirlSpeed = 50f * playerController.accel;
        endSwirlSpeed = 300f * playerController.accel;

        // Move the cars.
        if (!playerController.tornado)
        {
            for (int i = 0; i < lane0traffic.Count; i++)
                lane0traffic[i].transform.position += lane0traffic[i].transform.forward * adjustedSpeed;

            for (int i = 0; i < lane1traffic.Count; i++)
                lane1traffic[i].transform.position += lane1traffic[i].transform.forward * adjustedSpeed;

            for (int i = 0; i < lane2traffic.Count; i++)
                lane2traffic[i].transform.position += lane2traffic[i].transform.forward * adjustedSpeed;

            for (int i = 0; i < lane3traffic.Count; i++)
                lane3traffic[i].transform.position += lane3traffic[i].transform.forward * adjustedSpeed;

            for (int i = 0; i < lane4traffic.Count; i++)
                lane4traffic[i].transform.position += lane4traffic[i].transform.forward * adjustedSpeed;

            for (int i = 0; i < lane5traffic.Count; i++)
                lane5traffic[i].transform.position += lane5traffic[i].transform.forward * adjustedSpeed;

            for (int i = 0; i < lane6traffic.Count; i++)
                lane6traffic[i].transform.position += lane6traffic[i].transform.forward * adjustedSpeed;

            for (int i = 0; i < lane7traffic.Count; i++)
                lane7traffic[i].transform.position += lane7traffic[i].transform.forward * adjustedSpeed;
        }

        // If the tornado is active, attract and swirl the cars around the tornado.
        if (playerController.tornado || (playerController.tornadoExplodeCars && tornadoTraffic.Count > 0))
        {
            for (int laneIndex = 0; laneIndex < allLaneTraffic.Count; laneIndex++)
            {
                for (int carIndex = 0; carIndex < allLaneTraffic[laneIndex].Count; carIndex++)
                {
                    GameObject car = allLaneTraffic[laneIndex][carIndex];
                    if (car.transform.position.z < playerController.tornadoObject.transform.position.z + 10)
                    {
                        if (playerController.tornadoExplodeCars) // Explode the traffic in the tornado if this bool is true.
                        {
                            StartCoroutine(ExplodeTraffic(trafficExplosionParent, car.transform, true));
                            car.SetActive(false);
                            if (useLowPolyTraffic) car.transform.SetParent(originalParent[1]);
                            else car.transform.SetParent(originalParent[0]);
                            tornadoTraffic.Remove(car);
                            ReturnTraffic(laneIndex);
                        }
                        else if (playerController.tornado)
                        {
                            if (car.transform.parent != playerController.tornadoObject.transform)
                            {
                                ReplaceTrafficParent(car);
                                if (!tornadoTraffic.Contains(car))
                                {
                                    tornadoTraffic.Add(car);
                                }
                            }
                            float distanceToTarget = Mathf.Abs(car.transform.localPosition.x);

                            // If car is outside the swirl radius, attract it to the tornado.
                            if (distanceToTarget > swirlRadius)
                            {
                                // Lerp the car towards the target position.
                                Vector3 directionToTarget = (targetPosition - car.transform.localPosition).normalized;
                                car.transform.localPosition += attractionSpeed * Time.deltaTime * directionToTarget;

                                // Orient the car to face the tornado's center.
                                Vector3 directionToTornado = (playerController.tornadoObject.transform.position - car.transform.position).normalized;
                                Quaternion lookAtTornado = Quaternion.LookRotation(directionToTornado);
                                Quaternion targetRotation = lookAtTornado * Quaternion.Euler(0, 90, 0);  // Look at tornado and then add 90-degree offset

                                // Lerp rotation based on carSpeed and the inverse of the distance.
                                float rotationLerpFactor = attractionSpeed * Time.deltaTime;
                                car.transform.rotation = Quaternion.Lerp(car.transform.rotation, targetRotation, rotationLerpFactor);
                            }

                            // If car is within swirl radius, begin swirling it.
                            else
                            {
                                elapsedSwirlTime += Time.deltaTime;

                                float t = elapsedSwirlTime / maxSwirlTime;  // Normalize the time.
                                float currentSwirlSpeed = Mathf.Lerp(startSwirlSpeed, endSwirlSpeed, EaseInCubic(t));

                                // Move in a circular path around the tornado's center.
                                car.transform.RotateAround(playerController.tornadoObject.transform.position, Vector3.up, currentSwirlSpeed * Time.deltaTime);

                                // Move the car upwards until it reaches a Y height of 21.
                                if (car.transform.localPosition.y < 21)
                                {
                                    float upwardMovement = attractionSpeed * Time.deltaTime * 0.25f;
                                    car.transform.localPosition += new Vector3(0, upwardMovement, 0);
                                }
                                // Once the car is higher than 21 units, disable and despawn it.
                                else
                                {
                                    car.SetActive(false);
                                    tornadoTraffic.Remove(car);
                                    ReturnTraffic(laneIndex);
                                }
                            }
                        }
                    }
                    else // If the car is not yet within range of the tornado, continue moving it as normally.
                    {
                        if (useLowPolyTraffic)
                        {
                            if (car.transform.parent != originalParent[1])
                                car.transform.SetParent(originalParent[1]);
                        }
                        else
                        {
                            if (car.transform.parent != originalParent[0])
                                car.transform.SetParent(originalParent[0]);
                        }
                        car.transform.position += car.transform.forward * adjustedSpeed;
                    }
                }
            }
        }
        // Once the tornado goes away and all traffic inside it is exploded, change the bool.
        else playerController.tornadoExplodeCars = false;

        // Play the soundManager's 'woosh' sound if the player is passing a traffic car.
        if (playerController.currentLane != 0 && playerController.currentLane != 4)
        {
            if (allLaneTraffic[playerController.currentLane - 1][0] != leftCar && allLaneTraffic[playerController.currentLane - 1][0].transform.position.z < playerPosZ + 1.5f)
            {
                soundManager.PlayWoosh(true);
                leftCar = allLaneTraffic[playerController.currentLane - 1][0];
            }
        }
        if (playerController.currentLane != 7 && playerController.currentLane != 3)
        {
            if (allLaneTraffic[playerController.currentLane + 1][0] != rightCar && allLaneTraffic[playerController.currentLane + 1][0].transform.position.z < playerPosZ + 1.5f) // TODO: Fix index out of range exception
            {
                soundManager.PlayWoosh(false);
                rightCar = allLaneTraffic[playerController.currentLane + 1][0];
            }
        }
    }

    void FixedUpdate()
    {
        // If the game has ended, set variables to their respective end-game values.
        if (!gameEndSet)
        {
            isGameEnded = -1f;
            gameEndSet = true;
        }

        // Keep spawning roads.
        if (activeRoads.Count < maxRoads) SpawnRoad();

        // Keep cycling environment objects.
        switch (currentEnvironment)
        {
            case 0: // Spawn & return sci fi buildings (City 77)
                if (activeLeftBuildings.Count < maxBuildingPairs) SpawnSciFiBuilding(0);
                if (activeRightBuildings.Count < maxBuildingPairs) SpawnSciFiBuilding(1);
                
                // If any building is behind the player, move it to the object pool.
                if (activeLeftBuildings[0].transform.position.z < playerPosZ - 100) ReturnSciFiBuilding(0);
                if (activeRightBuildings[0].transform.position.z < playerPosZ - 100) ReturnSciFiBuilding(1);
                break;
            
            case 1: // Spawn & return wasteland buildings (Nuclear Wasteland)
                if (activeLeftWastelandBuildings.Count < maxBuildingPairs) SpawnWastelandBuilding(0);
                if (activeRightWastelandBuildings.Count < maxBuildingPairs) SpawnWastelandBuilding(1);

                // If any building is behind the player, move it to the object pool.
                if (activeLeftWastelandBuildings[0].transform.position.z < playerPosZ - 125) ReturnWastelandBuilding(0);
                if (activeRightWastelandBuildings[0].transform.position.z < playerPosZ - 125) ReturnWastelandBuilding(1);
                break;
            case 2: // Recycle space tunnels (Trans-galactic highway)
                if (activeSpaceTunnels[0].transform.position.z < playerPosZ - spaceTunnelLength + 70) RecycleSpaceTunnel();
                break;
            case 3:
                if (activeLeftRocks[0].transform.position.z < playerPosZ + 2) RecycleRock(0);
                if (activeRightRocks[0].transform.position.z < playerPosZ + 2) RecycleRock(1);
                if (Time.time - startTime > 1)
                {
                    if (activeLeftGrass[0].transform.position.z < playerPosZ) RecycleGrass(0);
                    if (activeRightGrass[0].transform.position.z < playerPosZ) RecycleGrass(1);
                }
                if (atlantis.transform.position.z < playerPosZ - 200) RecycleAtlantis();
                break;
            default:
                break;
        }

        if (lane0traffic.Count < maxCarsPerLane) SpawnTraffic(0);
        if (lane1traffic.Count < maxCarsPerLane) SpawnTraffic(1);
        if (lane2traffic.Count < maxCarsPerLane) SpawnTraffic(2);
        if (lane3traffic.Count < maxCarsPerLane) SpawnTraffic(3);
        if (lane4traffic.Count < maxCarsPerLane) SpawnTraffic(4);
        if (lane5traffic.Count < maxCarsPerLane) SpawnTraffic(5);
        if (lane6traffic.Count < maxCarsPerLane) SpawnTraffic(6);
        if (lane7traffic.Count < maxCarsPerLane) SpawnTraffic(7);

        // Spawn a powerup.
        if (!playerController.gameEnd && powerUpAllowedToSpawn && (Time.time - startTime) - powerUpSpawnTime >= Mathf.Max(-6.5f * playerController.accel + 30, 10)) SpawnPowerup();

        // If a road is behind the player, move it to the front.
        if (activeRoads[0].transform.position.z < playerPosZ - 99) RecycleRoad();

        // If any car is behind the player, move it to the object pool.
        if (Time.time - startTime > 1)
        {
            if (lane0traffic[0].transform.position.z < playerPosZ - 5) ReturnTraffic(0);
            if (lane1traffic[0].transform.position.z < playerPosZ - 5) ReturnTraffic(1);
            if (lane2traffic[0].transform.position.z < playerPosZ - 5) ReturnTraffic(2);
            if (lane3traffic[0].transform.position.z < playerPosZ - 5) ReturnTraffic(3);
            if (!playerController.gameEnd) // Return same lane traffic if the traffic is behind the player.
            {
                if (lane4traffic[0].transform.position.z < playerPosZ - 5) ReturnTraffic(4);
                if (lane5traffic[0].transform.position.z < playerPosZ - 5) ReturnTraffic(5);
                if (lane6traffic[0].transform.position.z < playerPosZ - 5) ReturnTraffic(6);
                if (lane7traffic[0].transform.position.z < playerPosZ - 5) ReturnTraffic(7);
            }
            else // Once the player has crashed and exploded, start returning same lane traffic that is 200 units in front of him.
            {
                if (lane5traffic[^1].transform.position.z > playerPosZ + 200) ReturnTraffic(5);
                if (lane4traffic[^1].transform.position.z > playerPosZ + 200) ReturnTraffic(4);
                if (lane6traffic[^1].transform.position.z > playerPosZ + 200) ReturnTraffic(6);
                if (lane7traffic[^1].transform.position.z > playerPosZ + 200) ReturnTraffic(7);
            }
        }
    }


    /*------------------------------------ ROAD SPAWNING FUNCTIONS ------------------------------------*/
    public void SpawnRoad()
    {
        GameObject spawnedRoad;
        if (activeRoads.Count > 0)
        {
            // Spawn a road one road length ahead of latest spawned road.
            spawnedRoad = Instantiate(roads[currentEnvironment], activeRoads[^1].transform.position + new Vector3(0, 0, roadLength), transform.rotation);
        }
        else
        {
            spawnedRoad = Instantiate(roads[currentEnvironment], transform.forward, transform.rotation);
        }
        activeRoads.Add(spawnedRoad);
    }

    // Move the road that is furthest back to the front.
    private void RecycleRoad()
    {
        GameObject recycledRoad = activeRoads[0];
        Vector3 newPosition = activeRoads[^1].transform.position + new Vector3(0, 0, roadLength);
        recycledRoad.transform.position = newPosition;
        activeRoads.RemoveAt(0);
        activeRoads.Add(recycledRoad);
    }

    // Spawn beginning roads behind the player.
    public void SpawnPosteriorRoads()
    {
        for (int i = 1; i < 11; i++)
        {
            Instantiate(roads[currentEnvironment], activeRoads[0].transform.position - new Vector3(0, 0, i * roadLength), transform.rotation);
        }
    }

    /*---------------------------------- ENVIRONMENT OBJECT SPAWNING FUNCTIONS ----------------------------------*/
    // Spawn beginning objects behind the player.
    public void CreatePosteriorObjects()
    {
        switch (currentEnvironment)
        {
            case 0: // Spawn sci fi buildings (City 77)
                OffsetAndFlip(activeLeftBuildings, -scifiPosteriorObjectMoveAmount, true);
                OffsetAndFlip(activeRightBuildings, -scifiPosteriorObjectMoveAmount, false);
                break;

            case 1: // Spawn wasteland buildings (Nuclear Wasteland)
                OffsetAndFlip(activeLeftWastelandBuildings, -wastelandPosteriorObjectMoveAmount, true);
                OffsetAndFlip(activeRightWastelandBuildings, -wastelandPosteriorObjectMoveAmount, false);
                break;

            case 2:
                OffsetAndFlip(activeSpaceTunnels, -spacePosteriorObjectMoveAmount, false);
                break;

            case 3:
                OffsetAndFlip(activeLeftRocks, -underwaterPosteriorObjectMoveAmount, true);
                OffsetAndFlip(activeRightRocks, -underwaterPosteriorObjectMoveAmount, false);
                OffsetAndFlip(activeLeftGrass, -underwaterPosteriorObjectMoveAmount, true, true);
                OffsetAndFlip(activeRightGrass, -underwaterPosteriorObjectMoveAmount, false, true);
                break;
        }
    }

    // Do the opposite: add and flip back
    public void ReturnPosteriorObjects()
    {
        switch (currentEnvironment)
        {
            case 0:
                OffsetAndFlip(activeLeftBuildings, scifiPosteriorObjectMoveAmount, true);
                OffsetAndFlip(activeRightBuildings, scifiPosteriorObjectMoveAmount, false);
                break;

            case 1:
                OffsetAndFlip(activeLeftWastelandBuildings, wastelandPosteriorObjectMoveAmount, true);
                OffsetAndFlip(activeRightWastelandBuildings, wastelandPosteriorObjectMoveAmount, false);
                break;

            case 2:
                OffsetAndFlip(activeSpaceTunnels, spacePosteriorObjectMoveAmount, false);
                break;

            case 3:
                OffsetAndFlip(activeLeftRocks, underwaterPosteriorObjectMoveAmount, true);
                OffsetAndFlip(activeRightRocks, underwaterPosteriorObjectMoveAmount, false);
                OffsetAndFlip(activeLeftGrass, underwaterPosteriorObjectMoveAmount, true, true);
                OffsetAndFlip(activeRightGrass, underwaterPosteriorObjectMoveAmount, false, true);
                break;
        }
    }

    // Get a scifi building from the building pool.
    public void SpawnSciFiBuilding(int right)
    {
        try
        {
            if (right == 0) // We are spawning a left building.
            {
                int buildingRotationIndex = Random.Range(0, leftBuildingPrefabs[leftBuildingIndex].buildingRotations.Count); // Randomly choose a rotation.
                GameObject leftBuilding = leftBuildingPrefabs[leftBuildingIndex].buildingRotations[buildingRotationIndex]; // Pick the building using current index, then pick the building's rotation.
                int rotationNumber = leftBuilding.name[0] - '0' - 1; // Retrieve the rotation # (1-4) and convert into an index to use for building_x_positions.
                float xPos = Random.Range(-208, building_x_positions[leftBuildingIndex, rotationNumber].Item1);
                Vector3 newPos;

                if (activeLeftBuildings.Count > 0)
                {
                    if (leftBuildingIndex == 5 && rotationNumber == 2)  // If the building has the drive-through tunnel do not randomize the X & Y spawning direction.
                        newPos = new Vector3(-133.3f, 0, activeLeftBuildings[^1].transform.position.z + 200);
                    
                    else // Otherwise spawn building normally.
                        newPos = new Vector3(xPos, Random.Range(-56.4f, 1), activeLeftBuildings[^1].transform.position.z + 200);
                }

                else // If this is the first time we are spawning a left building, initialize the Z position to 100.
                {
                    if (leftBuildingIndex == 5 && rotationNumber == 2)  // If the building has the drive-through tunnel do not randomize the X & Y spawning direction.
                        newPos = new Vector3(-133.3f, 0, 100);

                    else // Otherwise spawn building normally.
                        newPos = new Vector3(xPos, Random.Range(-56.4f, 1), 100);
                }
                
                leftBuilding.transform.position = newPos;
                activeLeftBuildings.Add(leftBuilding);
                leftBuilding.SetActive(true);
                leftBuildingPrefabs[leftBuildingIndex].buildingRotations.RemoveAt(buildingRotationIndex); // Remove the building from the original array to prevent unauthorized reuse.
                leftBuildingIndex += 1;
                if (leftBuildingIndex > 11) leftBuildingIndex = 0;
            }

            else // We are spawning a right building.
            {
                int buildingRotationIndex = Random.Range(0, rightBuildingPrefabs[rightBuildingIndex].buildingRotations.Count);
                GameObject rightBuilding = rightBuildingPrefabs[rightBuildingIndex].buildingRotations[buildingRotationIndex];
                int rotationNumber = rightBuilding.name[0] - '0' - 1;
                float xPos = Random.Range(building_x_positions[rightBuildingIndex, rotationNumber].Item2, 208);
                Vector3 newPos;

                if (activeRightBuildings.Count > 0)
                    newPos = new Vector3(xPos, Random.Range(-56.4f, 1), activeRightBuildings[^1].transform.position.z + 200);

                else // If this is the first time we are spawning a right building, initialize the Z position to 100.
                    newPos = new Vector3(xPos, Random.Range(-56.4f, 1), 100);
 
                rightBuilding.transform.position = newPos;
                activeRightBuildings.Add(rightBuilding);
                rightBuilding.SetActive(true);
                rightBuildingPrefabs[rightBuildingIndex].buildingRotations.RemoveAt(buildingRotationIndex); // Remove the building from the original array to prevent unauthorized reuse.
                rightBuildingIndex += 1;
                if (rightBuildingIndex > 11) rightBuildingIndex = 0;
            }
        }
        catch
        {
            Debug.Log("Building spawn failure");
        }
    }

    // Return the building back to the building pool.
    private void ReturnSciFiBuilding(int right)
    {
        Vector3 newPos = new(0,0,-1000); // The position we are returning the building to.
        if (right == 0)
        {
            activeLeftBuildings[0].SetActive(false);
            activeLeftBuildings[0].transform.position = newPos; // Return the building to the pool. 
            int indexToReturn = CalculateBuildngReturnIndex(leftBuildingIndex); // Calculate the original index of the returned building.
            leftBuildingPrefabs[indexToReturn].buildingRotations.Add(activeLeftBuildings[0]); // Add the building back to its original array using its original index.
            leftBuildingPrefabs[indexToReturn].buildingRotations.Sort((x, y) => x.name.CompareTo(y.name)); // Ensure that the secondary part of the building (the rotation) is in the same order as before.
            activeLeftBuildings.RemoveAt(0); // Remove building from the activeBuildings list.
        }
        else
        {
            activeRightBuildings[0].SetActive(false);
            activeRightBuildings[0].transform.position = newPos;
            int indexToReturn = CalculateBuildngReturnIndex(rightBuildingIndex);
            rightBuildingPrefabs[indexToReturn].buildingRotations.Add(activeRightBuildings[0]);
            rightBuildingPrefabs[indexToReturn].buildingRotations.Sort((x, y) => x.name.CompareTo(y.name));
            activeRightBuildings.RemoveAt(0);
        }
    }

    public void SpawnWastelandBuilding(int right)
    {
        // Increment building count & calculate distance from last building.
        int zDistanceFromLastBuilding;
        if (currentWastelandBuildingType == 0)
        {
            wasteLandBuildingCount += 1;
            zDistanceFromLastBuilding = 100;
        }
        else
        {
            wasteLandSkyscraperCount += 1;
            zDistanceFromLastBuilding = 75;
        }

        // Switch wasteland building types if needed.
        if (wasteLandBuildingCount > 64)
        {
            currentWastelandBuildingType = 1;
            wasteLandBuildingCount = 0;
            leftBuildingIndex = Random.Range(0, leftWastelandBuildings[currentWastelandBuildingType].buildingTypes.Count);
            rightBuildingIndex = (leftBuildingIndex + 16) % rightWastelandBuildings[currentWastelandBuildingType].buildingTypes.Count;
        }
        else if (wasteLandSkyscraperCount > 30)
        {
            currentWastelandBuildingType = 0;
            wasteLandSkyscraperCount = 0;
        } 

        if (right == 0) // We are spawning a left building.
        {
            WastelandBuilding leftBuilding = leftWastelandBuildings[currentWastelandBuildingType].buildingTypes[leftBuildingIndex];
            float xPos = Random.Range(leftBuilding.maxXPosition, leftBuilding.minXPosition);
            float yPos = Random.Range(leftBuilding.minYPosition, leftBuilding.maxYPosition);
            Vector3 newPos;

            // Set spawn position.
            if (activeLeftWastelandBuildings.Count > 0)
            {
                newPos = new Vector3(xPos, yPos, activeLeftWastelandBuildings[^1].transform.position.z + zDistanceFromLastBuilding);
            }
            else // If this is the first time we are spawning a left building, initialize the Z position to 200.
            {
                newPos = new Vector3(xPos, yPos, 200);
            }

            // Set rotation.
            if (leftBuilding.rotateableBuilding)
            {
                Vector3 randomRotation = leftBuilding.rotations[Random.Range(0, leftBuilding.rotations.Count)];
                leftBuilding.transform.rotation = Quaternion.Euler(randomRotation);
            }

            leftBuilding.transform.position = newPos;
            activeLeftWastelandBuildings.Add(leftBuilding);
            leftBuilding.gameObject.SetActive(true);
            leftWastelandBuildings[currentWastelandBuildingType].buildingTypes.RemoveAt(leftBuildingIndex); // Remove the building from the original array to prevent unauthorized reuse.
            leftBuildingIndex += 1;
            if (leftBuildingIndex > leftWastelandBuildings[currentWastelandBuildingType].buildingTypes.Count - 1) leftBuildingIndex = 0;
        }

        else // We are spawning a right building.
        {
            WastelandBuilding rightBuilding = rightWastelandBuildings[currentWastelandBuildingType].buildingTypes[rightBuildingIndex];
            float xPos = Random.Range(rightBuilding.minXPosition, rightBuilding.maxXPosition);
            float yPos = Random.Range(rightBuilding.minYPosition, rightBuilding.maxYPosition);
            Vector3 newPos;

            // Set spawn position.
            if (activeRightWastelandBuildings.Count > 0)
            {
                newPos = new Vector3(xPos, yPos, activeRightWastelandBuildings[^1].transform.position.z + zDistanceFromLastBuilding);
            }
            else // If this is the first time we are spawning a right building, initialize the Z position to 200.
            {
                newPos = new Vector3(xPos, yPos, 200);
            }

            // Set rotation.
            if (rightBuilding.rotateableBuilding)
            {
                Vector3 randomRotation = rightBuilding.rotations[Random.Range(0, rightBuilding.rotations.Count)];
                rightBuilding.transform.rotation = Quaternion.Euler(randomRotation);
            }

            rightBuilding.transform.position = newPos;
            activeRightWastelandBuildings.Add(rightBuilding);
            rightBuilding.gameObject.SetActive(true);
            rightWastelandBuildings[currentWastelandBuildingType].buildingTypes.RemoveAt(rightBuildingIndex); // Remove the building from the original array to prevent unauthorized reuse.
            rightBuildingIndex += 1;
            if (rightBuildingIndex > rightWastelandBuildings[currentWastelandBuildingType].buildingTypes.Count - 1) rightBuildingIndex = 0;
        }
    }

    // Return the building back to the building pool.
    private void ReturnWastelandBuilding(int right)
    {
        Vector3 newPos = new(0, 0, -1000); // The position we are returning the building to.
        int buildingType;
        if (right == 0)
        {
            buildingType = activeLeftWastelandBuildings[0].buildingType;
            activeLeftWastelandBuildings[0].gameObject.SetActive(false);
            activeLeftWastelandBuildings[0].transform.position = newPos; // Return the building to the pool. 
            leftWastelandBuildings[buildingType].buildingTypes.Add(activeLeftWastelandBuildings[0]); // Add the building back to its original array using its original index.
            activeLeftWastelandBuildings.RemoveAt(0); // Remove building from the activeBuildings list.
        }
        else
        {
            buildingType = activeRightWastelandBuildings[0].buildingType;
            activeRightWastelandBuildings[0].gameObject.SetActive(false);
            activeRightWastelandBuildings[0].transform.position = newPos; // Return the building to the pool. 
            rightWastelandBuildings[buildingType].buildingTypes.Add(activeRightWastelandBuildings[0]); // Add the building back to its original array using its original index.
            activeRightWastelandBuildings.RemoveAt(0); // Remove building from the activeBuildings list.
        }
    }

    private void SpawnRock(int right)
    {
        GameObject spawnedRock;
        int randomRockIndex;
        if (right == 1)
        {
            randomRockIndex = Random.Range(0, rightRocks.Count);
            spawnedRock = rightRocks[randomRockIndex];
            rightRocks.RemoveAt(randomRockIndex);
            if (activeRightRocks.Count > 0)
            {
                spawnedRock.transform.position = new Vector3(spawnedRock.transform.position.x, spawnedRock.transform.position.y, activeRightRocks[^1].transform.position.z + Random.Range(25, 50));
            }
            else
            {
                spawnedRock.transform.position = new Vector3(spawnedRock.transform.position.x, spawnedRock.transform.position.y, Random.Range(25, 50));
            }
            activeRightRocks.Add(spawnedRock);
        }
        else
        {
            randomRockIndex = Random.Range(0, leftRocks.Count);
            spawnedRock = leftRocks[randomRockIndex];
            leftRocks.RemoveAt(randomRockIndex);
            if (activeLeftRocks.Count > 0)
            {
                spawnedRock.transform.position = new Vector3(spawnedRock.transform.position.x, spawnedRock.transform.position.y, activeLeftRocks[^1].transform.position.z + Random.Range(25, 50));
            }
            else
            {
                spawnedRock.transform.position = new Vector3(spawnedRock.transform.position.x, spawnedRock.transform.position.y, Random.Range(25, 50));
            }
            activeLeftRocks.Add(spawnedRock);
        }
        spawnedRock.transform.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
        spawnedRock.SetActive(true);
    }


    private void RecycleRock(int right)
    {
        if (right == 1)
        {
            GameObject recycledRock = activeRightRocks[0];
            rightRocks.Add(recycledRock);
            activeRightRocks.RemoveAt(0);
            SpawnRock(1);
        }
        else
        {
            GameObject recycledRock = activeLeftRocks[0];
            leftRocks.Add(recycledRock);
            activeLeftRocks.RemoveAt(0);
            SpawnRock(0);
        }
    }

    private void SpawnGrass(int right)
    {
        GameObject spawnedGrass;
        int randomGrassIndex;
        if (right == 1)
        {
            randomGrassIndex = Random.Range(0, rightGrass.Count);
            spawnedGrass = rightGrass[randomGrassIndex];
            rightGrass.RemoveAt(randomGrassIndex);
            if (activeRightGrass.Count > 0)
            {
                spawnedGrass.transform.position = new Vector3(Random.Range(25f, 60f), spawnedGrass.transform.position.y, activeRightGrass[^1].transform.position.z + 5);
            }
            else
            {
                spawnedGrass.transform.position = new Vector3(Random.Range(25f, 60f), spawnedGrass.transform.position.y, 5);
            }
            activeRightGrass.Add(spawnedGrass);
        }
        else
        {
            randomGrassIndex = Random.Range(0, leftGrass.Count);
            spawnedGrass = leftGrass[randomGrassIndex];
            leftGrass.RemoveAt(randomGrassIndex);
            if (activeLeftGrass.Count > 0)
            {
                spawnedGrass.transform.position = new Vector3(Random.Range(-60f, -25f), spawnedGrass.transform.position.y, activeLeftGrass[^1].transform.position.z + 5);
            }
            else
            {
                spawnedGrass.transform.position = new Vector3(Random.Range(-60f, -25f), spawnedGrass.transform.position.y, 5);
            }
            activeLeftGrass.Add(spawnedGrass);
        }
        spawnedGrass.SetActive(true);
        //spawnedGrass.transform.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
    }

    private void RecycleGrass(int right)
    {
        if (right == 1)
        {
            GameObject recycledGrass = activeRightGrass[0];
            rightGrass.Add(recycledGrass);
            activeRightGrass.RemoveAt(0);
            SpawnGrass(1);
        }
        else
        {
            GameObject recycledGrass = activeLeftGrass[0];
            leftGrass.Add(recycledGrass);
            activeLeftGrass.RemoveAt(0);
            SpawnGrass(0);
        }
    }

    private void RecycleAtlantis()
    {
        atlantis.transform.position = new Vector3(atlantis.transform.position.x, atlantis.transform.position.y, playerPosZ + 5000);
        atlantis.SetActive(true);
    }

    private void SpawnSpaceTunnel()
    {
        GameObject spawnedSpaceTunnel;
        int randomSpaceTunnelIndex = Random.Range(0, spaceTunnels.Count);
        spawnedSpaceTunnel = spaceTunnels[randomSpaceTunnelIndex];
        spaceTunnels.RemoveAt(randomSpaceTunnelIndex);
        if (activeSpaceTunnels.Count > 0)
        {
            spawnedSpaceTunnel.transform.position = new Vector3(spawnedSpaceTunnel.transform.position.x, spawnedSpaceTunnel.transform.position.y, activeSpaceTunnels[^1].transform.position.z + spaceTunnelLength);
        }
        else
        {
            spawnedSpaceTunnel.transform.position = new Vector3(spawnedSpaceTunnel.transform.position.x, spawnedSpaceTunnel.transform.position.y, 100);
        }
        activeSpaceTunnels.Add(spawnedSpaceTunnel);
        spawnedSpaceTunnel.SetActive(true);
    }

    private void RecycleSpaceTunnel()
    {
        GameObject recycledSpaceTunnel = activeSpaceTunnels[0];
        spaceTunnels.Add(recycledSpaceTunnel);
        activeSpaceTunnels.RemoveAt(0);
        SpawnSpaceTunnel();
    }

    // Helper function for ReturnBuilding that calculates the index that we need to return the building to.
    private int CalculateBuildngReturnIndex(int index)
    {
        int indexToReturn = (index - (maxBuildingPairs - 12)) % 12;
        if (indexToReturn < 0)
            indexToReturn += 12;
        return indexToReturn;
    }


    /*----------------------------------- TRAFFIC SPAWNING FUNCTIONS ----------------------------------*/
    public void PutTrafficBehindPlayer()
    {
        // 1. Find max z position from last 4 elements of lane4traffic–lane7traffic
        float maxZ = float.MinValue;

        List<List<GameObject>> sameWayLanes = new List<List<GameObject>>()
        {
            lane4traffic,
            lane5traffic,
            lane6traffic,
            lane7traffic
        };

        foreach (var lane in sameWayLanes)
        {
            if (lane == null || lane.Count == 0) continue;

            int startIndex = Mathf.Max(0, lane.Count - 4); // Last 4 elements
            for (int i = startIndex; i < lane.Count; i++)
            {
                if (lane[i] == null) continue;
                float z = lane[i].transform.position.z;
                if (z > maxZ) maxZ = z;
            }
        }

        if (maxZ == float.MinValue)
            return; // No objects found
        maxZ += 10;
        // 2. Subtract full value from lane4traffic–lane7traffic
        foreach (var lane in sameWayLanes)
        {
            if (lane == null) continue;

            foreach (var go in lane)
            {
                if (go == null) continue;
                Vector3 pos = go.transform.position;
                pos.z -= maxZ;
                go.transform.position = pos;
            }
        }

        // 3. Subtract half value from lane0traffic–lane3traffic
        List<List<GameObject>> oppositeWayLanes = new List<List<GameObject>>()
        {
            lane0traffic,
            lane1traffic,
            lane2traffic,
            lane3traffic
        };

        foreach (var lane in oppositeWayLanes)
        {
            if (lane == null) continue;

            foreach (var go in lane)
            {
                if (go == null) continue;
                Vector3 pos = go.transform.position;
                pos.z -= maxZ * 0.5f;
                go.transform.position = pos;
            }
        }
    }

    // Get a traffic car from the traffic pool.
    public void SpawnTraffic(int lane /*0-7*/)
    {
        GameObject vehicle;
        float distance = Random.Range(10, 50);
        float trafficScaler = !playerController.bullet
            ? Mathf.Min(playerController.accel, 3.38f)
            : playerController.oldAccel;
        trafficScaler = Mathf.Max(trafficScaler, 1);
        distance *= trafficDensity * trafficScaler;
        if (lane < 4) distance *= 1.25f;

        int vehicle_id = vehicle_ids[Random.Range(0, vehicle_ids.Length)];

        BaseTrafficList[] currentTrafficPrefabList = useLowPolyTraffic
            ? trafficPrefabsLowPoly
            : trafficPrefabs;

        // Ensure the selected vehicle has at least one variation
        while (currentTrafficPrefabList[vehicle_id].trafficVariations.Count < 1)
        {
            vehicle_id = (vehicle_id + 1) % currentTrafficPrefabList.Length;
        }

        int vehicle_variation = Random.Range(0, currentTrafficPrefabList[vehicle_id].trafficVariations.Count);
        vehicle = currentTrafficPrefabList[vehicle_id].trafficVariations[vehicle_variation];

        float x = lane switch
        {
            0 => -11.5f,
            1 => -8.5f,
            2 => -5.5f,
            3 => -2.5f,
            4 => 1f,
            5 => 4f,
            6 => 7f,
            7 => 10f,
            _ => throw new System.ArgumentOutOfRangeException(nameof(lane))
        };

        List<GameObject> laneList = lane switch
        {
            0 => lane0traffic,
            1 => lane1traffic,
            2 => lane2traffic,
            3 => lane3traffic,
            4 => lane4traffic,
            5 => lane5traffic,
            6 => lane6traffic,
            7 => lane7traffic,
            _ => throw new System.ArgumentOutOfRangeException(nameof(lane))
        };

        float z = (laneList.Count > 0)
            ? laneList[^1].transform.position.z + distance
            : playerPosZ + distance;

        if (lane == 4 && z < playerPosZ + 12) z = playerPosZ + 12;

        if (playerController.gameEnd && lane > 3)
        {
            z = laneList[0].transform.position.z - distance;
            if (z > playerPosZ - 5) z = playerPosZ - 5;
        }

        Vector3 spawnPos = new Vector3(x, vehicle.transform.position.y, z);
        Quaternion rot = lane <= 3 ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

        vehicle.transform.SetPositionAndRotation(spawnPos, rot);
        if (playerController.gameEnd && lane > 3) laneList.Insert(0, vehicle);
        else laneList.Add(vehicle);
        currentTrafficPrefabList[vehicle_id].trafficVariations.RemoveAt(vehicle_variation);
        vehicle.SetActive(true);
    }

    // Return the 0th traffic car of the lane to the traffic pool.
    private void ReturnTraffic(int lane)
    {
        // Select correct lane list
        List<GameObject> laneList = lane switch
        {
            0 => lane0traffic,
            1 => lane1traffic,
            2 => lane2traffic,
            3 => lane3traffic,
            4 => lane4traffic,
            5 => lane5traffic,
            6 => lane6traffic,
            7 => lane7traffic,
            _ => throw new System.ArgumentOutOfRangeException(nameof(lane))
        };

        if (laneList.Count == 0)
            return;

        int index = playerController.gameEnd && lane > 3 ? laneList.Count - 1 : 0;
        GameObject vehicle = laneList[index];

        // Extract vehicle ID and variation index from name
        int vehicle_id = vehicle.name[0] - '0'; // prefix
        int vehicle_variation = vehicle.name[^1] - '0'; // suffix
        if (vehicle_variation > 9) vehicle_variation = 0;

        float yReturnValue = original_traffic_y_positions[vehicle_id];
        Vector3 resetPosition = new(lane_positions[vehicle_variation], yReturnValue, -101 + 10 * vehicle_id);

        // Deactivate and reset vehicle
        vehicle.transform.SetPositionAndRotation(resetPosition, Quaternion.identity);
        if (playerController.tornado || playerController.tornadoExplodeCars)
        {
            tornadoTraffic.Remove(vehicle);
            if (useLowPolyTraffic) vehicle.transform.SetParent(originalParent[1]);
            else vehicle.transform.SetParent(originalParent[0]);
        }

        // Return to appropriate prefab pool
        if (useLowPolyTraffic)
        {
            trafficPrefabsLowPoly[vehicle_id].trafficVariations.Add(vehicle);
        }
        else
        {
            trafficPrefabs[vehicle_id].trafficVariations.Add(vehicle);
        }

        laneList.RemoveAt(index);
    }

    /*------------------------------------ OTHER TRAFFIC FUNCTIONS ------------------------------------*/
    // If the player collides with a traffic while he is in aggro mode, this coroutine will launch the traffic into the air.
    public IEnumerator LaunchTraffic(Transform trafficTransform)
    {
        float upwardSpeed;
        if (playerController.aggro) upwardSpeed = Mathf.Min(6 + 10 * playerController.accel, 25);  // Units per second.
        else upwardSpeed = Mathf.Min(6 + 10 * playerController.accel, 40);

        float rotationSpeed = Mathf.Min(720 + 180 * playerController.accel, 14400); // Degrees per second.
        float duration = 1.0f / playerController.accel; // Duration of the effect in seconds.
        float sidewardSpeed = 15f; // Units per second.
        float distance = Mathf.Min(100 + 15f * playerController.accel, 200);

        // Choose a random direction: left or right.
        Vector3 direction = (Random.value < 0.5) ? Vector3.left : Vector3.right;
        direction *= sidewardSpeed;

        // Determine the start and end positions for the move.
        Vector3 startPosition = trafficTransform.position;
        Vector3 horizontalEndPosition = startPosition + distance * transform.forward + direction;
        float endYPosition = startPosition.y + upwardSpeed;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            // Calculate the Lerp factor.
            float factor = t / duration;

            // Interpolate the horizontal position using Lerp.
            Vector3 horizontalPosition = Vector3.Lerp(startPosition, horizontalEndPosition, factor);

            // Calculate the vertical position using EaseOutCubic to make it arc.
            float verticalPosition = startPosition.y + (endYPosition - startPosition.y) * EaseOutCubic(factor / 2);

            // Apply the calculated positions.
            trafficTransform.position = new Vector3(horizontalPosition.x, verticalPosition, horizontalPosition.z);

            // Rotate the object around its local X axis.
            trafficTransform.localRotation *= Quaternion.Euler(rotationSpeed * Time.deltaTime * Vector3.left);

            yield return null;
        }

        // Move the explosion to the traffic transform's position and activate it.
        int index = aggroExplosionIndex;
        aggroExplosionIndex += 1;
        if (aggroExplosionIndex == aggroExplosions.Length) aggroExplosionIndex = 0;

        aggroExplosions[index].transform.position = trafficTransform.position;
        aggroExplosions[index].gameObject.SetActive(true);
        aggroExplosions[index].Play();
        soundManager.PlayTrafficExplosion(true, false);

        trafficTransform.position = new Vector3(0, 0, playerPosZ - 5);
        yield return new WaitForSeconds(0.5f);

        // Move the explosion back to the fixed position.
        aggroExplosions[index].transform.position = new Vector3(0, 0, 0);
        aggroExplosions[index].gameObject.SetActive(false);
    }

    // Sets an explosion's parent to newparent, plays the explosion, then moves the explosion back to its oldparent (either the AggroExplosions gameObject or the TrafficExplosions gameObject).
    public IEnumerator ExplodeTraffic(Transform oldparent, Transform newparent, bool inTornado)
    {
        int index = trafficExplosionIndex;
        trafficExplosionIndex += 1;
        if (trafficExplosionIndex == trafficExplosions.Length) trafficExplosionIndex = 0;

        AudioSource explosionAudio = trafficExplosions[index].GetComponent<AudioSource>(); // TODO: Why is there a GetComponent here?
        explosionAudio.volume = SaveManager.Instance.SaveData.EffectsVolumeMultiplier;

        // Store the original scale of the explosion.
        Vector3 originalScale = trafficExplosions[index].transform.localScale;

        // Check if newparent is active.
        if (newparent.gameObject.activeInHierarchy)
        {
            // Set the parent to newparent so it follows the object.
            trafficExplosions[index].transform.SetParent(newparent);
            if (SaveManager.Instance.SaveData.cameraType == 0) trafficExplosions[index].transform.localPosition = new Vector3(0, 0, 3); // If we are in first person, move the explosion slightly forward so that's visible
            else trafficExplosions[index].transform.localPosition = new Vector3(0, 0, 0);
        }
        else
        {
            // If newparent is not active, just set the position to the newparent's position.
            trafficExplosions[index].transform.position = newparent.position;
        }

        // If inTornado is true, make adjustments.
        if (inTornado || playerController.gameEnd)
        {
            explosionAudio.playOnAwake = false;

            // Make the explosion 3 times bigger
            if (inTornado) trafficExplosions[index].transform.localScale = originalScale * 3;
        }

        // Set the Particle System speed to twice the normal speed.
        trafficExplosions[index].playbackSpeed = 2;

        // Activate the explosion and play it.
        trafficExplosions[index].gameObject.SetActive(true);
        trafficExplosions[index].Play();
        if (!playerController.gameEnd) soundManager.PlayTrafficExplosion(false, inTornado);

        yield return new WaitForSeconds(0.5f);

        // Move the explosion back to the fixed position and set its parent back to oldparent.
        trafficExplosions[index].transform.SetParent(oldparent);
        trafficExplosions[index].playbackSpeed = 1;
        trafficExplosions[index].transform.position = new Vector3(0, 0, 0);
        trafficExplosions[index].gameObject.SetActive(false);

        // Revert playOnAwake to its original state (true) for AudioSource and reset the scale.
        explosionAudio.playOnAwake = true;
        trafficExplosions[index].transform.localScale = originalScale;
    }

    // Replaces the parent of the traffic with the tornado object.
    void ReplaceTrafficParent(GameObject car)
    {
        car.transform.SetParent(playerController.tornadoObject.transform);
    }


    /*---------------------------------- POWERUP MANAGEMENT FUNCTIONS ---------------------------------*/
    // Spawns a random powerup with more weight on spawning the 'lives' powerup.
    public void SpawnPowerup()
    {
        powerUpSpawnTime = Time.time - startTime;
        int randomPowerup = powerup_probabilities[Random.Range(0, powerup_probabilities.Length)];
        //int randomPowerup = 2;
        powerups[randomPowerup].transform.position = new(lane_positions[Random.Range(0, lane_positions.Length)], -5, playerPosZ + 200);
        powerups[randomPowerup].Play();
    }

    // Returns the lane with the furthest away traffic from the player ; Used for the auto lane changing the player when he is in the 'bullet' powerup.
    public int LaneChangeForBullet(int lane, float playerPosition, bool ending)
    {
        float getZPositionOfLane(int laneNumber)
        {
            return laneNumber switch
            {
                0 => lane0traffic.Count > 0 ? lane0traffic[0].transform.position.z : -1,
                1 => lane1traffic.Count > 0 ? lane1traffic[0].transform.position.z : -1,
                2 => lane2traffic.Count > 0 ? lane2traffic[0].transform.position.z : -1,
                3 => lane3traffic.Count > 0 ? lane3traffic[0].transform.position.z : -1,
                4 => lane4traffic.Count > 0 ? lane4traffic[0].transform.position.z : -1,
                5 => lane5traffic.Count > 0 ? lane5traffic[0].transform.position.z : -1,
                6 => lane6traffic.Count > 0 ? lane6traffic[0].transform.position.z : -1,
                7 => lane7traffic.Count > 0 ? lane7traffic[0].transform.position.z : -1,
                _ => -1,
            };
        }

        if (ending)
        {
            int bestLane = 4;
            float maxDistance = -Mathf.Infinity;

            for (int i = 4; i <= 7; i++)
            {
                float currentDistance = Mathf.Abs(getZPositionOfLane(i) - playerPosition);
                if (currentDistance > maxDistance)
                {
                    maxDistance = currentDistance;
                    bestLane = i;
                }
            }

            return bestLane - lane;
        }

        float leftLaneDistance = -1;
        float rightLaneDistance = -1;

        if (lane > 0) leftLaneDistance = Mathf.Abs(getZPositionOfLane(lane - 1) - playerPosition);
        float currentLaneDistance = Mathf.Abs(getZPositionOfLane(lane) - playerPosition);
        if (lane < 7) rightLaneDistance = Mathf.Abs(getZPositionOfLane(lane + 1) - playerPosition);

        float leftDiff = Mathf.Abs(leftLaneDistance - currentLaneDistance);
        float rightDiff = Mathf.Abs(rightLaneDistance - currentLaneDistance);

        // Check if the distances are close to each other.
        //if (leftDiff <= 5 && leftLaneDistance > rightLaneDistance) return -2;
        //else if (rightDiff <= 5 && rightLaneDistance > leftLaneDistance) return 2;

        /*else*/ if (leftLaneDistance > currentLaneDistance && leftLaneDistance > rightLaneDistance) return -1;
        else if (rightLaneDistance > currentLaneDistance && rightLaneDistance > leftLaneDistance) return 1;

        else return 0;
    }


    /*---------------------------------------- OTHER FUNCTIONS ----------------------------------------*/
    private static void OffsetAndFlip(IList<GameObject> list, float zDelta, bool isLeft, bool isGrass = false)
    {
        if (list == null) return;

        for (int i = 1; i < list.Count; i++)
        {
            var go = list[i];
            if (go == null) continue;

            Transform t = go.transform;

            // World position Z adjustment
            Vector3 p = t.position;
            p.z += zDelta;
            t.position = p;

            if (isGrass)
            {
                // Rotate 180 degrees around Z axis (local rotation)
                t.Rotate(0f, 0f, 180f, Space.Self);
            }
            else
            {
                // Flip scale depending on left/right
                Vector3 s = t.localScale;
                if (isLeft)
                    s.z = -s.z;
                else
                    s.x = -s.x;
                t.localScale = s;
            }
        }
    }

    private static void OffsetAndFlip(IList<WastelandBuilding> list, float zDelta, bool isLeft)
    {
        if (list == null) return;

        for (int i = 1; i < list.Count; i++)
        {
            var wb = list[i];
            if (wb == null) continue;

            Transform t = wb.transform;

            // World position Z adjustment
            Vector3 p = t.position;
            p.z += zDelta;
            t.position = p;

            // Flip scale depending on left/right
            Vector3 s = t.localScale;
            if (isLeft)
                s.z = -s.z;
            else
                s.x = -s.x;
            t.localScale = s;
        }
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

