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

    [Tooltip("Parent object of spawned obstacle beacons.")]
    public GameObject obstacleBeaconManager;

    [Tooltip("Parent object of spawned text beacons.")]
    public GameObject textBeaconManager;

    void Start () {

        //Start a gesture recognizer to look for the Tap gesture
        gestureRec = new GestureRecognizer();

        gestureRec.Tapped += Tap;
        gestureRec.HoldStarted += HoldStarted;
        gestureRec.ManipulationStarted += ManipulationStarted;

        gestureRec.SetRecognizableGestures(GestureSettings.Tap);
        //gestureRec.SetRecognizableGestures(GestureSettings.DoubleTap);
        gestureRec.SetRecognizableGestures(GestureSettings.Hold);
        gestureRec.SetRecognizableGestures(GestureSettings.ManipulationTranslate);

        gestureRec.StartCapturingGestures();
        Debug.Log("Gesture Recognizer Initialized");

    }

    void OnDisable()
    {
        //unsubscribe from event
        gestureRec.Tapped -= Tap;
        gestureRec.HoldStarted -= HoldStarted;
        gestureRec.ManipulationStarted -= ManipulationStarted;
    }

    #endregion


    #region **GESTURE CONTROLS**
    public void Tap(TappedEventArgs args) //(InteractionSourceKind source, int tapCount, Ray headRay) //(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose, int tapCount) 
    {
        //On Tap, activate "Obstacles" command
        Obstacles();

        Debug.Log("Tap event registered");
    }

    public void HoldStarted (HoldStartedEventArgs args)
    {
        //On Hold, activate "Locate Text" command
        LocateText();
        Debug.Log("Hold Started event registered");
    }

    public void ManipulationStarted (ManipulationStartedEventArgs args)
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

        GetComponentInParent<ShootCone>().ConeShot();

    }

    public void LocateText ()
    {
        Debug.Log("Locate Text command activated");


    }

    public void ReadText ()
    {
        Debug.Log("Read Text command activated");

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



}
