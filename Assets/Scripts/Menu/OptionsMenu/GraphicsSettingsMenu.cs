using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Settings
{
	public enum GraphicsPreset { Trash, Low, Medium, High, Insane }

	[System.Serializable]
	public struct GraphicsPresetSettings
	{
		public float renderScale;
		public int antiAliasing; // Must match aaValues
		public bool useLowPolyTraffic;
		public bool shadowsEnabled;
	}

	public class GraphicsSettingsMenu : MonoBehaviour
	{
		public Slider renderScaleSlider;       // Slider for URP render scale (0 to 4)
		public Slider aaSlider;                // Slider for anti-aliasing (0 to 3)
		public Slider trafficQualitySlider;    // Slider for traffic quality (0 or 1)
		public Toggle shadowsEnabledToggle;    // Toggle to enable/disable shadows
		public Slider fpsSlider;               // Slider to control Framerate

		public Button revertChangesButton;
		public Button acceptChangesButton;

		private float[] renderScaleValues = { 0.5f, 0.75f, 1f, 1.5f, 2f };
		private int[] aaValues = { 0, 2, 4, 8 }; // Corresponding to Disabled, 2x, 4x, 8x
		private int[] frameRateValues = { 30, 60, 120 };

		private float originalRenderScale;
		private int originalAASetting;
		private bool originalLowPolyTraffic;
		private bool originalShadowsEnabled;
		private int originalFrameRate;

		public Camera sceneCamera;
		private UniversalAdditionalCameraData cameraData;

		public GameObject applySettingsPopup;
		public Button yesButton;
		public Button noButton;
		private bool hasChanged = false;

		private Dictionary<GraphicsPreset, GraphicsPresetSettings> presets = new()
		{
			[GraphicsPreset.Trash] = new GraphicsPresetSettings { renderScale = 0.5f, antiAliasing = 0, useLowPolyTraffic = true, shadowsEnabled = false },
			[GraphicsPreset.Low] = new GraphicsPresetSettings { renderScale = 0.75f, antiAliasing = 0, useLowPolyTraffic = true, shadowsEnabled = true },
			[GraphicsPreset.Medium] = new GraphicsPresetSettings { renderScale = 1f, antiAliasing = 2, useLowPolyTraffic = true, shadowsEnabled = true },
			[GraphicsPreset.High] = new GraphicsPresetSettings { renderScale = 1.5f, antiAliasing = 4, useLowPolyTraffic = false, shadowsEnabled = true },
			[GraphicsPreset.Insane] = new GraphicsPresetSettings { renderScale = 2f, antiAliasing = 8, useLowPolyTraffic = false, shadowsEnabled = true },
		};

		private void Awake()
		{
			// Load and store original settings
			originalRenderScale = SaveManager.Instance.SaveData.renderScale;
			originalAASetting = SaveManager.Instance.SaveData.aaSettings;
			originalLowPolyTraffic = SaveManager.Instance.SaveData.UseLowPolyTraffic;
			originalShadowsEnabled = SaveManager.Instance.SaveData.ShadowsEnabled;
			originalFrameRate = SaveManager.Instance.SaveData.frameRate;

			// Set UI values
			renderScaleSlider.value = System.Array.IndexOf(renderScaleValues, originalRenderScale);
			aaSlider.value = System.Array.IndexOf(aaValues, originalAASetting);
			trafficQualitySlider.value = originalLowPolyTraffic ? 0 : 1;
			shadowsEnabledToggle.isOn = originalShadowsEnabled;
			fpsSlider.value = System.Array.IndexOf(frameRateValues, originalFrameRate);

			// Deactivate revert/accept buttons initially.
			revertChangesButton.gameObject.SetActive(false);
			acceptChangesButton.gameObject.SetActive(false);

			// Cache the UniversalAdditionalCameraData component.
			if (sceneCamera != null)
			{
				cameraData = sceneCamera.GetComponent<UniversalAdditionalCameraData>();
			}
		}


		private void Start()
		{
			// Add listeners
			renderScaleSlider.onValueChanged.AddListener(delegate { OnSliderChanged(); });
			aaSlider.onValueChanged.AddListener(delegate { OnSliderChanged(); });
			trafficQualitySlider.onValueChanged.AddListener(delegate { OnSliderChanged(); });
			shadowsEnabledToggle.onValueChanged.AddListener(delegate { OnSliderChanged(); });
			fpsSlider.onValueChanged.AddListener(delegate { OnSliderChanged(); });

			revertChangesButton.onClick.AddListener(RevertChanges);
			acceptChangesButton.onClick.AddListener(ApplyChanges);
		}


		private void OnSliderChanged()
		{
			hasChanged =
				renderScaleValues[(int)renderScaleSlider.value] != originalRenderScale ||
				aaValues[(int)aaSlider.value] != originalAASetting ||
				(trafficQualitySlider.value == 0) != originalLowPolyTraffic ||
				shadowsEnabledToggle.isOn != originalShadowsEnabled ||
				frameRateValues[(int)fpsSlider.value] != originalFrameRate;


			revertChangesButton.gameObject.SetActive(hasChanged);
			acceptChangesButton.gameObject.SetActive(hasChanged);
		}


		public void RevertChanges()
		{
			renderScaleSlider.value = System.Array.IndexOf(renderScaleValues, originalRenderScale);
			aaSlider.value = System.Array.IndexOf(aaValues, originalAASetting);
			trafficQualitySlider.value = originalLowPolyTraffic ? 0 : 1;
			shadowsEnabledToggle.isOn = originalShadowsEnabled;
			fpsSlider.value = System.Array.IndexOf(frameRateValues, originalFrameRate);

			revertChangesButton.gameObject.SetActive(false);
			acceptChangesButton.gameObject.SetActive(false);
			hasChanged = false;
		}


		public void ApplyChanges()
		{
			float newRenderScale = renderScaleValues[(int)renderScaleSlider.value];
			int newAASetting = aaValues[(int)aaSlider.value];
			bool useLowPolyTraffic = trafficQualitySlider.value == 0;
			bool enableShadows = shadowsEnabledToggle.isOn;
			int newFrameRate = frameRateValues[(int)fpsSlider.value];

			SaveManager.Instance.SaveData.renderScale = newRenderScale;
			SaveManager.Instance.SaveData.aaSettings = newAASetting;
			SaveManager.Instance.SaveData.UseLowPolyTraffic = useLowPolyTraffic;
			SaveManager.Instance.SaveData.ShadowsEnabled = enableShadows;
			SaveManager.Instance.SaveData.frameRate = newFrameRate;
			SaveManager.Instance.SaveGame();

			UniversalRenderPipelineAsset urpAsset = GraphicsSettingsHelper.GetActiveURPAsset();
			if (urpAsset != null)
			{
				urpAsset.renderScale = newRenderScale;
				urpAsset.msaaSampleCount = newAASetting;
			}
			if (cameraData != null) cameraData.renderShadows = enableShadows;
			Application.targetFrameRate = newFrameRate;

			// Update originals
			originalRenderScale = newRenderScale;
			originalAASetting = newAASetting;
			originalLowPolyTraffic = useLowPolyTraffic;
			originalShadowsEnabled = enableShadows;
			originalFrameRate = newFrameRate;

			bool matchedAnyPreset = false;

			foreach (GraphicsPreset preset in System.Enum.GetValues(typeof(GraphicsPreset)))
			{
				if (MatchesPreset(preset))
				{
					SaveManager.Instance.SaveData.graphicsPresetIndex = (int)preset;
					SaveManager.Instance.SaveData.isCustomGraphics = false;
					matchedAnyPreset = true;
					break;
				}
			}

			if (!matchedAnyPreset)
			{
				SaveManager.Instance.SaveData.isCustomGraphics = true;
			}

			revertChangesButton.gameObject.SetActive(false);
			acceptChangesButton.gameObject.SetActive(false);
			hasChanged = false;
		}

		public void DisplayPopup(System.Action<bool> onChoiceMade)
		{
			if (!hasChanged)
			{
				onChoiceMade?.Invoke(true); // Default to 'continue' if no changes
				return;
			}

			revertChangesButton.gameObject.SetActive(false);
			acceptChangesButton.gameObject.SetActive(false);
			applySettingsPopup.SetActive(true);

			// Remove previous listeners
			yesButton.onClick.RemoveAllListeners();
			noButton.onClick.RemoveAllListeners();

			// YES: Apply changes
			yesButton.onClick.AddListener(() =>
			{
				ApplyChanges();
				applySettingsPopup.SetActive(false);
				onChoiceMade?.Invoke(true);
			});

			// NO: Revert changes
			noButton.onClick.AddListener(() =>
			{
				RevertChanges();
				applySettingsPopup.SetActive(false);
				onChoiceMade?.Invoke(true);
			});
		}

		public void ApplyPreset(GraphicsPreset preset, System.Action<bool> onChoiceMade)
		{
			var selected = presets[preset];

			// Apply preset values to UI controls
			renderScaleSlider.value = System.Array.IndexOf(renderScaleValues, selected.renderScale);
			aaSlider.value = System.Array.IndexOf(aaValues, selected.antiAliasing);
			trafficQualitySlider.value = selected.useLowPolyTraffic ? 0 : 1;
			shadowsEnabledToggle.isOn = selected.shadowsEnabled;

			// Mark UI as changed
			OnSliderChanged();

			// Signal that the preset was loaded (but not saved/applied yet)
			onChoiceMade?.Invoke(true);
		}


		public bool MatchesPreset(GraphicsPreset preset)
		{
			var presetSettings = presets[preset];

			return
				Mathf.Approximately(renderScaleValues[(int)renderScaleSlider.value], presetSettings.renderScale) &&
				aaValues[(int)aaSlider.value] == presetSettings.antiAliasing &&
				(trafficQualitySlider.value == 0) == presetSettings.useLowPolyTraffic &&
				shadowsEnabledToggle.isOn == presetSettings.shadowsEnabled;
		}
	}

	public static class GraphicsSettingsHelper
	{
		public static UniversalRenderPipelineAsset GetActiveURPAsset()
		{
			return GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
		}

	}
}
