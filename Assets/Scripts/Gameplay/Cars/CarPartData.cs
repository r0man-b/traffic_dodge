using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Car Part", menuName = "Car Part Scriptable Object")]
public class CarPartData : ScriptableObject
{
    [Header("Attributes")]
    public float price;
    public Vector3[] exhaustFlamePositions;
    public Quaternion[] exhaustFlameRotations;
    public Vector3[] exhaustFlameScales;
}
