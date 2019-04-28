using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;

public class ObstacleBeaconScript : MonoBehaviour {

    [Tooltip("Parent object of spawned obstacle beacons.")]
    public GameObject obstacleBeaconManager;

    [Tooltip("Choose a text manager to enable voice feedback.")]
    public GameObject textManager;

    [Tooltip("Choose whether all obstacle beacons produce sound or only those the user is looking at.")]
    public bool spotlightMode = true;

    [Tooltip("Choose whether obstacle beacon sound fall off logarthmically (proximity mode on) or linearly (proximity mode off).")]
    public bool proximityMode = true;

    [Tooltip("Choose the angle for ConeCasting")]
    public float coneCastAngle;

    [Tooltip("Size of the spotlight. All beacons within a spherecast of this size directed out from the user's vision will sonify.")]
    public float spotlightSize = 1;

    [Tooltip("Choose whether to refresh obstacle beacons based on distance (true) or time (false).")]
    public bool distanceRefresh = true;

    [Tooltip("Distance in meters to trigger beacon refresh when obstacle mode is on and refresh mode is set to distance.")]
    public float obstacleRefreshDistance = 2;


    [Tooltip("Time in seconds between beacon refresh when obstacle mode is on and refresh mode is set to time.")]
    public float obstacleRefreshTime = 8;

    [Tooltip("Logarithmic falloff obstacle beacon prefab to use.")]
    public GameObject obstacleBeaconLog;

    [Tooltip("Linear falloff obstacle beacon prefab to use.")]
    public GameObject obstacleBeaconLin;

    [Tooltip("Color of obstacle beacons.")]
    public Material obstacleMaterial;

    [Tooltip("Logarithmic falloff wall beacon prefab to use.")]
    public GameObject wallBeaconLog;

    [Tooltip("Linear falloff wall beacon prefab to use.")]
    public GameObject wallBeaconLin;

    [Tooltip("Color of wall beacons.")]
    public Material wallMaterial;

    [Tooltip("This object will be the parent for all spawned obstaclce and wall beacons.")]
    public GameObject beaconManager;

    [Tooltip("How far beacons should shoot.")]
    public float depth = 10;

    [Tooltip("Maximum number of obstacle beacons to shoot out per command.")]
    public int maxBeacons = 20;

    [Tooltip("Amount to deviate from center of gaze when spherecasting; smaller results in tighter spray.")]
    public float deviation = 0.2f;

    [HideInInspector]
    public GameObject obstacleBeacon;

    [HideInInspector]
    public GameObject wallBeacon;

    private Vector3 startPos;
    private float currentDistance;
    private float time = 0;
    public bool obstacleMode = false;

    private Physics physics;


    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        //If obstacle mode is on, calculate either distance or time and refresh beacons appropriately.
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

        //If spotlight mode is on, mute all obstacle beacons other than the ones the user is looking at.
        if (spotlightMode)
        {
            //Mutes and grays all beacons
            MuteAllObstacleBeacons();


            //Spherecast out from user

            //Capture camera's location and orientation
            var headPosition = Camera.main.transform.position;
            var gazeDirection = Camera.main.transform.forward;

            //Instantiate variable to hold hit info
            //RaycastHit[] spotlightHits = Physics.SphereCastAll(headPosition, spotlightSize, gazeDirection, depth);
            RaycastHit[] spotlightHits = physics.ConeCastAll(headPosition, spotlightSize, gazeDirection, depth, coneCastAngle);

            foreach (RaycastHit hit in spotlightHits)
            {
                if (hit.transform != null)
                {
                    if (hit.transform.gameObject.tag == "Obstacle Beacon" || hit.transform.gameObject.tag == "Wall Beacon")
                    {
                        hit.transform.gameObject.GetComponent<AudioSource>().volume = 1;
                        //Also restore beacons' color (red for obstacle, orange for wall)
                        if (hit.transform.gameObject.tag == "Obstacle Beacon")
                        {
                            hit.transform.gameObject.GetComponent<Renderer>().material = obstacleMaterial;
                        }

                        else //presuming wall beacon
                        {
                            hit.transform.gameObject.GetComponent<Renderer>().material = wallMaterial;
                        }
                    }
                }
            }

        }

        //Set logarithmic or linear falloff beacon type.
        if (proximityMode)
        {
            if (obstacleBeacon != obstacleBeaconLog)
            {
                obstacleBeacon = obstacleBeaconLog;
            }

            if (wallBeacon != wallBeaconLog)
            {
                wallBeacon = wallBeaconLog;
            }
        }

