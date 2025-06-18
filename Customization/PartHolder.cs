using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartHolder : MonoBehaviour
{
    public CarPart[] partArray;

    public CarPart[] GetPartArray()
    {
        // Sort the partArray by price in place.
        Array.Sort(partArray, (part1, part2) => part1.price.CompareTo(part2.price));

        // Return the sorted array.
        return partArray;
    }

    public void ActivatePart(int partIndex)
    {
        partArray[partIndex].gameObject.SetActive(true);
    }

    public void DeactivatePart(int partIndex)
    {
        partArray[partIndex].gameObject.SetActive(false);
    }

    public void SetSuspensionHeight(int index)
    {
        transform.localPosition = new Vector3(transform.localPosition.x, partArray[index].suspensionHeight, transform.localPosition.z);
    }
}
