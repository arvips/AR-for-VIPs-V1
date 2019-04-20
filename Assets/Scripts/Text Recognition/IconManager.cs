using UnityEngine;
using System;
using SimpleJSON;
using Levenshtein;
using System.Collections.Generic;

/// <summary>
/// IconManager calculates where icons should go and places them in the realspace.
/// </summary>
public class IconManager: MonoBehaviour {

    [Tooltip("Select the layers raycast should target.")]
    public LayerMask RaycastLayer;

    [Tooltip("Select the icon to show.")]
    public GameObject Icon;

    // Currently selected icon
    public GameObject SelectedIcon { get; set; }

    // Dictionary of Icons
    private Dictionary<int, List<GameObject>> iconDictionary;
    public int NumIcons { get; set; }

    // Thresholds and Constants
    private const float DefaultIconThickness = 0.015f;
    private const float DefaultIconWidth = 0.08f;
    private const float DefaultIconHeight = 0.08f;
    private const float DefaultIconRadius = 0.15f;
    private const float MinimumIconRadius = 0.12f;
    private const float MaximumIconRadius = 0.25f;
    private const float ScaleIconRadius = 0.9f;
    private const int CombiningThreshold = 31;
    private const int WarningThreshold = 11;
    private const int ValidThreshold = 21;

    // Camera properties
    private  Camera raycastCamera;
    private float ScaleX;
    private float ScaleY;

    // Indicators
    private Boolean NewTextDetected;                                        // Used to tell users if the app detected new text
    private int numIconsDetectedInOneCall;                                  // Used to tell users how many icons were detected after one call

    private SettingsManager SettingsManager;

    void Start()
    {
        SettingsManager = gameObject.GetComponent<SettingsManager>();
        iconDictionary = new Dictionary<int, List<GameObject>>();           // Create new dictionary for icons
        NumIcons = 0;
    }