        else
        {
            if (obstacleBeacon != obstacleBeaconLin)
            {
                obstacleBeacon = obstacleBeaconLin;
            }

            if (wallBeacon != wallBeaconLin)
            {
                wallBeacon = wallBeaconLin;
            }
        }
    }

    #region OBSTACLE MODE

    public void ObstaclesOn ()
    {
        Debug.Log("Obstacle mode on.");
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
        Debug.Log("Obstacle beacons cleared. Obstacle mode off.");
        obstacleMode = false;
        DeleteBeacons();
        if (!distanceRefresh)
        {
            time = 0;
        }
    }

    #endregion

    #region SPOTLIGHT MODE

    public void SpotlightOn()
    {
        Debug.Log("Spotlight mode on.");
        GameObject.Find("Spotlight Text").GetComponent<TextMesh>().text = "Spotlight Mode: On";
        textManager.GetComponent<TextToSpeechGoogle>().playTextGoogle("Spotlight mode on.");
        spotlightMode = true;
        MuteAllObstacleBeacons();
    }

    public void SpotlightOff()
    {
        Debug.Log("Spotlight mode off.");
        GameObject.Find("Spotlight Text").GetComponent<TextMesh>().text = "Spotlight Mode: Off";
        textManager.GetComponent<TextToSpeechGoogle>().playTextGoogle("Spotlight mode off.");
        spotlightMode = false;
        UnmuteAllObstacleBeacons();
    }

    #endregion

    #region PROXIMITY MODE

    public void ProximityModeOn()
    {
        Debug.Log("Proximity mode on. Beacons refreshed.");
        GameObject.Find("Proximity Text").GetComponent<TextMesh>().text = "Proximity Mode: On";
        textManager.GetComponent<TextToSpeechGoogle>().playTextGoogle("Proximity mode on.");
        proximityMode = true;
        obstacleBeacon = obstacleBeaconLog;
        wallBeacon = wallBeaconLog;
        DeleteBeacons();
        ConeShot();
    }

    public void ProximityModeOff()
    {
        Debug.Log("Proximity mode off. Beacons refreshed.");
        GameObject.Find("Proximity Text").GetComponent<TextMesh>().text = "Proximity Mode: Off";
        textManager.GetComponent<TextToSpeechGoogle>().playTextGoogle("Proximity mode off.");
        proximityMode = true;
        obstacleBeacon = obstacleBeaconLin;
        wallBeacon = wallBeaconLin;
        DeleteBeacons();
        ConeShot();
    }

    #endregion

    #region BEACON SHOOTING

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

            else if (hit.transform.gameObject.tag == "Obstacle Beacon" || hit.transform.gameObject.tag == "Wall Beacon")
            {
                Debug.Log("Hit preexisting beacon.");
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
        float sphereRadius = 0.1f;

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
                //Check whether hit item is a floor, a ceiling, or another beacon; if not, instantiate beacon
                if (hit.transform.gameObject.tag == "Floor" || hit.transform.gameObject.tag == "Ceiling" || hit.transform.gameObject.tag == "Obstacle Beacon" || hit.transform.gameObject.tag == "Wall Beacon")
                {
                    //Do nothing
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

    #endregion

    #region BEACON MANAGEMENT

    public void DeleteBeacons()
    {
        foreach (Transform beacon in beaconManager.transform)
        {
            GameObject.Destroy(beacon.gameObject);
        }
    }

    public void MuteAllObstacleBeacons ()
    {
        foreach (Transform beacon in beaconManager.transform)
        {
            beacon.gameObject.GetComponent<AudioSource>().volume = 0;
            beacon.gameObject.GetComponent<Renderer>().material.color = Color.gray;
            //Also turn the beacons gray
        }
    }

    public void UnmuteAllObstacleBeacons()
    {
        foreach (Transform beacon in beaconManager.transform)
        {
            beacon.gameObject.GetComponent<AudioSource>().volume = 1;
            //Also restore beacons' color (red for obstacle, orange for wall)
            if (beacon.gameObject.tag == "Obstacle Beacon")
            {
                beacon.gameObject.GetComponent<Renderer>().material = obstacleMaterial;
            }

            else //presuming wall beacon
            {
                beacon.gameObject.GetComponent<Renderer>().material = wallMaterial;
            }

        }
    }
    #endregion

}
