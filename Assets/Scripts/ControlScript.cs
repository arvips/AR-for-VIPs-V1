using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using UnityEngine.UI;
using UnityEngine.XR.WSA.Input;
using System;

/// <summary>
/// Control Script should take in inputs (primarily voice) and use them to trigger the three main commands: Obstacles, Locate Text, and Read Text.
/// Which method each of the three commands maps to should be adjustable by adjusting parameters in the Control Script's insepctor.
/// For example, it should be easy to have "Obstacles" use shoot beacon, spray beacon, or boxcast spray beacon.
/// This should make it easier to test various scripts and incorporate them into the prototype.
/// </summary>

[System.Serializable]
public class ControlScript : MonoBehaviour {

    //Help field in inspector
    [SerializeField]
#if UNITY_EDITOR
    [Help("\nHOW TO USE CONTROL SCRIPT\n\n1. Attach relevent scripts to the Script Manager\n\n2. Open Control Script in editor\n\n3. Adjust the Core Commands (Obstacles, LocateText, and ReadText functions) to use the attached scripts\n", UnityEditor.MessageType.Info)]
#endif
    [Tooltip("Necessary for help field above to appear.")]
    float inspectorField = 1440f; //without this, help field goes away

    #region **INITIALIZATION**
   
    public GestureRecognizer gestureRec;

    [Tooltip("Spatial Processing object.")]
    public GameObject spatialProcessing;

    [Tooltip("Parent object of spawned obstacle beacons.")]
    public GameObject obstacleBeaconManager;

    [Tooltip("Parent object of spawned text beacons.")]
    public GameObject textBeaconManager;

    [Tooltip("Test object will change color when one of the three core commands are input.")]
    public GameObject testCube;

    private int tapCount = 1;

    void Start () {

        //Start a gesture recognizer to look for the Tap gesture
        gestureRec = new GestureRecognizer();

        //"Manipulation" seems to be overriding tap and hold, so temporarily adjusting to a simple rotate-through using tap; tap once for obstacles, again for locate text, again for read text, repeat

        gestureRec.Tapped += Tap;
        //gestureRec.HoldCompleted += HoldCompleted;
        //gestureRec.ManipulationCompleted += ManipulationCompleted;

        gestureRec.SetRecognizableGestures(GestureSettings.Tap);
        //gestureRec.SetRecognizableGestures(GestureSettings.DoubleTap);
        //gestureRec.SetRecognizableGestures(GestureSettings.Hold);
        //gestureRec.SetRecognizableGestures(GestureSettings.ManipulationTranslate);

        gestureRec.StartCapturingGestures();
        Debug.Log("Gesture Recognizer Initialized");
        LocateText();

    }

    void OnDisable()
    {
        //unsubscribe from event
        gestureRec.Tapped -= Tap;
        //gestureRec.HoldCompleted -= HoldCompleted;
        //gestureRec.ManipulationCompleted -= ManipulationCompleted;
    }

    #endregion


    #region **GESTURE CONTROLS**
    public void Tap(TappedEventArgs args) //(InteractionSourceKind source, int tapCount, Ray headRay) //(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose, int tapCount) 
    {
        //On Tap, activate current command
        if (tapCount == 1)
        {
            Obstacles();
            tapCount++;
        }

        else if (tapCount == 2)
        {
            LocateText();
            tapCount++;
        }

        else
        {
            ReadText();
            tapCount = 1;
        }

        Debug.Log("Tap event registered");
    }

    public void HoldCompleted(HoldCompletedEventArgs args)
    {
        //On Hold, activate "Locate Text" command
        LocateText();
        Debug.Log("Hold Started event registered");
    }

    public void ManipulationCompleted(ManipulationCompletedEventArgs args)
    {
        //On Manipulation (tap and move), activate "Read Text" command
        ReadText();
        Debug.Log("Manipulation Started event registered");
    }

    #endregion


    //**ADD CORE COMMANDS TO OTHER SCRIPTS ATTACHED TO CONTROL SCRIPT HERE**
    #region **CORE COMMANDS**

    public void Obstacles ()
    {
        Debug.Log("Obstacles command activated");
        testCube.GetComponent<Renderer>().material.color = Color.red;

        //Create planes
        //spatialProcessing.GetComponent<PlaySpaceManager>().CreatePlanes();

        //Shoot cone of beacons
        GetComponentInParent<ShootCone>().ConeShot();

    }

    public void LocateText ()
    {
        Debug.Log("Locate Text command activated");
        testCube.GetComponent<Renderer>().material.color = Color.blue;
        InputManager go = GetComponent<InputManager>();  //you probably need to actually specify the input manager using transforms
        Debug.Log(go);
        
        //GetComponent<CameraManager>().BeginManualPhotoMode();

    }

    public void ReadText ()
    {
        Debug.Log("Read Text command activated");
        testCube.GetComponent<Renderer>().material.color = Color.green;


    }

    #endregion


    //OTHER FUNCTIONS
    #region **CLEAR BEACONS**

    public void ClearObstacleBeacons ()
    {
        //Destroys all obstacle beacons.
        foreach (Transform child in obstacleBeaconManager.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void ClearTextBeacons ()
    {
        //Destorys all text beacons.
        foreach (Transform child in textBeaconManager.transform)
        {
            Destroy(child.gameObject);
        }
    }

    #endregion

    #region **ADJUST OBSTACLE BEACON CONE**
    public void MaxBeaconsAdjust (bool up)
    {
        //int maxBeacons = GetComponentInParent<ShootCone>().maxBeacons;

        if (up)
        {
            GetComponentInParent<ShootCone>().maxBeacons = Mathf.RoundToInt(GetComponentInParent<ShootCone>().maxBeacons * 2f);
            Debug.Log("Max Beacons Up: " + GetComponentInParent<ShootCone>().maxBeacons);
        }

        else
        {
            GetComponentInParent<ShootCone>().maxBeacons = Mathf.RoundToInt(GetComponentInParent<ShootCone>().maxBeacons / 2f);
            Debug.Log("Max Beacons Down: " + GetComponentInParent<ShootCone>().maxBeacons);
        }
    }

    public void DeviationAdjust (bool wider) {
        if (wider)
        {
            GetComponentInParent<ShootCone>().deviation *= 5f;
            Debug.Log("Max Beacons Up: " + GetComponentInParent<ShootCone>().maxBeacons);
        }

        else
        {
            GetComponentInParent<ShootCone>().deviation /= 5f;
            Debug.Log("Max Beacons Down: " + GetComponentInParent<ShootCone>().maxBeacons);
        }
        }



    #endregion

    #region MESH PROCESSING
    
    public void ProcessMesh ()
    {
        //Stops scanning and creates planes
        Debug.Log("Process Mesh activated");
        spatialProcessing.GetComponent<PlaySpaceManager>().ProcessMesh();
        testCube.GetComponent<Renderer>().material.color = Color.yellow;

    }

    public void RestartScanning ()
    {
        //Destroys planes and restarts scanning
        Debug.Log("Restart Scanning activated");
        testCube.GetComponent<Renderer>().material.color = Color.cyan;
        spatialProcessing.GetComponent<PlaySpaceManager>().RestartScanning();

        //Adjustments needed:
        //1. Adjust meshing behavior to not create walls over doorways
    }


    #endregion

}
