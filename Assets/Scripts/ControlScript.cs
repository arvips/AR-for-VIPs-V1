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
public class ControlScript : MonoBehaviour, IInputClickHandler {

    //Help field in inspector
    [SerializeField]
#if UNITY_EDITOR
    [Help("\nHOW TO USE CONTROL SCRIPT\n\n1. Attach relevent scripts to the Script Manager\n\n2. Open Control Script in editor\n\n3. Adjust the Core Commands (Obstacles, LocateText, and ReadText functions) to use the attached scripts\n", UnityEditor.MessageType.Info)]
#endif
    [Tooltip("Necessary for help field above to appear.")]
    float inspectorField = 1440f; //without this, help field goes away
    bool stopReading = false;
    TextToSpeechGoogle text;

    #region **INITIALIZATION**
   

    //Gestures and Spatial Processing
    public GestureRecognizer gestureRec;

    [Tooltip("Spatial Processing object.")]
    public GameObject spatialProcessing;


    //Text
    [Tooltip("Text Manager object.")]
    public GameObject TextManager;

    [Tooltip("Parent object of spawned text beacons.")]
    public GameObject textBeaconManager;

    [Tooltip("Strings to be used with sample text beacons.")]
    public string[] sampleText;

    //Testing and Debugging
    [Tooltip("Test object will change color when one of the three core commands are input.")]
    public GameObject testCube;

    [Tooltip("This text will change to copy the Debug output.")]
    public GameObject debugText;

    public bool inCoroutine = true;
    
    private string output = ""; //helps print text in UI

    private bool stop = false;

    private bool readTextRunning = false;


    private string repeatText = "No text to repeat.";
    private GameObject repeatBeacon = null;
    private bool isRepeating = false;
    private Physics physics;
    private bool captureTextRunning = false;


    enum TapState {
        Obstacles,
        Text,
        // Object Sonification 
    }
    private TapState tapState = TapState.Obstacles; 


    void Start () {

        tapState = TapState.Obstacles;
        //Start a gesture recognizer to look for the Tap gesture
        gestureRec = new GestureRecognizer();

        //"Manipulation" seems to be overriding tap and hold, so temporarily adjusting to a simple rotate-through using tap; tap once for obstacles, again for locate text, again for read text, repeat

        gestureRec.Tapped += Tap;
        //gestureRec.HoldCompleted += HoldCompleted;

        //gestureRec.HoldCompleted += HoldCompleted;
        //gestureRec.ManipulationCompleted += ManipulationCompleted;

        gestureRec.SetRecognizableGestures(GestureSettings.Tap);
        //gestureRec.SetRecognizableGestures(GestureSettings.DoubleTap);
        //gestureRec.SetRecognizableGestures(GestureSettings.Hold);
        //gestureRec.SetRecognizableGestures(GestureSettings.ManipulationTranslate);

        gestureRec.StartCapturingGestures();
        Debug.Log("Gesture Recognizer Initialized");

        //Voice confirmation that interface is ready.
        TextManager.GetComponent<TextToSpeechGoogle>().playTextGoogle("Ready to assist.");

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
        Application.logMessageReceived -= HandleLog;
    }

    #endregion


    #region **GESTURE CONTROLS**
    // This *should* work 
    public void Tap(TappedEventArgs args) //(InteractionSourceKind source, int tapCount, Ray headRay) //(InteractionSource source, InteractionSourcePose sourcePose, Pose headPose, int tapCount) 
    {
        // We should add a visual indicator of the current mode in the future
        switch(tapState)
        {
            case TapState.Obstacles:
                tapState = TapState.Text;
                break;
            case TapState.Text:
                tapState = TapState.Obstacles;
                break;
            //case TapState.ObjectSonification
        }
  
        // Will change to object sonification instead of toggling obstacles
        //ObjectSonification();

        //On Tap, toggle obstacles on or off.
        //Debug.Log("Obstacle mode: " + GetComponentInParent<ObstacleBeaconScript>().obstacleMode);
        

        

        Debug.Log("Tap event registered");
    }

    

