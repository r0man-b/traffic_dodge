using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MaxOutTheEmmission : MonoBehaviour
{
    public Material maxThisOut;
    public bool isAnimated = false;
    public float desiredIntensity = 1.0f; // New public field for desired intensity

    private Color lastEmissionColor;
    private Vector2 currentOffset;
    private int currentSceneBuildIndex;
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

        // Use the new desiredIntensity field instead of intensityMultiplier
        SetEmissionIntensity(maxThisOut.GetColor("_EmissionColor"), desiredIntensity);
        currentOffset = maxThisOut.GetTextureOffset("_BaseMap");
        //Debug.Log("The current emission intensity of " + maxThisOut.name + " is " + maxThisOut.GetColor("_EmissionColor"));
    }

    void FixedUpdate()
    {
        if (isAnimated)
        {
            currentOffset.y += yOffsetSpeed * Time.fixedDeltaTime;
            maxThisOut.SetTextureOffset("_BaseMap", currentOffset);
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
            SetEmissionIntensity(currentEmissionColor, desiredIntensity);
        }
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
}
