using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentLighting : MonoBehaviour
{
    private int currentEnvironment = 0;

    [Header("Environment Skyboxes")]
    public Material scifiSkybox;
    public Material wastelandSkybox;
    public Material underwaterSkybox;
    public Material spaceSkybox;

    [Header("Environment Fog Color")]
    public Color scifiFog;
    public Color wastelandFog;
    public Color underwaterFog;
    public Color spaceFog;

    [Header("Environment Fog Start")]
    public float scifiFogStart;
    public float wastelandFogStart;
    public float underwaterFogStart;
    public float spaceFogStart;

    [Header("Environment Fog End")]
    public float scifiFogEnd;
    public float wastelandFogEnd;
    public float underwaterFogEnd;
    public float spaceFogEnd;

    [Header("Environment Sun Settings")]
    public Light directionalLight;
    public Color scifiSunColor;
    public Color wastelandSunColor;
    public Color underwaterSunColor;
    public Color spaceSunColor;

    [Header("Environment Sun Rotation (Euler Angles)")]
    public Vector3 scifiSunRotation;
    public Vector3 wastelandSunRotation;
    public Vector3 underwaterSunRotation;
    public Vector3 spaceSunRotation;

    void Start()
    {
        RenderSettings.fog = true;

        switch (currentEnvironment)
        {
            case 0: // Sci-Fi
                RenderSettings.skybox = scifiSkybox;
                RenderSettings.fogColor = scifiFog;
                RenderSettings.fogStartDistance = scifiFogStart;
                RenderSettings.fogEndDistance = scifiFogEnd;
                directionalLight.color = scifiSunColor;
                directionalLight.transform.rotation = Quaternion.Euler(scifiSunRotation);
                break;
            case 1: // Wasteland
                RenderSettings.skybox = wastelandSkybox;
                RenderSettings.fogColor = wastelandFog;
                RenderSettings.fogStartDistance = wastelandFogStart;
                RenderSettings.fogEndDistance = wastelandFogEnd;
                directionalLight.color = wastelandSunColor;
                directionalLight.transform.rotation = Quaternion.Euler(wastelandSunRotation);
                break;
            case 2: // Underwater
                RenderSettings.skybox = underwaterSkybox;
                RenderSettings.fogColor = underwaterFog;
                RenderSettings.fogStartDistance = underwaterFogStart;
                RenderSettings.fogEndDistance = underwaterFogEnd;
                directionalLight.color = underwaterSunColor;
                directionalLight.transform.rotation = Quaternion.Euler(underwaterSunRotation);
                break;
            case 3: // Space
                RenderSettings.skybox = spaceSkybox;
                RenderSettings.fogColor = spaceFog;
                RenderSettings.fogStartDistance = spaceFogStart;
                RenderSettings.fogEndDistance = spaceFogEnd;
                directionalLight.color = spaceSunColor;
                directionalLight.transform.rotation = Quaternion.Euler(spaceSunRotation);
                break;
            default: // Fallback to Sci-Fi
                RenderSettings.skybox = scifiSkybox;
                RenderSettings.fogColor = scifiFog;
                RenderSettings.fogStartDistance = scifiFogStart;
                RenderSettings.fogEndDistance = scifiFogEnd;
                directionalLight.color = scifiSunColor;
                directionalLight.transform.rotation = Quaternion.Euler(scifiSunRotation);
                break;
        }
    }
}
