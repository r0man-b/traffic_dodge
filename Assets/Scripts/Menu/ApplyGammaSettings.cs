using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ApplyGammaSettings : MonoBehaviour
{
    private Volume postProcessingVolume;
    private LiftGammaGain liftGammaGain;

    void Awake()
    {
        // Get the Post Processing Volume component on the same GameObject
        postProcessingVolume = GetComponent<Volume>();

        if (postProcessingVolume != null && postProcessingVolume.profile.TryGet(out liftGammaGain))
        {
            // Load the saved gamma value from SaveData
            float savedGamma = SaveManager.Instance.SaveData.GammaValue;

            // Apply the gamma setting
            Vector4 gamma = liftGammaGain.gamma.value;
            gamma.w = savedGamma * 5f; // Modify only the W component (brightness)
            liftGammaGain.gamma.value = gamma;

            Debug.Log($"Gamma Loaded and Applied: {savedGamma}");
        }
        else
        {
            Debug.LogError("Post Processing Volume or Lift Gamma Gain not found!");
        }
    }
}