    /// <summary>
    /// Main function to create icons
    /// </summary>
    public void CreateIcons(string respJson)
    {
        Debug.Log("CreatIcons");
        // Indicators and counts
        NewTextDetected = false;
        numIconsDetectedInOneCall = 0;

        // Parse JSON
        JSONNode json = JSON.Parse(respJson);
        JSONArray texts = json["responses"][0]["textAnnotations"].AsArray;

        // Run audio only mode
        if (gameObject.GetComponent<SettingsManager>().UserSetting == UserType.AudioOnly)
        {
            RunAudioOnlyMode(texts);

            if (SettingsManager.OCRSetting == OCRRunSetting.Manual)                                             // Update indicator to say that the app is iot detecting anymore (user can now detect text again)
            {
                //GetComponent<CameraManager>().detecting = false;
            }

            return;
        }

        // Camera properties
        ScaleX = (float)Screen.width / (float)CameraManager.cameraResolution.width;
        ScaleY = (float)Screen.height / (float)CameraManager.cameraResolution.height;
        Matrix4x4 cameraToWorldMatrix = GetComponent<CameraManager>().managerCameraToWorldMatrix;

        raycastCamera = GetComponent<CameraManager>().PositionCamera(cameraToWorldMatrix);                      // Position camera to where you took a picture
        Debug.Log("raycastCamera");
        // Vectors to pinpoint location of text
        Vector3 lastRaycastPoint = Vector3.zero;
        Ray lastRay = new Ray();
        Vector3 lastTopLeft = new Vector3();
        Vector3 lastTopRight = new Vector3();
        Vector3 lastBottomRight = new Vector3();
        Vector3 lastBottomLeft = new Vector3();

        Vector3 combinedTopLeft = new Vector3(float.MaxValue, float.MaxValue);
        Vector3 combinedTopRight = new Vector3(float.MinValue, float.MaxValue);
        Vector3 combinedBottomRight = new Vector3(float.MinValue, float.MinValue);
        Vector3 combinedBottomLeft = new Vector3(float.MaxValue, float.MinValue);

        String runningText = "";                                                                                // Text string that should be in one icon (multiple text results can be in one icon so keep a running tab)
        Debug.Log("pre foreach");
        foreach (JSONNode text in texts)
        {
            JSONNode vertices = text["boundingPoly"]["vertices"];

            Vector3 currTopLeft = CalcTopLeftVector(vertices);
            Vector3 currTopRight = CalcTopRightVector(vertices);
            Vector3 currBottomRight = CalcBottomRightVector(vertices);
            Vector3 currBottomLeft = CalcBottomLeftVector(vertices);

            // Google Vision API returns coordinates with the top-left being the origin. Unity uses bottom-left as the origin so you must flip the Y.
            Vector3 topLeftScaledAndFlipped = ScaleVector(FlipY(currTopLeft));
            Vector3 topRightScaledAndFlipped = ScaleVector(FlipY(currTopRight));
            Vector3 bottomRightScaledAndFlipped = ScaleVector(FlipY(currBottomRight));
            Vector3 bottomLeftScaledAndFlipped = ScaleVector(FlipY(currBottomLeft));

            // Find center and shoot a ray from the camera to the center
            Vector3 raycastPoint = (topLeftScaledAndFlipped + topRightScaledAndFlipped + bottomRightScaledAndFlipped + bottomLeftScaledAndFlipped) / 4;
            Ray ray = raycastCamera.ScreenPointToRay(raycastPoint);
            
            // Extract Text
            String currText = text["description"];

            if (currText.IndexOf('\n') != -1)                                                           // Skip first result
            {
                continue;
            } else if ((currBottomLeft.y - currTopLeft.y) > (currTopRight.x - currTopLeft.x))           // Skip vertical text
            {
                continue;
            }

            // Combine results into one icon if needed
            float minWidth = Math.Min(currTopLeft.x, currBottomLeft.x) - Math.Max(lastTopRight.x, lastBottomRight.x);
            float minHeight = Math.Min(currTopLeft.y, currTopRight.y) - Math.Max(lastBottomLeft.y, lastBottomRight.y);
            float minPixelDistance = Math.Min(Math.Abs(minWidth), Math.Abs(minHeight));

            if (lastRaycastPoint == Vector3.zero || minPixelDistance < CombiningThreshold)                      // Combine text into one icon
            {
                runningText = runningText + " " + currText;

                // Update location of text 
                combinedTopLeft = new Vector3(Math.Min(combinedTopLeft.x, currTopLeft.x), Math.Min(combinedTopLeft.y, currTopLeft.y));
                combinedTopRight = new Vector3(Math.Max(combinedTopRight.x, currTopRight.x), Math.Min(combinedTopRight.y, currTopRight.y));
                combinedBottomRight = new Vector3(Math.Max(combinedBottomRight.x, currBottomRight.x), Math.Max(combinedBottomRight.y, currBottomRight.y));
                combinedBottomLeft = new Vector3(Math.Min(combinedBottomLeft.x, currBottomLeft.x), Math.Max(combinedBottomLeft.y, currBottomLeft.y));                
            }
            else                                                                                                // Place icon into spatial mapping
            {
                RaycastHit centerHit;

                if (runningText.Length > 1)                                                                     // Make sure you have text to put into the icon
                {
                    Debug.Log("shoot raycast");
                    if (Physics.Raycast(lastRay, out centerHit, 15.0f, RaycastLayer))                           // First try and hit the spatial mapping with the ray
                    {
                        PlaceIcons(centerHit, runningText, currTopLeft, currTopRight, currBottomRight, currBottomLeft, combinedTopLeft, combinedTopRight, combinedBottomRight, combinedBottomLeft);
                    } else if (Physics.Raycast(lastRay, out centerHit, 15.0f, LayerMask.GetMask("Plane")))      // Try to hit the plane if there is no spatial mapping with the ray
                    {
                        PlaceIcons(centerHit, runningText, currTopLeft, currTopRight, currBottomRight, currBottomLeft, combinedTopLeft, combinedTopRight, combinedBottomRight, combinedBottomLeft);
                    }
                }

                combinedTopLeft = new Vector3(float.MaxValue, float.MaxValue);
                combinedTopRight = new Vector3(float.MinValue, float.MaxValue);
                combinedBottomRight = new Vector3(float.MinValue, float.MinValue);
                combinedBottomLeft = new Vector3(float.MaxValue, float.MinValue);
                runningText = currText;
            }

            // Update previous points 
            lastRaycastPoint = raycastPoint;
            lastRay = ray;
            lastTopLeft = currTopLeft;
            lastTopRight = currTopRight;
            lastBottomRight = currBottomRight;
            lastBottomLeft = currBottomLeft;
        }

        // Do the same thing for the last result
        RaycastHit hit;

        if (Physics.Raycast(lastRay, out hit, 15.0f, RaycastLayer))
        {
            PlaceIcons(hit, runningText, lastTopLeft, lastTopRight, lastBottomRight, lastBottomLeft, combinedTopLeft, combinedTopRight, combinedBottomRight, combinedBottomLeft);
        }
        else if (Physics.Raycast(lastRay, out hit, 15.0f, LayerMask.GetMask("Plane")))
        {
            PlaceIcons(hit, runningText, lastTopLeft, lastTopRight, lastBottomRight, lastBottomLeft, combinedTopLeft, combinedTopRight, combinedBottomRight, combinedBottomLeft);
        }

        // Text-to-speech for new text
        if (SettingsManager.OCRSetting == OCRRunSetting.Manual )
        {
            if (NewTextDetected)
            {
                if (numIconsDetectedInOneCall == 1)
                {
                    GetComponent<TextToSpeechManager>().SpeakText(numIconsDetectedInOneCall + " region of text detected.");
                } else
                {
                    GetComponent<TextToSpeechManager>().SpeakText(numIconsDetectedInOneCall + " regions of text detected.");
                }
            }
            else
            {
                GetComponent<TextToSpeechManager>().SpeakText("No New Text Detected");
            }
        }

        // Tell user if there are too many icons
        if (NumIcons > SettingsManager.MaxIcons)
        {
            if (SettingsManager.OCRSetting == OCRRunSetting.Manual)
            {
                GetComponent<TextToSpeechManager>().SpeakText("There are too many icons. Please clear icons before finding new text.");
            } else
            {
                GetComponent<TextToSpeechManager>().SpeakText("There are too many icons. Please clear icons before finding new text.");
            }
        }

        // Update indicators to say that the app is not detecting anymore. At this point, the user can detect text again
        if (SettingsManager.OCRSetting == OCRRunSetting.Manual)
        {
            //GetComponent<CameraManager>().detecting = false;
        }

    }

