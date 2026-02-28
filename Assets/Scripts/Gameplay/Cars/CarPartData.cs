using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Car Part", menuName = "Car Part Scriptable Object")]
public class CarPartData : ScriptableObject
{
    [Header("Attributes")]
    public float price;

    [Header("Exhaust Flames (Tube -> Flames)")]
    public ExhaustFlameTube[] exhaustFlameTubes;
}

[System.Serializable]
public class ExhaustFlameTube
{
    [Tooltip("All flames that belong to this tube (tube index maps to tube child index under ACTIVE_EXHAUST).")]
    public ExhaustFlameTransform[] flames;
}

[System.Serializable]
public class ExhaustFlameTransform
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}
