using UnityEngine;
using System.Collections;

/// <summary>
/// Type of mode for certain users
/// </summary>
public enum UserType
{
    /// <summary>
    /// The default
    /// </summary>
    Default,

    /// <summary>
    /// Audio-only mode for those that can't see the icons
    /// </summary>
    AudioOnly,
}

public enum ResolutionSetting
{
    /// <summary>
    /// Default to low
    /// </summary>
    Default,

    /// <summary>
    /// 2048x1152
    /// </summary>
    High,

    /// <summary>
    /// 1280x720
    /// </summary>
    Low,
}

public enum OCRRunSetting
{
    /// <summary>
    /// Manual
    /// </summary>
    Manual,
}

public enum ApiSetting
{
    /// <summary>
    /// Google
    /// </summary>
    Google,

}

public class SettingsManager : MonoBehaviour {
    [Tooltip("Select Mode for Type of User.")]
    public UserType UserSetting;

    [Tooltip("Select Cursor to turn off during audio only mode.")]
    public GameObject CursorObject;

    [Tooltip("Select the resolution setting.")]
    public ResolutionSetting ResolutionLevel;

    [Tooltip("Select interaction setting of OCR.")]
    public OCRRunSetting OCRSetting;

    [Tooltip("Select the OCR API to use.")]
    public ApiSetting ApiType;

    [Tooltip("Maximum Number of icons shown.")]
    public int MaxIcons = 30;

    [Tooltip("Maximum text length (in characters) to read aloud.")]
    public int MaxTextLength = 60;

    // Use this for initialization
    void Start () {
        if (UserSetting == UserType.AudioOnly)
        {
            CursorObject.SetActive(false);
        }
    }

    /// <summary>
    /// Switch to audio-only mode
    /// </summary>
    public void SwitchToAudioMode()
    {
        UserSetting = UserType.AudioOnly;
        CursorObject.SetActive(false);
        GetComponent<TextToSpeechManager>().SpeakText("Switched to Audio-only Mode");
    }

    /// <summary>
    /// Switch to default mode to show icons
    /// </summary>
    public void SwitchToIconMode()
    {
        UserSetting = UserType.Default;
        CursorObject.SetActive(true);
        GetComponent<TextToSpeechManager>().SpeakText("Switched to Icon Mode");
    }
}