    /// <summary>
    /// Create the top left vector
    /// </summary>
    Vector3 CalcTopLeftVector(JSONNode node)
    {
        Vector3 vector = new Vector3(node[0]["x"].AsFloat, node[0]["y"].AsFloat, 0);

        return vector;
    }

    /// <summary>
    /// Create the top right vector
    /// </summary>
    Vector3 CalcTopRightVector(JSONNode node)
    {
        Vector3 vector = new Vector3(node[1]["x"].AsFloat, node[1]["y"].AsFloat, 0);

        return vector;
    }

    /// <summary>
    /// Create the bottom right vector
    /// </summary>
    Vector3 CalcBottomRightVector(JSONNode node)
    {
        Vector3 vector = new Vector3(node[2]["x"].AsFloat, node[2]["y"].AsFloat, 0);

        return vector;
    }

    /// <summary>
    /// Create the bottom left vector
    /// </summary>
    Vector3 CalcBottomLeftVector(JSONNode node)
    {
        Vector3 vector = new Vector3(node[3]["x"].AsFloat, node[3]["y"].AsFloat, 0);

        return vector;
    }

    /// <summary>
    /// Fix y coordinate to be referenced from the bottom right
    /// </summary>
    Vector3 FlipY(Vector3 v)
    {
        return new Vector3();
        //return new Vector3(v.x, (float)CameraManager.cameraResolution.height - v.y, v.z);
    }

