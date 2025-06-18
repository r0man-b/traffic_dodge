using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CarCollection", menuName = "Garage/CarCollection")]
public class CarCollection : ScriptableObject
{
    public List<CarType> carTypes = new();

    [System.Serializable]
    public class CarType
    {
        public List<ScriptableObject> items = new(); // Variations of each car.
    }
}