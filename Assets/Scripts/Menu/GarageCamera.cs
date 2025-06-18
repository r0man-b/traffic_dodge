using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GarageCamera : MonoBehaviour
{
    public Camera cam;
    public Transform target;
    public Vector3 previousPosition;
    private float distanceToTarget = 5.14f;
    private bool ignoreTouch = false;
    public float screenRatioToIgnore;

    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        if (Input.GetAxis("Mouse ScrollWheel") > 0f) // forward
        {
            distanceToTarget -= 100 * Time.deltaTime;
        }
        if (Input.GetAxis("Mouse ScrollWheel") < 0f) // backwards
        {
            distanceToTarget += 100 * Time.deltaTime;
        }
        distanceToTarget = Mathf.Clamp(distanceToTarget, 2.7f, 7f); //i limited the zoom and zoomout distance inside (12,25).

        // Pinch to Zoom
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            // Calculate the distance between the two touches in the current and previous frames
            float pinchDistance = Vector2.Distance(touch0.position, touch1.position);
            float previousPinchDistance = Vector2.Distance(touch0.position - touch0.deltaPosition, touch1.position - touch1.deltaPosition);

            // Find out how much distance they moved compared to the last frame
            float deltaDistance = pinchDistance - previousPinchDistance;

            // Modify the camera's distance based on the change in the distance between touches
            distanceToTarget -= deltaDistance * Time.deltaTime / 5;

            // Ensure the distance stays within the desired limits
            distanceToTarget = Mathf.Clamp(distanceToTarget, 2.7f, 7f);
            cam.transform.position = target.position;
            cam.transform.Translate(new Vector3(0, 0, -distanceToTarget));
        }

        else
        {
            if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                if (Input.touchCount > 0)
                {
                    if (Input.GetTouch(0).position.y <= Screen.height / screenRatioToIgnore)
                    {
                        // Ignore the touch if it originated in the bottom 1/8th of the screen
                        ignoreTouch = true;
                    }
                    else
                    {
                        // Proceed with camera movement logic
                        ignoreTouch = false;
                        previousPosition = cam.ScreenToViewportPoint(Input.GetTouch(0).position);
                    }
                }
                else
                {
                    if (Input.mousePosition.y <= Screen.height / screenRatioToIgnore)
                    {
                        // Ignore the touch if it originated in the bottom 1/8th of the screen
                        ignoreTouch = true;
                    }
                    else
                    {
                        // Proceed with camera movement logic
                        // Your existing code to handle the start of a touch or mouse click
                        ignoreTouch = false;
                        previousPosition = cam.ScreenToViewportPoint(Input.mousePosition);
                    }
                }
            }

            else if ((Input.GetMouseButton(0) && !ignoreTouch) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved && !ignoreTouch))
            {
                Vector3 direction;
                Vector3 newPosition;
                if (Input.touchCount > 0)
                {
                    direction = previousPosition - cam.ScreenToViewportPoint(Input.GetTouch(0).position);
                    newPosition = cam.ScreenToViewportPoint(Input.GetTouch(0).position);
                }
                else
                {
                    direction = previousPosition - cam.ScreenToViewportPoint(Input.mousePosition);
                    newPosition = cam.ScreenToViewportPoint(Input.mousePosition);
                }
                if (Input.touchCount == 1 || Input.GetMouseButton(0))
                    direction.Scale(new Vector3(-360, 90, 0)); // x is half, y is double of the original scale
                else
                    direction.Scale(new Vector3(0, 0, 0)); // x is half, y is double of the original scale
                                                           //i limited the angles between  10<angle<90 degree in the x direction that means vertical movement
                var desiredQ = Quaternion.Euler(Mathf.Clamp(cam.transform.eulerAngles.x + direction.y, 0, 35),
                                                cam.transform.eulerAngles.y + direction.x, 0);
                //let assume movementspeed= 30.0f;
                cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, desiredQ, Time.deltaTime * 1200.0f);
                cam.transform.position = target.position;
                cam.transform.Translate(new Vector3(0, 0, -distanceToTarget));

                float clampedY = Mathf.Clamp(cam.transform.position.y, 0, 4f);
                cam.transform.position = new Vector3(cam.transform.position.x, clampedY, cam.transform.position.z);

                previousPosition = newPosition;
            }
            else if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
            {
                // Reset the ignore flag when the touch or click ends
                ignoreTouch = false;
            }
            else
            {
                cam.transform.position = target.position;
                cam.transform.Translate(new Vector3(0, 0, -distanceToTarget));
            }
        }
    }

    public void SetCameraPosition(int preset)
    {
        Vector3 newPos = Vector3.zero;
        Quaternion newRot = Quaternion.identity;

        switch (preset)
        {
            case 0: // DEFAULT
                newPos = new Vector3(-2.38209987f, 0.887270451f, 4.7662282f);
                newRot = new Quaternion(0.0189268328f, 0.969958067f, -0.0802059025f, 0.228888839f);
                break;
            case 1: // EXHAUSTS
                newPos = new Vector3(0.00116867165f, 0.275417805f, -3.35446215f);
                newRot = new Quaternion(-0.0409491248f, 0.000174050452f, -7.1331965e-06f, -0.999161243f);
                break;
            case 2: // FRONT SPLITTERS
                newPos = new Vector3(0.0209209751f, 0.689357638f, 3.94207573f);
                newRot = new Quaternion(0.000229400466f, -0.996252596f, 0.086451076f, 0.00264358567f);
                break;
            case 3: // FRONT WHEELS
                newPos = new Vector3(-2.29049325f, 0.307607889f, 3.38278055f);
                newRot = new Quaternion(-0.0110159721f, -0.955369055f, 0.0359171554f, -0.293016464f);
                break;
            case 4: // REAR SPLITTERS
                newPos = new Vector3(-0.0159216411f, 0.440565914f, -3.76867294f);
                newRot = new Quaternion(0.0581534915f, 0.00210877811f, -0.000122840982f, 0.99830544f);
                break;
            case 5: // REAR WHEELS
                newPos = new Vector3(-2.37483048f, 0.311843455f, -3.32371998f);
                newRot = new Quaternion(-0.0362688005f, -0.305026591f, 0.0116258506f, -0.951581955f);

                break;
            case 6: // SIDESKIRTS
                newPos = new Vector3(-2.04558563f, 0.601261377f, -1.6565218f);
                newRot = new Quaternion(-0.101135269f, -0.427794784f, 0.0482383296f, -0.896903753f);
                break;
            case 7: // SPOILERS
                newPos = new Vector3(0.0694983676f, 1.46753013f, -3.76027918f);
                newRot = new Quaternion(-0.184938163f, 0.00908053201f, -0.00170888891f, -0.982706785f);
                break;
            case 8: // SUSPENSIONS
                newPos = new Vector3(-4.19782162f, 0.612690032f, 0.0521764755f);
                newRot = new Quaternion(-0.0508729741f, -0.709620357f, 0.0515092276f, -0.700854957f);
                break;
            case 9: // RIM COLOR
                newPos = new Vector3(-4.19782162f, 0.612690032f, 0.0521764755f);
                newRot = new Quaternion(-0.0508729741f, -0.709620357f, 0.0515092276f, -0.700854957f);
                break;
            case 10: // RIM COLOR
                newPos = new Vector3(-4.19782162f, 0.612690032f, 0.0521764755f);
                newRot = new Quaternion(-0.0508729741f, -0.709620357f, 0.0515092276f, -0.700854957f);
                break;
        }

        cam.transform.SetPositionAndRotation(newPos, newRot);
        distanceToTarget = Vector3.Distance(target.position, newPos);
    }
}