    /// <summary>
    /// Scale the vector
    /// </summary>
    Vector3 ScaleVector(Vector3 vector)
    {
        return new Vector3(vector.x * ScaleX, vector.y * ScaleY, vector.z);
    }

    /// <summary>
    /// Place icons given a hit to the spatial mapping
    /// </summary>
    public void PlaceIcons(RaycastHit centerHit, String runningText, Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, Vector3 bottomLeft, Vector3 combinedTopLeft, Vector3 combinedTopRight, Vector3 combinedBottomRight, Vector3 combinedBottomLeft)
    {
        if (SameIconExists(centerHit.point, runningText))                   // Avoid duplicates
        {
            return;
        }

        // Put icon into the scene
        GameObject icon = Instantiate(Icon);
        icon.transform.position = centerHit.point;
        //icon.GetComponent<IconAction>().Text = runningText;
        icon.transform.rotation = Quaternion.LookRotation(-centerHit.normal);

        // Change icon size
        ChangeCircleIconScale(icon, combinedTopLeft, combinedTopRight, combinedBottomLeft); 

        // Change icon color based on coordinates
        //icon.GetComponent<Interactible>().OriginalMaterial = GetMaterial(topLeft.y, topRight.y, bottomRight.y, bottomLeft.y, icon);

        // Add icon to dictionary to prevent duplicates later on
        int key = LevenshteinDistance.GetLevenshteinKey(runningText);
        List<GameObject> iconList;

        if (iconDictionary.TryGetValue(key, out iconList))
        {
            iconList.Add(icon);
        } else
        {
            iconList = new List<GameObject>();
            iconList.Add(icon);
            iconDictionary.Add(key, iconList);
        }

        // Update counts and indicators
        NumIcons++;
        numIconsDetectedInOneCall++;
        NewTextDetected = true;
    }

