using System;
using UnityEngine;

[ExecuteAlways]
public class MetalSphereLayout : MonoBehaviour
{
    [Serializable]
    public struct RefLayout
    {
        [Header("Reference resolution (pixels)")]
        public Vector2 resolution;                    // e.g. (2960,1440) or (2100,1800)

        [Header("Per-sphere authoring at this resolution")]
        public Vector2 referencePixelPosition;        // center in pixels in the screenshot
        public float referencePixelDiameter;         // visual diameter in pixels
    }

    [Serializable]
    public class SphereLayout
    {
        public Transform sphere;

        [Header("Layout A (e.g. 2960×1440)")]
        public RefLayout layoutA;

        [Header("Layout B (e.g. 2100×1800)")]
        public RefLayout layoutB;

        [Header("3D placement")]
        [Tooltip("World-space distance from the camera along its forward axis.")]
        public float depthFromCamera = 2.0f;

        [Tooltip("Extra overall scale after layout is computed.")]
        public float uniformScaleMultiplier = 1.0f;
    }

    [Header("Camera")]
    public Camera targetCamera;

    [Header("Blending")]
    [Tooltip("If blank, the script derives these from the two reference resolutions' aspect ratios.")]
    public float minAspect = 0f;  // smaller of the two (e.g. 2100/1800 ≈ 1.1667)
    public float maxAspect = 0f;  // larger of the two (e.g. 2960/1440 ≈ 2.0556)

    [Tooltip("Adjust the interpolation across aspect ratios. X=0 maps to minAspect, X=1 to maxAspect.")]
    public AnimationCurve blendCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Spheres")]
    public SphereLayout[] spheres;

    private int _lastW = -1, _lastH = -1, _lastFovHash = -1;

    private void Reset()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        ApplyLayout();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) ApplyIfChanged();
#endif
        if (Application.isPlaying) ApplyIfChanged();
    }

    private void OnValidate()
    {
        ApplyLayout();
    }

    private void ApplyIfChanged()
    {
        int fovHash = targetCamera ? Mathf.RoundToInt(targetCamera.fieldOfView * 1000f) : 0;
        if (Screen.width != _lastW || Screen.height != _lastH || fovHash != _lastFovHash)
            ApplyLayout();
    }

    public void ApplyLayout()
    {
        if (targetCamera == null || spheres == null) return;

        // Derive blend bounds from the two layouts if not provided.
        float arA = 0f, arB = 0f;
        if (spheres.Length > 0)
        {
            var aRes = spheres[0].layoutA.resolution;
            var bRes = spheres[0].layoutB.resolution;
            if (aRes.x > 0 && aRes.y > 0) arA = aRes.x / aRes.y;
            if (bRes.x > 0 && bRes.y > 0) arB = bRes.x / bRes.y;
        }
        float lo = minAspect > 0 ? minAspect : Mathf.Min(arA, arB);
        float hi = maxAspect > 0 ? maxAspect : Mathf.Max(arA, arB);
        if (lo <= 0 || hi <= 0 || hi <= lo) { lo = 1.0f; hi = 2.0f; }  // fallback

        float currentAR = (float)Screen.width / Mathf.Max(1, Screen.height);
        float tLinear = Mathf.InverseLerp(lo, hi, currentAR);
        float t = blendCurve != null ? Mathf.Clamp01(blendCurve.Evaluate(tLinear)) : tLinear;

        // Common FOV math (perspective)
        float fovRad = targetCamera.fieldOfView * Mathf.Deg2Rad;

        foreach (var s in spheres)
        {
            if (s == null || s.sphere == null) continue;

            // Convert both references to viewport space (0..1)
            Vector2 resA = s.layoutA.resolution;
            Vector2 resB = s.layoutB.resolution;

            Vector2 vpA = new Vector2(
                s.layoutA.referencePixelPosition.x / Mathf.Max(1f, resA.x),
                s.layoutA.referencePixelPosition.y / Mathf.Max(1f, resA.y));

            Vector2 vpB = new Vector2(
                s.layoutB.referencePixelPosition.x / Mathf.Max(1f, resB.x),
                s.layoutB.referencePixelPosition.y / Mathf.Max(1f, resB.y));

            // For size, use a fraction of screen height at each reference
            float fracA = s.layoutA.referencePixelDiameter / Mathf.Max(1f, resA.y);
            float fracB = s.layoutB.referencePixelDiameter / Mathf.Max(1f, resB.y);

            // Blend position and size fraction based on aspect ratio
            Vector2 vp = Vector2.Lerp(vpA, vpB, t);
            float frac = Mathf.Lerp(fracA, fracB, t);

            float d = Mathf.Max(0.001f, s.depthFromCamera);

            // Position
            Vector3 worldPos = targetCamera.ViewportToWorldPoint(new Vector3(vp.x, vp.y, d));
            s.sphere.position = worldPos;
            s.sphere.rotation = targetCamera.transform.rotation;

            // Scale: world height at depth d, then take the blended fraction
            float worldHeightAtD = 2f * d * Mathf.Tan(fovRad * 0.5f);
            float worldDiameter = Mathf.Max(0.0001f, frac * worldHeightAtD);
            float finalScale = worldDiameter * Mathf.Max(0.0001f, s.uniformScaleMultiplier);
            s.sphere.localScale = Vector3.one * finalScale;
        }

        _lastW = Screen.width;
        _lastH = Screen.height;
        _lastFovHash = Mathf.RoundToInt(targetCamera.fieldOfView * 1000f);
    }
}
