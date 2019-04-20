using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;

public class ObstacleBeaconScript : MonoBehaviour {

    [Tooltip("Parent object of spawned obstacle beacons.")]
    public GameObject obstacleBeaconManager;

    [Tooltip("Choose whether to refresh obstacle beacons based on distance (true) or time (false).")]
    public bool distanceRefresh = true;

    [Tooltip("Distance in meters to trigger beacon refresh when obstacle mode is on and refresh mode is set to distance.")]
    public float obstacleRefreshDistance = 2;


    [Tooltip("Time in seconds between beacon refresh when obstacle mode is on and refresh mode is set to time.")]
    public float obstacleRefreshTime = 8;

    [Tooltip("Obstacle beacon prefab to use.")]
    public GameObject obstacleBeacon;

    [Tooltip("Wall beacon prefab to use.")]
    public GameObject wallBeacon;

    [Tooltip("This object will be the parent for all spawned obstaclce and wall beacons.")]
    public GameObject beaconManager;

    [Tooltip("How far beacons should shoot.")]
    public float depth = 10;

    [Tooltip("Maximum number of obstacle beacons to shoot out per command.")]
    public int maxBeacons = 20;

    [Tooltip("Amount to deviate from center of gaze when spherecasting; smaller results in tighter spray.")]
    public float deviation = 0.2f;


    private Vector3 startPos;
    private float currentDistance;
    private float time = 0;
    private bool obstacleMode = false;


    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        //Debug.Log("Time: " + time);
        if (obstacleMode)
        {
            if (distanceRefresh)
            {
                //Refresh based on distance
                currentDistance = Vector3.Distance(Camera.main.gameObject.transform.position, startPos);
                if (currentDistance >= obstacleRefreshDistance)
                {
                    startPos = Camera.main.gameObject.transform.position;
                    DeleteBeacons();
                    ConeShot();
                    Debug.Log("Distance refresh limit reached. New beacons placed.");
                }
            }

            else
            {
                //Refresh based on time
                time += Time.deltaTime;
                if (time >= obstacleRefreshTime)
                {
                    DeleteBeacons();
                    ConeShot();
                    time = 0;
                    Debug.Log("Obstacle refresh time reached. New beacons placed.");
                }
            }


        }

    }

    public void ObstaclesOn ()
    {
        if (!obstacleMode)
        {
            //Toggle on obstacle mode
            obstacleMode = true;

            //Shoot cone of beacons
            ConeShot();

            //Set time or distance parameter
            if (distanceRefresh)
            {
                startPos = Camera.main.gameObject.transform.position;
            }

            else { time = 0; }
        }
    }

    public void ObstaclesOff ()
    {
        //Toggle off obstacle mode
        obstacleMode = false;
        DeleteBeacons();
        if (!distanceRefresh)
        {
            time = 0;
        }
    }

    public void SingleShot()
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
            //Debug.Log("Beacon hit at location: " + hit.point);
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
            //Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
            //Debug.Log("Did not Hit");
        }
    }

    public void ConeShot()
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
                    //Debug.Log("Hit floor or ceiling");
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

    public void DeleteBeacons()
    {
        foreach (Transform beacon in beaconManager.transform)
        {
            GameObject.Destroy(beacon.gameObject);
        }
    }

}
