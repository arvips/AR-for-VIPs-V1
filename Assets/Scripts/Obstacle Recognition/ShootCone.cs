using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;

public class ShootCone : MonoBehaviour {

    [Tooltip("Obstacle beacon prefab to use.")]
    public GameObject obstacleBeacon;

    [Tooltip("Wall beacon prefab to use.")]
    public GameObject wallBeacon;

    [Tooltip("This object will be the parent for all spawned beacons.")]
    public GameObject beaconManager;

    [Tooltip("How far beacons should shoot.")]
    public float depth = 10;

    [Tooltip("Maximum number of obstacle beacons to shoot out per command.")]
    public int maxBeacons = 20;

    [Tooltip("Amount to deviate from center of gaze when spherecasting; smaller results in tighter spray.")]
    public float deviation = 0.2f;

    // Use this for initialization
    void Start () {
        //Debug.Log("ShootBeacon OnStart Triggered");
    }


    // Update is called once per frame
    void Update () {

	}

    public void SingleShot ()
    {
        // Do a single raycast straight out from the camera.
        var headPosition = Camera.main.transform.position;
        var gazeDirection = Camera.main.transform.forward;

        RaycastHit hit;

        if (Physics.Raycast(headPosition, gazeDirection, out hit,
            30.0f))
        {
            //Debug
            //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hitInfo.distance, Color.yellow);
            //Debug.Log("Did Hit");
            Debug.Log("Beacon hit at location: " + hit.point);
            //Debug.Log("Hit transform: " + hitInfo.transform);

            if (hit.transform.gameObject.tag == "Wall")
            {
                //If a wall is hit, instantiate a wall beacon
                Instantiate(wallBeacon, hit.point, Quaternion.identity, beaconManager.transform);

            }

            else
            {
                //Otherwise, instantiate an obstacle beacon
                Instantiate(obstacleBeacon, hit.point, Quaternion.identity, beaconManager.transform);
            }

        }
        else
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
            Debug.Log("Did not Hit");
        }
    }

    public void ConeShot ()
    {
        //Shoot a spray of beacons in a cone

        //Capture camera's location and orientation
        var headPosition = Camera.main.transform.position;
        var gazeDirection = Camera.main.transform.forward;

        //Uses spherecast
        float sphereRadius = 0.05f;

        //List of hits, if necessary
        //List<RaycastHit> coneCastHitList = new List<RaycastHit>();

        for (int i = 0; i < maxBeacons; i++)
        {
            //Instantiate variable to hold hit info
            RaycastHit hit;

            //Create random modifier to gaze direction
            //Returns random vector within (5*deviation) unit sphere
            Vector3 randomizer = Random.onUnitSphere * deviation;

            //Perform spherecast
            Physics.SphereCast(headPosition, sphereRadius, gazeDirection + randomizer, out hit, depth);
            

            //Add hit to coneCastHitList
            //coneCastHitList.Add(hit);

            if (hit.transform != null)
            {
                //Check whether hit item is a floor or ceiling; if not, instantiate beacon
                if (hit.transform.gameObject.tag == "Floor" || hit.transform.gameObject.tag == "Ceiling")
                {
                    Debug.Log("Hit floor or ceiling");
                }

                else if (hit.transform.gameObject.tag == "Wall")
                {
                    //If a wall is hit, instantiate a wall beacon
                    Instantiate(wallBeacon, hit.point, Quaternion.identity, beaconManager.transform);

                }

                else
                {
                    //Otherwise, instantiate an obstacle beacon
                    Instantiate(obstacleBeacon, hit.point, Quaternion.identity, beaconManager.transform);
                }
            }


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