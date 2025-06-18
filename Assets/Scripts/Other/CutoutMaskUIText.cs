using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

public class CutoutMaskUIText : TextMeshProUGUI
{
    public override Material materialForRendering
    {
        get
        {
            Material material = new Material(base.materialForRendering);

            // Set stencil comparison function to NotEqual
            material.SetInt("_StencilComp", (int)CompareFunction.NotEqual);

            // Set Softness to 0
            material.SetFloat("_OutlineSoftness", 0f);

            // Set Dilate (Face Dilate in TMP) to 0.35
            material.SetFloat("_FaceDilate", -0.6f);

            // Set Outline Thickness to 1
            material.SetFloat("_OutlineWidth", 1f);

            // Set Outline Color to RGB (84, 81, 0)
            material.SetColor("_OutlineColor", new Color(69f / 255f, 69f / 255f, 0f / 255f));

            // Disable Glow by setting key parameters to neutral values
            material.SetFloat("_GlowPower", 0f);
            material.SetFloat("_GlowOuter", 0f);
            material.SetColor("_GlowColor", Color.clear);

            // Disable Underlay by setting key parameters to neutral values
            material.SetFloat("_UnderlaySoftness", 0f);
            material.SetFloat("_UnderlayOffsetX", 0f);
            material.SetFloat("_UnderlayOffsetY", 0f);
            material.SetFloat("_UnderlayDilate", 0f);
            material.SetColor("_UnderlayColor", Color.clear);

            return material;
        }
    }
}
