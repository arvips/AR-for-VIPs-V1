using UnityEngine;
using UnityEngine.XR.WSA.WebCam;
using System.Linq;
using System;

public class CameraManager : MonoBehaviour
{
    // Camera
    public static Resolution cameraResolution;
    public static Camera raycastCamera;
    public PhotoCapture photoCaptureObject = null;
    public Matrix4x4 managerCameraToWorldMatrix;
    [HideInInspector]
    public Vector3 oldHoloPos;
    [HideInInspector]
    public Quaternion oldHoloRot;

    public GameObject newCameraPrefab;

    // Indicators
    private Boolean detecting { get; set; }                                  // Indicator to prevent user from calling the API multiple times too quickly (e.g. double clicks on the clicker)

    public string image { get; set; }                                       // Image to send to API in Base64
    private SettingsManager SettingsManager;

    public void Start()
    {

        

        SettingsManager = gameObject.GetComponent<SettingsManager>();

        detecting = false;


        // Set resolution of camera
        var cameraResolutions = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height);           // Get all possible resolutions

        if (SettingsManager.ResolutionLevel == ResolutionSetting.High)
        {
            cameraResolution = cameraResolutions.First();                   // TODO: high resolution still doesn't work
        }
        else
        {
            foreach (Resolution r in cameraResolutions)
            {
                if (r.width == 1280 || r.height == 720)
                {
                    cameraResolution = r;
                    break;
                }
            }
        }

    }

    /// <summary>
    /// Begin manual capture of photo by creating a PhotoCaptureObject
    /// </summary>
    public void BeginManualPhotoMode ()
    {
        oldHoloPos = new Vector3(0f, 0f, 0f);
        oldHoloRot = Quaternion.identity;
        Debug.Log("photmode");
        SettingsManager = gameObject.GetComponent<SettingsManager>();

        detecting = false;


        // Set resolution of camera
        var cameraResolutions = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height);           // Get all possible resolutions

        if (SettingsManager.ResolutionLevel == ResolutionSetting.High)
        {
            cameraResolution = cameraResolutions.First();                   // TODO: high resolution still doesn't work
        }
        else
        {
            foreach (Resolution r in cameraResolutions)
            {
                if (r.width == 1280 || r.height == 720)
                {
                    cameraResolution = r;
                    break;
                }
            }
        }

        if (GetComponent<IconManager>().NumIcons > SettingsManager.MaxIcons)
        {
            GetComponent<TextToSpeechManager>().SpeakText("There are too many icons. Please clear icons and try again.");
            return;
        }

        if (detecting == true)
        {
            return;
        }

        detecting = true;
        Debug.Log("before async");
        GameObject newCam = GameObject.FindGameObjectWithTag("MainCamera");
        Debug.Log(newCam);
        oldHoloPos = newCam.transform.position;
        Debug.Log(oldHoloPos);
        oldHoloRot = newCam.transform.rotation;
        Debug.Log(oldHoloRot);
        //StartCoroutine(GetComponent<TextReco>().GoogleRequest());
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
    }

    /// <summary>
    /// Callback to when PhotoCaptureObject is created. Start PhotoMode once object is created
    /// </summary>
    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        Debug.Log("photocapture");
        photoCaptureObject = captureObject;
        
        // Set camera properties
        CameraParameters c = new CameraParameters();
        c.hologramOpacity = 0.0f;
        c.cameraResolutionWidth = cameraResolution.width;
        c.cameraResolutionHeight = cameraResolution.height;
        c.pixelFormat = CapturePixelFormat.BGRA32;

        captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
    }

    /// <summary>
    /// Callback to start PhotoMode. Take a photo once PhotoMode starts
    /// </summary>
    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        if (result.success)
        {
            BeginOCRProcess();
        }
        else
        {
            StopPhotoMode();
        }
    }

    /// <summary>
    /// Begin OCR process. Take photo repeatedly if in automatic/hybrid mode.
    /// </summary>
    public void BeginOCRProcess()
    {
        TakePhoto();

    }

    /// <summary>
    /// Take photo
    /// </summary>
    private void TakePhoto()
    {
        Debug.Log("CM: takephoto");
        GetComponent<TextToSpeechManager>().SpeakText("Detecting Text");
        Debug.Log("CM: Text to speech manager retrieved.");

        // Keep cameraToWorldMatrix for placing icons into the real world and take photo
        managerCameraToWorldMatrix = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>().cameraToWorldMatrix;
        Debug.Log("CM: managerCameraToWorldMatrix set.");
        photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        Debug.Log("CM: photoCaptureObject TakePhoto Async activated.");
    }
    
    public Matrix4x4 getManagerCamera()
    {
        return managerCameraToWorldMatrix;
    }

    /// <summary>
    /// Put photo into RAM
    /// </summary>
    private void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        Debug.Log("CM: OnCapturedPhotoToMemory called.");
        if (result.success)
        {
            // Convert image into image bytes
            Debug.Log("CM: OCPtM: result.success if statement triggered.");
            Texture2D targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);                               // Create our Texture2D for use and set the correct resolution
            photoCaptureFrame.UploadImageDataToTexture(targetTexture);                                                              // Copy the raw image data into our target texture, then convert to byte array
            byte[] imageBytes = targetTexture.EncodeToPNG();

            Debug.Log("CM: OCPM: PreCoroutine");
            StartCoroutine(GetComponent<TextReco>().GoogleRequest(imageBytes));                                                   // Begin Google API call
            Debug.Log("CM: OCPM: PostCoroutine");
            if (SettingsManager.OCRSetting == OCRRunSetting.Manual)                                                                 // Stop PhotoMode if in manual mode (restart on another call)
            {
                GetComponent<CameraManager>().StopPhotoMode();
            }
        }
    }

    public void StopPhotoMode()
    {
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    /// <summary>
    /// Callback to stop photo mode
    /// </summary>
    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }

    /// <summary>
    /// Position camera
    /// </summary>
    public Camera PositionCamera(Matrix4x4 cameraToWorldMatrix)
    {
        Vector3 position = cameraToWorldMatrix.MultiplyPoint(Vector3.zero);
        Quaternion rotation = Quaternion.LookRotation(-cameraToWorldMatrix.GetColumn(2), cameraToWorldMatrix.GetColumn(1));
        //Camera raycastCamera = this.gameObject.GetComponentInChildren<Camera>();
        Camera newCamera = Instantiate(newCameraPrefab).GetComponent<Camera>() ;
        newCamera.transform.position = oldHoloPos;
        newCamera.transform.rotation = oldHoloRot;

        return newCamera;
    }
}