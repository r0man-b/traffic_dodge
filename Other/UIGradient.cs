using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
    public Color m_color1 = Color.white;  // Top Color
    public Color m_color2 = Color.white;  // Middle Color
    public Color m_color3 = Color.white;  // Bottom Color
    [Range(-180f, 180f)]
    public float m_angle = 0f;
    public bool m_ignoreRatio = true;

    // Factor to control how much of the gradient is dedicated to the middle color
    [Range(0.1f, 0.9f)] 
    public float middleColorWidth = 0.5f;  // Determines the width of the middle color

    public override void ModifyMesh(VertexHelper vh)
    {
        if (enabled)
        {
            Rect rect = graphic.rectTransform.rect;
            Vector2 dir = UIGradientUtils.RotationDir(m_angle);

            if (!m_ignoreRatio)
                dir = UIGradientUtils.CompensateAspectRatio(rect, dir);

            UIGradientUtils.Matrix2x3 localPositionMatrix = UIGradientUtils.LocalPositionMatrix(rect, dir);

            UIVertex vertex = default(UIVertex);
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vertex, i);
                Vector2 localPosition = localPositionMatrix * vertex.position;

                // Adjusting the blending based on middleColorWidth
                float lowerBound = (1f - middleColorWidth) / 2f; // Lower boundary for the middle color
                float upperBound = 1f - lowerBound;              // Upper boundary for the middle color

                // Calculate the gradient color with an expanded middle color
                float positionFactor = localPosition.y;
                if (positionFactor < lowerBound)
                {
                    // Blend between bottom color and middle color
                    vertex.color *= Color.Lerp(m_color3, m_color2, positionFactor / lowerBound);
                }
                else if (positionFactor > upperBound)
                {
                    // Blend between middle color and top color
                    vertex.color *= Color.Lerp(m_color2, m_color1, (positionFactor - upperBound) / (1f - upperBound));
                }
                else
                {
                    // Set the middle color region
                    vertex.color *= m_color2;
                }

                vh.SetUIVertex(vertex, i);
            }
        }
    }
}
