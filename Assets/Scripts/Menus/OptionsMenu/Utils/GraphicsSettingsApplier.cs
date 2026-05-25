using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GraphicsSettingsApplier : MonoBehaviour
{
    [SerializeField] private Camera sceneCamera;

    private void Awake()
    {
        ApplySavedGraphicsSettings();
    }

    public void ApplySavedGraphicsSettings()
    {
        var save = SaveManager.Instance.SaveData;

        UniversalRenderPipelineAsset urpAsset =
            GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;

        if (urpAsset != null)
        {
            urpAsset.renderScale = save.renderScale;
            urpAsset.msaaSampleCount = save.aaSettings;
        }

        if (sceneCamera != null &&
            sceneCamera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
        {
            cameraData.renderShadows = save.ShadowsEnabled;
        }

        Application.targetFrameRate = save.frameRate;
    }
}