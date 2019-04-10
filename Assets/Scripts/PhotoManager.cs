using UnityEngine;
using UnityEngine.VR.WSA.WebCam;
using System.Linq;
using System;
using TextReco

public class CameraManager : MonoBehaviour
{
    // Camera
    public static Resolution cameraResolution;
    public static Camera raycastCamera;
    private PhotoCapture photoCaptureObject = null;

    // Indicators
    public Boolean detecting { get; set; }                                  

    public string image { get; set; }           
    private SettingsManager SettingsManager;

    void Start()
    {
        SettingsManager = gameObject.GetComponent<SettingsManager>();

        detecting = false;


        // Set resolution of camera
        var cameraResolutions = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height);     

        if (SettingsManager.ResolutionLevel == ResolutionSetting.High)
        {
            cameraResolution = cameraResolutions.First();         
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
        if (detecting == true)
        {
            return;
        }

        detecting = true;
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
    }

    /// <summary>
    /// Callback to when PhotoCaptureObject is created. Start PhotoMode once object is created
    /// </summary>
    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        photoCaptureObject = captureObject;
        
        // Set camera properties
        CameraParameters c = new CameraParameters();
        c.hologramOpacity = 0.0f;
        c.cameraResolutionWidth = cameraResolution.width;
        c.cameraResolutionHeight = cameraResolution.height;
        c.pixelFormat = CapturePixelFormat.BGRA32;

        captureObject.StartPhotoModeAsync(c, false, OnPhotoModeStarted);
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
        photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
    }

    /// <summary>
    /// Put photo into RAM
    /// </summary>
    private void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        if (result.success)
        {
            // Convert image into image bytes
            Texture2D targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);                               // Create our Texture2D for use and set the correct resolution
            photoCaptureFrame.UploadImageDataToTexture(targetTexture);                                                              // Copy the raw image data into our target texture, then convert to byte array
            byte[] imageBytes = targetTexture.EncodeToPNG();

            //StartCoroutine(GetComponent<ApiManager>().GoogleRequest(imageBytes));                                                   // Begin Google API call
            TextReco.GoogleRequest(imageBytes);
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
}