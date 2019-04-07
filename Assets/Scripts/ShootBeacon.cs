using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;

public class ShootBeacon : MonoBehaviour {

    public GameObject beacon;
    public GameObject beaconManager;

	// Use this for initialization
	void Start () {
        //Debug.Log("ShootBeacon OnStart Triggered");
    }


    // Update is called once per frame
    void Update () {

	}

    public void SingleShot ()
    {
        // Do a raycast straight out from the camera.
        var headPosition = Camera.main.transform.position;
        var gazeDirection = Camera.main.transform.forward;



        RaycastHit hitInfo;

        if (Physics.Raycast(headPosition, gazeDirection, out hitInfo,
            30.0f))
        {
            //Debug
            //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hitInfo.distance, Color.yellow);
            //Debug.Log("Did Hit");
            Debug.Log("Beacon hit at location: " + hitInfo.point);
            //Debug.Log("Hit transform: " + hitInfo.transform);

            //Instantiate Beacon
            Instantiate(beacon, hitInfo.point, Quaternion.identity, beaconManager.transform);

        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
            Debug.Log("Did not Hit");
        }
    }
}



/* Backup Script
 * 
 * using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;

public class ShootBeacon : MonoBehaviour, IInputClickHandler {

    public GameObject beacon; 

	// Use this for initialization
	void Start () {
        Debug.Log("ShootBeacon OnStart Triggered");
    }


    public void OnInputClicked(InputClickedEventData eventData)
    {
        Debug.Log("OnInputClicked Triggered");

        shootBeacon();

        eventData.Use(); // Mark the event as used, so it doesn't fall through to other handlers.
    }

    // Update is called once per frame
    void Update () {

	}

    public void shootBeacon ()
    {
        // Do a raycast into the world that will only hit the Spatial Mapping mesh.
        var headPosition = Camera.main.transform.position;
        var gazeDirection = Camera.main.transform.forward;



        RaycastHit hitInfo;

        if (Physics.Raycast(headPosition, gazeDirection, out hitInfo,
            30.0f))
        {
            //Debug
            //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hitInfo.distance, Color.yellow);
            //Debug.Log("Did Hit");
            Debug.Log("Beacon hit at location: " + hitInfo.point);
            //Debug.Log("Hit transform: " + hitInfo.transform);

            //Instantiate Beacon
            Instantiate(beacon, hitInfo.point, Quaternion.identity);

        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
            Debug.Log("Did not Hit");
        }
    }
}
*/