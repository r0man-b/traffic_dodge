using UnityEngine;

public class WheelRotator : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(1050 * Time.deltaTime, 0, 0);
    }
}
