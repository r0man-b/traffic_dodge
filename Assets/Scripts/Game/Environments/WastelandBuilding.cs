using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WastelandBuilding : MonoBehaviour
{
    public int buildingType; // 0 = Wasteland Building, 1 = Wasteland Skyscraper
    public float minXPosition;
    public float maxXPosition;
    public float minYPosition;
    public float maxYPosition;
    public bool rotateableBuilding; // Determines if the building can appear tilted over when spawned.
    public List<Vector3> rotations;
    public Vector3 firePosition;
    public Vector3 fireScale;
}
