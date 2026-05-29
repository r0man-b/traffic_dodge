using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ApplyGammaSettings : MonoBehaviour
{
    private Volume postProcessingVolume;
    private LiftGammaGain liftGammaGain;
    private Vignette vignette;
    private FilmGrain filmGrain;

    void Awake()
    {
        // Get the Post Processing Volume component on the same GameObject
        postProcessingVolume = GetComponent<Volume>();

        if (postProcessingVolume != null && postProcessingVolume.sharedProfile != null)
        {
            postProcessingVolume.profile = Instantiate(postProcessingVolume.sharedProfile);
        }

        if (postProcessingVolume != null)
        {
            // Apply Lift Gamma Gain
            if (postProcessingVolume.profile.TryGet(out liftGammaGain))
            {
                // Load the saved gamma value from SaveData
                float savedGamma = SaveManager.Instance.SaveData.GammaValue;

                // Apply the gamma setting
                Vector4 gamma = liftGammaGain.gamma.value;
                gamma.w = savedGamma * 5f; // Modify only the W component (brightness)
                liftGammaGain.gamma.value = gamma;

                Debug.Log($"Gamma Loaded and Applied: {savedGamma}");
            }

            // Apply Vignette
            if (postProcessingVolume.profile.TryGet(out vignette))
            {
                vignette.active = SaveManager.Instance.SaveData.VignetteEnabled;
                Debug.Log($"Vignette Loaded and Applied: {vignette.active}");
            }

            // Apply Film Grain
            if (postProcessingVolume.profile.TryGet(out filmGrain))
            {
                filmGrain.active = SaveManager.Instance.SaveData.FilmGrainEnabled;
                Debug.Log($"Film Grain Loaded and Applied: {filmGrain.active}");
            }
        }
        else
        {
            Debug.LogError("Post Processing Volume not found!");
        }
    }
}
