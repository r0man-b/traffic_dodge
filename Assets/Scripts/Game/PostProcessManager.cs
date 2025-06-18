using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessManager : MonoBehaviour
{
    // Public objects.
    public VolumeProfile profile;
    public Material[] roadLine;
    public Material[] roadEdge;
    public Material[] metalWall;
    public Material[] roadDashLine;

    // Variables to store default lift gamma gain values.
    private Vector4 originalLift = new Vector4(0.99f, 0.99f, 1.00f, -0.045f);
    private Vector4 originalGamma = new Vector4(0.83f, 1.00f, 0.92f, 0.08f);
    private Vector4 originalGain = new Vector4(0.77f, 1.00f, 0.95f, 0.52f);

    // Variables to store default material colours.
    private Color originalRoadLineColor;
    private Color originalRoadLineColorEmissive;
    private Color originalRoadEdgeColorEmissive;
    private Color originalMetalWallColorEmissive;
    private Color originalroadDashLineColorEmissive;

    // Post-processing effects
    private LiftGammaGain liftGammaGain;
    public Vignette vignette;
    private const float duration = 93f;

    // Motion blur
    public MotionBlur motionBlur;
    private const float motionBlurTargetIntensity = 0.337f;

    // System variables.
    private PlayerController playerController;
    private float startTime;
    private bool inCoroutine = false;
    private bool colorsSet = false;
    private bool inScreenFlash = false;

    private int currentEnvironment = 0;

    private void Start()
    {
        // Start the clock.
        startTime = Time.time;

        // Store initial material colors from the materials themselves
        if (roadLine != null)
        {
            originalRoadLineColor = roadLine[currentEnvironment].HasProperty("_BaseColor")
                ? roadLine[currentEnvironment].GetColor("_BaseColor")
                : roadLine[currentEnvironment].color;

            originalRoadLineColorEmissive = roadLine[currentEnvironment].HasProperty("_EmissionColor")
                ? roadLine[currentEnvironment].GetColor("_EmissionColor")
                : Color.black;
        }

        if (roadEdge != null && roadEdge[currentEnvironment].HasProperty("_EmissionColor"))
        {
            originalRoadEdgeColorEmissive = roadEdge[currentEnvironment].GetColor("_EmissionColor");
        }

        if (metalWall != null && metalWall[currentEnvironment].HasProperty("_EmissionColor"))
        {
            originalMetalWallColorEmissive = metalWall[currentEnvironment].GetColor("_EmissionColor");
        }

        if (roadDashLine != null && roadDashLine[currentEnvironment].HasProperty("_EmissionColor"))
        {
            originalroadDashLineColorEmissive = roadDashLine[currentEnvironment].GetColor("_EmissionColor");
        }

        // Get Lift Gamma Gain from Post Processing Profile
        if (profile.TryGet(out liftGammaGain))
        {
            // Load the saved gamma value from SaveData
            float savedGammaW = SaveManager.Instance.SaveData.GammaValue * 3f - 0.16f;

            // Apply the saved gamma W value to the gamma vector
            Vector4 gamma = originalGamma;
            gamma.w = savedGammaW;
            liftGammaGain.gamma.value = gamma;
            originalGamma = gamma;

            Debug.Log($"Applied Saved Gamma W: {savedGammaW}");
        }

        // Get Motion Blur
        profile.TryGet(out motionBlur);
        motionBlur.intensity.value = 0;

        // Get Vignette Effect
        if (profile.TryGet(out vignette))
        {
            // Enable vignette if it was saved as enabled
            vignette.active = SaveManager.Instance.SaveData.VignetteEnabled;
            vignette.intensity.value = 0; // Start at 0 intensity
            Debug.Log($"Vignette Enabled: {vignette.active}");
        }

        // Find the playerController script.
        GameObject PlayerCarObject = GameObject.Find("PlayerCar");
        playerController = PlayerCarObject.GetComponent<PlayerController>();
    }

    private void FixedUpdate()
    {
        if (playerController.raceStarted && !playerController.gameEnd && !playerController.aggro)
        {
            float elapsedTime = Time.time - (startTime + playerController.accelTimeOffset);

            if (elapsedTime <= duration)
            {
                float newMotionBlurIntensity = Mathf.Lerp(0, motionBlurTargetIntensity, elapsedTime / duration);
                //motionBlur.intensity.value = newMotionBlurIntensity;
            }
            else
            {
                //motionBlur.intensity.value = motionBlurTargetIntensity;
            }

        }

        // **Increase Vignette Intensity Based on Acceleration**
        if (vignette != null && vignette.active && !playerController.gameEnd && !playerController.aggro)
        {
            float accelRatio = Mathf.Clamp01(playerController.accel / playerController.currentCar.accelMaxValue);
            vignette.intensity.value = Mathf.Lerp(0, 0.75f, accelRatio);
        }


        // Reset all screen effects to default state.
        if (!inCoroutine && !colorsSet && !inScreenFlash)
        {
            roadLine[currentEnvironment].SetColor("_BaseColor", originalRoadLineColor);
            roadDashLine[currentEnvironment].SetColor("_BaseColor", Color.white);
            roadLine[currentEnvironment].SetColor("_EmissionColor", originalRoadLineColorEmissive);
            roadEdge[currentEnvironment].SetColor("_EmissionColor", originalRoadEdgeColorEmissive);
            metalWall[currentEnvironment].SetColor("_EmissionColor", originalMetalWallColorEmissive);
            roadDashLine[currentEnvironment].SetColor("_EmissionColor", originalroadDashLineColorEmissive);

            colorsSet = true;
        }
    }


    /*------------------------------- POWERUP SCREEN COLOURING FUNCTIONS ------------------------------*/
    public IEnumerator ColorScreen(Color color, float duration)
    {
        inCoroutine = true;
        colorsSet = false;

        // If the duration is greater than 5, we are in the aggro powerup. Save 3 seconds at the end for the flashing (12 instead of 15).
        if (duration > 5) duration = 12;

        // Access the LiftGammaGain override.
        if (profile.TryGet(out LiftGammaGain liftGammaGain))
        {
            SetColorValues(color);

            // Wait.
            yield return new WaitForSeconds(duration);

            // Flash for 3 seconds.
            if (duration > 5)
            {
                for (int i = 0; i < 3; i++)
                {
                    // Set original values.
                    SetOriginalValues(false);
                    yield return new WaitForSeconds(0.5f);

                    // Set to red values.
                    SetColorValues(color);
                    yield return new WaitForSeconds(0.5f); // Remain for the rest of the second.
                }
            }
            inCoroutine = false;

            // Ensure the ending values are the original ones.
            SetOriginalValues(true);
        }
    }

    private void SetColorValues(Color color)
    {
        if (profile.TryGet(out LiftGammaGain liftGammaGain))
        {
            if (color == Color.red)
            {
                liftGammaGain.lift.value = new Vector4(1.00f, 0.84f, 0.83f, originalLift.w);
                liftGammaGain.gamma.value = new Vector4(1.00f, 0.00f, 0.003f, originalGamma.w);
                liftGammaGain.gain.value = new Vector4(1.00f, 0.07f, 0.00f, originalGain.w);
            }

            else if (color == Color.blue)
            {
                liftGammaGain.lift.value = new Vector4(0.99f, 0.99f, 1.00f, originalLift.w);
                liftGammaGain.gamma.value = new Vector4(0.75f, 0.81f, 1.00f, originalGamma.w);
                liftGammaGain.gain.value = new Vector4(1.00f, 1.00f, 1.00f, originalGain.w);
            }

            if (color == Color.white) metalWall[currentEnvironment].SetColor("_EmissionColor", Color.black);
            else metalWall[currentEnvironment].SetColor("_EmissionColor", color);
            roadLine[currentEnvironment].SetColor("_BaseColor", color);
            roadDashLine[currentEnvironment].SetColor("_BaseColor", color);
            roadLine[currentEnvironment].SetColor("_EmissionColor", color);
            roadEdge[currentEnvironment].SetColor("_EmissionColor", color);
            roadDashLine[currentEnvironment].SetColor("_EmissionColor", color);
        }
    }

    private void SetOriginalValues(bool ending)
    {
        if (profile.TryGet(out LiftGammaGain liftGammaGain))
        {
            if (ending)
            {
                liftGammaGain.lift.value = originalLift;
                liftGammaGain.gamma.value = new Vector4(originalGamma.x, originalGamma.y, originalGamma.z, originalGamma.w);
                liftGammaGain.gain.value = originalGain;
            }
            roadLine[currentEnvironment].SetColor("_BaseColor", originalRoadLineColor);
            roadDashLine[currentEnvironment].SetColor("_BaseColor", Color.white);
            roadLine[currentEnvironment].SetColor("_EmissionColor", originalRoadLineColorEmissive);
            roadEdge[currentEnvironment].SetColor("_EmissionColor", originalRoadEdgeColorEmissive);
            metalWall[currentEnvironment].SetColor("_EmissionColor", originalMetalWallColorEmissive);
            roadDashLine[currentEnvironment].SetColor("_EmissionColor", originalroadDashLineColorEmissive);
        }
    }


    /*---------------------------------------- OTHER FUNCTIONS ----------------------------------------*/
    public IEnumerator LerpMotionBlur(float startIntensity, float targetIntensity, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Lerp Motion Blur Intensity.
            motionBlur.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, t);

            yield return null;
        }

        motionBlur.intensity.value = targetIntensity;
    }

    public IEnumerator FlashScreen()
    {
        // Temporarily change the 4th value of lift to simulate the lightning flash.
        Vector4 modifiedLift = liftGammaGain.lift.value;
        modifiedLift.w = 0.29f;
        liftGammaGain.lift.value = modifiedLift;
        inScreenFlash = true;

        yield return new WaitForSeconds(0.1f);

        // Revert lift value back to its original after the flash.
        liftGammaGain.lift.value = originalLift;
        inScreenFlash = false;
    }
}
