using System.Collections;
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
        //SpatialMapping.Instance.DrawVisualMeshes = true;
    }


    public void OnInputClicked(InputClickedEventData eventData)
    {
        Debug.Log("OnInputClicked Triggered");

        
        // Do a raycast into the world that will only hit the Spatial Mapping mesh.
        var headPosition = Camera.main.transform.position;
        var gazeDirection = Camera.main.transform.forward;


        
        RaycastHit hitInfo;

        if (Physics.Raycast(headPosition, gazeDirection, out hitInfo,
            30.0f))
        {
            //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hitInfo.distance, Color.yellow);
            Debug.Log("Did Hit");
            Debug.Log("Hit point: " + hitInfo.point);
            Debug.Log("Hit transform: " + hitInfo.transform);

            //Instantiate Beacon
            GameObject beaconClone = Instantiate(beacon, hitInfo.point, Quaternion.identity);

        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
            Debug.Log("Did not Hit");
        }


        eventData.Use(); // Mark the event as used, so it doesn't fall through to other handlers.
    }

    // Update is called once per frame
    void Update () {

	}
}