    public void HoldCompleted(HoldCompletedEventArgs args)
    {
        // stop reading or clear obstacles?
        switch (tapState)
        {
            case TapState.Obstacles:
                if (!GetComponentInParent<ObstacleBeaconScript>().obstacleMode)
                {
                    GetComponentInParent<ObstacleBeaconScript>().ObstaclesOn();
                }
                else
                {
                    GetComponentInParent<ObstacleBeaconScript>().ObstaclesOff();
                }
                break;
            case TapState.Text:
                CaptureText();
                break;
        }

        ////On Hold, activate "Locate Text" command.
        //if (!captureTextRunning && !readTextRunning)
        //{
        //    CaptureText();
        //}

        

        Debug.Log("Hold Completed event registered");
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

    private void ObjectSonification()
    {
        throw new NotImplementedException();
    }

    public void CaptureText ()
    {
        Debug.Log("Capture Text command activated");
        testCube.GetComponent<Renderer>().material.color = Color.blue;

        //Make sure capture text doesn't try to run while a previous instance is running!
        //For now I'm putting in a simple time delay but this could be improved
        StartCoroutine(CaptureTextRoutine());


    }

    public void StopReadingText()
    {
        Debug.Log("stop reading text");
        if(text && text.audioSourceFinal.isPlaying)
            stopReading = true;
    }

    public IEnumerator CaptureTextRoutine()
    {
        //Check whether capture text is running. If it's not, mark it as running, start it running, and wait 5 seconds before marking it done.

        if (captureTextRunning == false)
        {
            captureTextRunning = true;
            TextManager.GetComponent<CameraManager>().BeginManualPhotoMode();
            WaitForSeconds wait = new WaitForSeconds(5);
            yield return wait;
            captureTextRunning = false;
            Debug.Log("Ready for new capture.");
        }
        else
        {
            Debug.Log("Please wait for current capture to complete.");
            yield return null;
        }
    }

    public void ReadText ()
    {
        Debug.Log("Starting read text coroutine.");
        StartCoroutine(ReadTextRoutine(false));
    }

    public void ReadAllText()
    {
        Debug.Log("Starting read ALL text coroutine.");
        StartCoroutine(ReadTextRoutine(true));
    }


    public IEnumerator ReadTextRoutine(bool allText)
    {
        Debug.Log("Read Text command activated");
        readTextRunning = true;
        testCube.GetComponent<Renderer>().material.color = Color.green;

        //Note current audio source of Text Manager
        //AudioSource defaultAudioSource = TextManager.GetComponent<TextToSpeechManager>().ttsmAudioSource;
        AudioSource defaultAudioSource = TextManager.GetComponent<TextToSpeechGoogle>().audioSourceFinal;

        if (textBeaconManager.transform.childCount > 0)
        {
            if (allText) //read everything
            {
                 
                foreach (Transform beacon in textBeaconManager.transform)
                {

                    //Stop audio playback on user command
                    if (stop)
                    {
                        stop = false;
                        break;
                    }

                    //If user requested a repeat, wait until it's finished (TEMPORARILY DISABLED)
                    //yield return new WaitUntil(() => isRepeating == false);

                    //For each beacon, change the audio source to the beacon's audio source and have it read out the beacon's text.

                    if (beacon.gameObject!= null)
                    {
                        string beaconText = beacon.gameObject.GetComponent<TextInstanceScript>().beaconText;

                        repeatBeacon = beacon.gameObject; //Sets latest beacon to be the beacon to repeat.
                        repeatText = beaconText; //Sets latest text to be the phrase to repeat.

                        Debug.Log("CS: Text is: " + beaconText);

                        //Set audio source to that of the beacon
                        text = TextManager.GetComponent<TextToSpeechGoogle>();
                        text.audioSourceFinal = beacon.gameObject.GetComponent<AudioSource>();

                        //Start playback of current beacon
                        text.playTextGoogle(beaconText);
                        float clipLength = text.clipLength;

                        //Wait for length of clip.
                        WaitForSeconds wait = new WaitForSeconds(clipLength);

                        //Debug.Log("Wait time: " + wait);
                        // yield return wait;
                        float count = 0f;
                        while(count < clipLength)
                        {
                            if(stopReading)
                            {
                                if (text.audioSourceFinal.isPlaying)
                                {
                                    Debug.Log("stopping");
                                    text.audioSourceFinal.Stop();
                                }
                            }
                            count += Time.deltaTime;
                            yield return null;
                        }

                    }

                }
                stopReading = false;
                text = null;
            }

            else //read only what's in the user's Spotlight view 
            {
                Debug.Log("Starting read (not all) texts.");
                //Capture camera's location and orientation
                var headPosition = Camera.main.transform.position;
                var gazeDirection = Camera.main.transform.forward;

                float spotlightSize = 2f;
                float depth = 20;
                float coneCastAngle = 120;

                bool textBeaconFound = false;

                //Instantiate variable to hold hit info
                RaycastHit[] spotlightHits = physics.ConeCastAll(headPosition, spotlightSize, gazeDirection, depth, coneCastAngle);

                foreach (RaycastHit hit in spotlightHits)
                {
                    if (hit.transform != null)
                    {
                        if (hit.transform.IsChildOf(textBeaconManager.transform))
                        {
                            //Stop audio playback on user command
                            if (stop)
                            {
                                stop = false;
                                break;
                            }

                            //If user requested a repeat, wait until it's finished (TEMPORARILY DISABLED)
                            //yield return new WaitUntil(() => isRepeating == false);

                            if (hit.transform.gameObject != null)
                            {
                                GameObject beacon = hit.transform.gameObject;

                                //For each beacon, change the audio source to the beacon's audio source and have it read out the beacon's text.
                                string beaconText = beacon.gameObject.GetComponent<TextInstanceScript>().beaconText;
                                //Debug.Log("Text beacon hit: " + beaconText);


                                repeatBeacon = beacon.gameObject; //Sets latest beacon to be the beacon to repeat.
                                repeatText = beaconText; //Sets latest text to be the phrase to repeat.

                                Debug.Log("CS: Text is: " + beaconText);

                                //Set audio source to that of the beacon
                                TextManager.GetComponent<TextToSpeechGoogle>().audioSourceFinal = beacon.gameObject.GetComponent<AudioSource>();

                                //Start playback of current beacon
                                TextManager.GetComponent<TextToSpeechGoogle>().playTextGoogle(beaconText);
                                float clipLength = TextManager.GetComponent<TextToSpeechGoogle>().clipLength;

                                //Wait for length of clip.
                                WaitForSeconds wait = new WaitForSeconds(clipLength);

                                textBeaconFound = true;

                                //Debug.Log("Wait time: " + wait);
                                yield return wait;
                            }
                           
                        }
                    }
                }

                if (!textBeaconFound)
                {
                    TextManager.GetComponent<TextToSpeechGoogle>().playTextGoogle("No text in this direction. Try again or say read all text.");
                }
            }
        }
        else
        {
            //No child text beacons found
            Debug.Log("No text beacons found.");
            TextManager.GetComponent<TextToSpeechGoogle>().playTextGoogle("No text found. Try the command, 'capture text.'");
        }

        //When done, return audio source to default.
        TextManager.GetComponent<TextToSpeechGoogle>().audioSourceFinal = defaultAudioSource;
        TextManager.transform.position = new Vector3(0, 0, 0);
        readTextRunning = false;
        stop = false;
        yield return null;
    }

    public void stopPlayback()
    {
        Debug.Log("Stop playback.");
        //if (TextManager.transform.position != new Vector3()) 
        stop = true;

        TextManager.GetComponent<TextToSpeechGoogle>().audioSourceFinal.Stop();
    }

    public void nextPlayback()
    {
        //NOTE: The next command may still wait the full length of the current clip before going to the next one. This should be fixed.
        Debug.Log("Skipping to next text.");
        TextManager.GetComponent<TextToSpeechGoogle>().audioSourceFinal.Stop();
    }

    public void repeatPlayback()
    {
        StartCoroutine(repeatPlaybackRoutine());

    }

    public IEnumerator repeatPlaybackRoutine()
    {
        Debug.Log("Repeating text.");
        isRepeating = true;

        //Note current audio source of Text Manager
        AudioSource defaultAudioSource = TextManager.GetComponent<TextToSpeechGoogle>().audioSourceFinal;

        if (readTextRunning)
        {
            //If read text is currently running, have TextToSpeechGoogle's audio source play the current clip again and reset the wait time.
            TextManager.GetComponent<TextToSpeechGoogle>().audioSourceFinal.Play();
            WaitForSeconds wait = new WaitForSeconds(TextManager.GetComponent<TextToSpeechGoogle>().clipLength);
            yield return wait;
        }

        else
        {
            //Set audio source to that of the beacon
            if (repeatBeacon != null) TextManager.GetComponent<TextToSpeechGoogle>().audioSourceFinal = repeatBeacon.gameObject.GetComponent<AudioSource>();
            else TextManager.GetComponent<TextToSpeechGoogle>().audioSourceFinal = defaultAudioSource;
            //TextManager.transform.position = beacon.transform.position; //Tries to change position of text manager to match beacon

            //Start playback of current beacon
            TextManager.GetComponent<TextToSpeechGoogle>().playTextGoogle(repeatText);
            float clipLength = TextManager.GetComponent<TextToSpeechGoogle>().clipLength;

            //Wait for length of clip.
            WaitForSeconds wait = new WaitForSeconds(clipLength);

            //Debug.Log("Wait time: " + wait);
            yield return wait;
        }

        //Reset audio source to the default.
        TextManager.GetComponent<TextToSpeechGoogle>().audioSourceFinal = defaultAudioSource;
        isRepeating = false;
        yield return null;

    }

    public void increaseSpeed()
    {
        Debug.Log("Text speed increased to: " + TextManager.GetComponent<TextToSpeechGoogle>().speakingRate);
        TextManager.GetComponent<TextToSpeechGoogle>().increaseSpeechRate();
    }

    public void decreaseSpeed()
    {
        Debug.Log("Text speed decreased to: " + TextManager.GetComponent<TextToSpeechGoogle>().speakingRate);
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
        //Destroys all text beacons and resets the icon dictionary in Icon Manager.
        Debug.Log("Text beacons cleared.");
        foreach (Transform child in textBeaconManager.transform)
        {
            Destroy(child.gameObject);
            TextManager.GetComponent<IconManager>().iconDictionary.Clear();
        }

       repeatText = "No text to repeat.";
       repeatBeacon = null;
       isRepeating = false;

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

    #region SAMPLE TEXT BEACONS

    public void ShootText(int index)
    {
        //Shoots a sample text beacon imbued with text according to index.
        IconManager textManager = TextManager.GetComponent<IconManager>();
        textManager.ShootText(sampleText[index]);
    }

    public void DemoCaptureText (int index)
    {
        //Replicates experience of capturing text without using camera. Ideal for use while capturing video.
        
            Debug.Log("Starting demo capture text routine. Index: " + index);
            StartCoroutine(DemoCaptureTextRoutine(index));
        
    }

    public IEnumerator DemoCaptureTextRoutine (int index)
    {
        IconManager textManager = TextManager.GetComponent<IconManager>();
        TextManager.GetComponent<TextToSpeechGoogle>().playTextGoogle("Capturing image. Analyzing...");
        WaitForSeconds wait = new WaitForSeconds(4f);
        yield return wait; 

        textManager.ShootText(sampleText[index]);

    }

    #endregion

    #region MESH PROCESSING

    public void ProcessMesh ()
    {
        //Stops scanning and creates planes
        Debug.Log("Process Mesh activated");
        ObstaclesOff();
        spatialProcessing.GetComponent<PlaySpaceManager>().ProcessMesh();
        testCube.GetComponent<Renderer>().material.color = Color.yellow;

    }

    public void RestartScanning ()
    {
        //Destroys planes and restarts scanning
        Debug.Log("Restart Scanning activated");
        ObstaclesOff();
        testCube.GetComponent<Renderer>().material.color = Color.cyan;
        spatialProcessing.GetComponent<PlaySpaceManager>().RestartScanning();
        TextManager.GetComponent<TextToSpeechGoogle>().playTextGoogle("Restarting scanning.");

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

    public void ToggleDebug()
    {
        //Toggles active status of Debug Window.
        if (debugText.gameObject.transform.parent.gameObject.activeSelf)
        {
            debugText.gameObject.transform.parent.gameObject.SetActive(false);
            Debug.Log("Debug window deactivated.");
        }

        else
        {
            debugText.gameObject.transform.parent.gameObject.SetActive(true);
            Debug.Log("Debug window reactivated.");
        }
    }


    #endregion


}