    /// <summary>
    /// Returns true if there is a duplicate icon in the scene. Uses locality hashing to do so
    /// </summary>
    Boolean SameIconExists(Vector3 point, string text)
    {
        int key = LevenshteinDistance.GetLevenshteinKey(text);
        List<GameObject> iconList;

        // Compare icon against all other icons that have a similar text
        if (iconDictionary.TryGetValue(key, out iconList))
        {
            foreach(GameObject go in iconList)
            {
                string goText = "";// go.GetComponent<IconAction>().Text;

                if (LevenshteinDistance.GetLevenshteinDistance(text, goText) < LevenshteinDistance.ToleranceLevel && Vector3.Distance(point, go.transform.position) < 0.135)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Read all icons in front of the user
    /// </summary>
    public void ReadAllIconsInView()
    {
        Camera mainCamera = Camera.main;
        List<GameObject> iconsInView = FindAllIconsInView(mainCamera);

        // Read total number of icons in the scene
        if (iconsInView.Count == 0)
        {
            GetComponent<TextToSpeechManager>().SpeakText("There are no icons in the scene");
            return;
        } else
        {
            GetComponent<TextToSpeechManager>().SpeakText("There are " + iconsInView.Count + " icons in the scene");
        }

        // Sort by icons that are closest to user 
        iconsInView.Sort(delegate (GameObject a, GameObject b)
        {
            float distA = Vector3.Distance(a.transform.position, mainCamera.transform.position);
            float distB = Vector3.Distance(b.transform.position, mainCamera.transform.position);

            if (distA < distB) return -1;
            else return 1;
        });

        // Read each individual icon
        int num = 1;
        foreach (var icon in iconsInView)
        {
            //GetComponent<TextToSpeechManager>().SpeakText("Icon " + num + " says " + icon.GetComponent<IconAction>().Text);
            num++;
        }
    }

    /// <summary>
    /// Find all icons in front of the user
    /// </summary>
    List<GameObject> FindAllIconsInView(Camera camera)
    {
        List<GameObject> iconsInView = new List<GameObject>();

        // Find all icons and see if they are in view
        var icons = GameObject.FindGameObjectsWithTag("Icon");

        foreach (var icon in icons)
        {
            if (icon.GetComponent<Renderer>().isVisible == true)            // See if icon is in view
            {
                iconsInView.Add(icon);
            }
        }

        return iconsInView;
    }

    /// <summary>
    /// Change circle icon scale given the raycasts to the top left, top right, and bottom left
    /// </summary>
    void ChangeCircleIconScale(GameObject icon, Vector3 topLeft, Vector3 topRight, Vector3 bottomLeft)
    {
        float widthSize = GetWorldObjectWidthFromRaycast(topLeft, topRight);
        float heightSize = GetWorldObjectHeightFromRaycast(topLeft, bottomLeft);

        float radius = Math.Max(widthSize, heightSize) * ScaleIconRadius;
        radius = Math.Max(radius, MinimumIconRadius);
        radius = Math.Min(radius, MaximumIconRadius);

        icon.transform.localScale = new Vector3(radius, radius, radius);
    }

    /// <summary>
    /// Get width of object in real space, using the top left and top right coners
    /// </summary>
    float GetWorldObjectWidthFromRaycast(Vector3 topLeft, Vector3 topRight)
    {
        Ray topLeftRay = raycastCamera.ScreenPointToRay(topLeft);
        Ray topRightRay = raycastCamera.ScreenPointToRay(topRight);

        RaycastHit topLeftHit;
        RaycastHit topRightHit;

        // Find corners on the spatial mapping
        bool foundTopLeft = Physics.Raycast(topLeftRay, out topLeftHit, 15.0f, RaycastLayer);
        bool foundTopRight = Physics.Raycast(topRightRay, out topRightHit, 15.0f, RaycastLayer);
        bool foundCorners = foundTopLeft && foundTopRight;

        if (!foundCorners)
        {
            return -1f;
        }

        float widthSize = Vector3.Distance(topRightHit.point, topLeftHit.point) * 2.0f;
        return widthSize;
    }

    /// <summary>
    /// Get height of object in real space,  using the top left and bottom right coners
    /// </summary>
    float GetWorldObjectHeightFromRaycast(Vector3 topLeft, Vector3 bottomLeft)
    {
        Ray topLeftRay = raycastCamera.ScreenPointToRay(topLeft);
        Ray bottomLeftRay = raycastCamera.ScreenPointToRay(bottomLeft);

        RaycastHit topLeftHit;
        RaycastHit bottomLeftHit;

        // Find corners on the spatial mapping

        bool foundTopLeft = Physics.Raycast(topLeftRay, out topLeftHit, 15.0f, RaycastLayer);
        bool foundBottomLeft = Physics.Raycast(bottomLeftRay, out bottomLeftHit, 15.0f, RaycastLayer);
        bool foundCorners = foundTopLeft && foundBottomLeft;

        if (!foundCorners)
        {
            return -1f;
        }

        float heightSize = Vector3.Distance(topLeftHit.point, bottomLeftHit.point) * 2.0f;
        return heightSize;
    }

    /// <summary>
    /// Read icons to the user, not caring about placing visible icons anywhere else
    /// </summary>
    private void RunAudioOnlyMode(JSONArray texts)
    {
        string entireString = "";

        foreach (JSONNode text in texts)
        {
            // Extract Text
            String currText = text["description"];

            if (currText.IndexOf('\n') != -1)                                                           // Skip first result
            {
                continue;
            }

            entireString = entireString + " " + currText;
        }

        GetComponent<TextToSpeechManager>().SpeakText(entireString);
    }

}
