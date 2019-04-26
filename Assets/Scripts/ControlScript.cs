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

    [Tooltip("Text Manager object.")]
    public GameObject TextManager;


    [Tooltip("Parent object of spawned text beacons.")]
    public GameObject textBeaconManager;

    [Tooltip("Test object will change color when one of the three core commands are input.")]
    public GameObject testCube;

    [Tooltip("This text will change to copy the Debug output.")]
    public GameObject debugText;

    private int tapCount = 1;

    private string output = ""; //helps print text in UI


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

    }

    private void Update()
    {
    }

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog; //helps print text in UI
    }

    void OnDisable()
    {
        //unsubscribe from event
        gestureRec.Tapped -= Tap;
        //gestureRec.HoldCompleted -= HoldCompleted;
        //gestureRec.ManipulationCompleted -= ManipulationCompleted;
        Application.logMessageReceived -= HandleLog;
    }

    #endregion


    #region **GESTURE CONTROLS**
    public void Tap(TappedEventArgs args) //(InteractionSourceKind source, int tapCount, Ray headRay) //(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose, int tapCount) 
    {
        //On Tap, toggle obstacles on or off.
        //Debug.Log("Obstacle mode: " + GetComponentInParent<ObstacleBeaconScript>().obstacleMode);
        if (!GetComponentInParent<ObstacleBeaconScript>().obstacleMode)
        {
            GetComponentInParent<ObstacleBeaconScript>().ObstaclesOn();
        }

        else 
        {
            GetComponentInParent<ObstacleBeaconScript>().ObstaclesOff();
        }

        //Debug.Log("Tap event registered");
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

    public void ObstaclesOn ()
    {
        //Activates obstacles mode. While active, beacons are replaced when necessary to ensure user's surroundings are covered.

        //Debug.Log("Obstacle beacons turned on.");
        testCube.GetComponent<Renderer>().material.color = Color.red;
        GetComponentInParent<ObstacleBeaconScript>().ObstaclesOn();

    }

    public void ObstaclesOff()
    {
        //Deactivates obstacles mode.
        //Debug.Log("Obstacle beacons turned off.");
        testCube.GetComponent<Renderer>().material.color = Color.gray;
        GetComponentInParent<ObstacleBeaconScript>().ObstaclesOff();
    }

    public void LocateText ()
    {
        Debug.Log("Locate Text command activated");
        testCube.GetComponent<Renderer>().material.color = Color.blue;
        TextManager.GetComponent<CameraManager>().BeginManualPhotoMode();

    }

    public void ReadText()
    {
        Debug.Log("Read Text command activated");
        testCube.GetComponent<Renderer>().material.color = Color.green;

        //Note current audio source of Text Manager
        //AudioSource defaultAudioSource = TextManager.GetComponent<TextToSpeechManager>().ttsmAudioSource;
        AudioSource defaultAudioSource = TextManager.GetComponent<TextToSpeechGoogle>().audioSourceFinal;

        if (textBeaconManager.transform.childCount > 0)
        {
            foreach (Transform beacon in textBeaconManager.transform)
            {
                GameObject newCam = GameObject.FindGameObjectWithTag("MainCamera");
                if (Vector3.Distance(beacon.transform.position, newCam.transform.position) < 50)
                {
                    //For each beacon, change the audio source to the beacon's audio source and have it read out the beacon's text.
                    string beaconText = beacon.gameObject.GetComponent<TextInstanceScript>().beaconText;
                    Debug.Log("CS: Text is: " + beaconText);
                    //TextManager.GetComponent<TextToSpeechManager>().ttsmAudioSource = beacon.gameObject.GetComponent<AudioSource>();
                    //TextManager.GetComponent<TextToSpeechManager>().SpeakText("Text: " + beaconText);
                    TextManager.GetComponent<TextToSpeechGoogle>().audioSourceFinal = beacon.gameObject.GetComponent<AudioSource>();
                    TextManager.transform.position = beacon.transform.position;
                    StartCoroutine(TextManager.GetComponent<TextToSpeechGoogle>().playTextGoogle("Text: " + beaconText));
                }
            }
        }

        else
        {
            //No child text beacons found
            Debug.Log("No text beacons found.");
            //TextManager.GetComponent<TextToSpeechManager>().SpeakText("No text found. Try the command, 'locate text.'");
            StartCoroutine(TextManager.GetComponent<TextToSpeechGoogle>().playTextGoogle("No text found. Try the command, 'locate text.'"));


        }

        //When done, return audio source to default.
        //TextManager.GetComponent<TextToSpeechManager>().ttsmAudioSource = defaultAudioSource;
        TextManager.GetComponent<TextToSpeechGoogle>().audioSourceFinal = defaultAudioSource;
        TextManager.transform.position = new Vector3(0, 0, 0);
    }

    public void increaseSpeed()
    {
        TextManager.GetComponent<TextToSpeechGoogle>().increaseSpeechRate();
    }

    public void decreaseSpeed()
    {
        TextManager.GetComponent<TextToSpeechGoogle>().decreaseSpeechRate();
    }

    #endregion


    //OTHER FUNCTIONS
    #region **CLEAR BEACONS**

    public void ClearObstacleBeacons ()
    {
        //Destroys all obstacle beacons and turns off obstacle mode.
        GetComponentInParent<ObstacleBeaconScript>().DeleteBeacons();
        GetComponentInParent<ObstacleBeaconScript>().ObstaclesOff();
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
        //int maxBeacons = GetComponentInParent<ObstacleBeaconScript>().maxBeacons;

        if (up)
        {
            GetComponentInParent<ObstacleBeaconScript>().maxBeacons = Mathf.RoundToInt(GetComponentInParent<ObstacleBeaconScript>().maxBeacons * 2f);
            Debug.Log("Max Beacons Up: " + GetComponentInParent<ObstacleBeaconScript>().maxBeacons);
        }

        else
        {
            GetComponentInParent<ObstacleBeaconScript>().maxBeacons = Mathf.RoundToInt(GetComponentInParent<ObstacleBeaconScript>().maxBeacons / 2f);
            Debug.Log("Max Beacons Down: " + GetComponentInParent<ObstacleBeaconScript>().maxBeacons);
        }
    }

    public void DeviationAdjust (bool wider) {
        if (wider)
        {
            GetComponentInParent<ObstacleBeaconScript>().deviation *= 5f;
            Debug.Log("Max Beacons Up: " + GetComponentInParent<ObstacleBeaconScript>().maxBeacons);
        }

        else
        {
            GetComponentInParent<ObstacleBeaconScript>().deviation /= 5f;
            Debug.Log("Max Beacons Down: " + GetComponentInParent<ObstacleBeaconScript>().maxBeacons);
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

    #region HOLOLENS DEBUG UI

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        output = logString;
        debugText.GetComponent<TextMesh>().text += "\n" + output;
    }

    public void ClearDebug()
    {
        debugText.GetComponent<TextMesh>().text = "Debug log cleared.";
    }

    #endregion
}
