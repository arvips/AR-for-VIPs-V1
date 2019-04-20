// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using HoloToolkit.Unity;
using System;
using UnityEngine;

#if WINDOWS_UWP
using Windows.Foundation;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;
using System.Linq;
using System.Threading.Tasks;
#endif


/// <summary>
/// The well-know voices that can be used by <see cref="TextToSpeechManager"/>.
/// </summary>
public enum TextToSpeechVoice
    {
        /// <summary>
        /// The default system voice.
        /// </summary>
        Default,

        /// <summary>
        /// Microsoft David Mobile
        /// </summary>
        David,

        /// <summary>
        /// Microsoft Mark Mobile
        /// </summary>
        Mark,

        /// <summary>
        /// Microsoft Zira Mobile
        /// </summary>
        Zira,
    }

    /// <summary>
    /// Enables text to speech using the Windows 10 <see cref="SpeechSynthesizer"/> class.
    /// </summary>
    /// <remarks>
    /// <see cref="SpeechSynthesizer"/> generates speech as a <see cref="SpeechSynthesisStream"/>. 
    /// This class converts that stream into a Unity <see cref="AudioClip"/> and plays the clip using 
    /// the <see cref="AudioSource"/> you supply in the inspector. This allows you to position the voice 
    /// as desired in 3D space. One recommended approach is to place the AudioSource on an empty 
    /// GameObject that is a child of Main Camera and position it approximately 0.6 units above the 
    /// camera. This orientation will sound similar to Cortana's speech in the OS.
    /// </remarks>
    public class TextToSpeechManager : IntervalWorkQueue
    {
        // Inspector Variables
        [Tooltip("The audio source where speech will be played.")]
        [SerializeField]
        private AudioSource audioSource;

        [Tooltip("The voice that will be used to generate speech.")]
        [SerializeField]
        private TextToSpeechVoice voice;

        [Tooltip("Select default value of text-to-speech.")]
        public bool TextToSpeechOn;

        public AudioSource AudioSource
        {
            get
            {
                return (this.audioSource);
            }
            set
            {
                this.audioSource = value;
            }
        }
        public TextToSpeechVoice Voice
        {
            get
            {
                return (this.voice);
            }
            set
            {
                this.voice = value;
            }
        }

        /// <summary>
        /// Speaks the specified SSML markup using text-to-speech.
        /// </summary>
        /// <param name="ssml">
        /// The SSML markup to speak.
        /// </param>
        public void SpeakSsml(string ssml)
        {
            // Make sure there's something to speak
            if (string.IsNullOrEmpty(ssml)) { return; }

            // Pass to helper method
    #if WINDOWS_UWP
          PlaySpeech(ssml, this.voice, synthesizer.SynthesizeSsmlToStreamAsync);
    #else
            LogSpeech(ssml);
    #endif
        }

        /// <summary>
        /// Speaks the specified text using text-to-speech.
        /// </summary>
        /// <param name="text">
        /// The text to speak.
        /// </param>
        public void SpeakText(string text)
        {
            // Make sure there's something to speak
            if (string.IsNullOrEmpty(text)) { return; }

            // Pass to helper method
    #if WINDOWS_UWP
          PlaySpeech(text, this.voice, synthesizer.SynthesizeTextToStreamAsync);
    #else
            LogSpeech(text);
    #endif
        }
        /// <summary>
        /// Logs speech text that normally would have been played.
        /// </summary>
        /// <param name="text">
        /// The speech text.
        /// </param>
        void LogSpeech(string text)
        {
            Debug.LogFormat("Speech not supported in editor. \"{0}\"", text);
        }
        public new void Start()
        {
            base.Start();

            try
            {
                if (audioSource == null)
                {
                    Debug.LogError("An AudioSource is required and should be assigned to 'Audio Source' in the inspector.");
                }
                else
                {
    #if WINDOWS_UWP
              this.synthesizer = new SpeechSynthesizer();
    #endif
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Could not start Speech Synthesis");
                Debug.LogException(ex);
            }
        }
        protected override void DoWorkItem(object item)
        {
  /**  #if WINDOWS_UWP
 
          try
          {
            SpeechEntry speechEntry = item as SpeechEntry;
 
            // Need await, so most of this will be run as a new Task in its own thread.
            // This is good since it frees up Unity to keep running anyway.
            Task.Run(async () =>
            {
              this.ChangeVoice(voice);
 
              var buffer = await UnityAudioHelper.SynthesizeToUnityDataAsync(
                speechEntry.Text,
                speechEntry.SpeechGenerator);
 
              // Convert raw WAV data into Unity audio data
              int sampleCount = 0;
              int frequency = 0;
              float[] unityData = null;
 
              unityData = UnityAudioHelper.ToUnityAudio(
                buffer, out sampleCount, out frequency);
 
              // The remainder must be done back on Unity's main thread
              UnityEngine.WSA.Application.InvokeOnAppThread(
                () =>
                {
                    // Convert to an audio clip
                    var clip = UnityAudioHelper.ToClip(
                      "Speech", unityData, sampleCount, frequency);
 
                    // Set the source on the audio clip
                    audioSource.clip = clip;
 
                    // Play audio
                    audioSource.Play();
                },
                false);
            });
          }
          catch (Exception ex)
          {
            Debug.LogErrorFormat("Speech generation problem: \"{0}\"", ex.Message);
          }
    #endif **/
        }
        protected override bool WorkIsInProgress
        {
            get
            {
    #if WINDOWS_UWP
            return (this.audioSource.isPlaying);
    #else
                return (false);
    #endif
            }
        }

#if WINDOWS_UWP
        class SpeechEntry
        {
          public string Text { get; set; }
          public TextToSpeechVoice Voice { get; set; }
          public Func<string, IAsyncOperation<SpeechSynthesisStream>> SpeechGenerator { get; set; }
        }
        private SpeechSynthesizer synthesizer;
        private VoiceInformation voiceInfo;
 
        /// <summary>
        /// Executes a function that generates a speech stream and then converts and plays it in Unity.
        /// </summary>
        /// <param name="text">
        /// A raw text version of what's being spoken for use in debug messages when speech isn't supported.
        /// </param>
        /// <param name="speakFunc">
        /// The actual function that will be executed to generate speech.
        /// </param>
        void PlaySpeech(
          string text,
          TextToSpeechVoice voice,
          Func<string, IAsyncOperation<SpeechSynthesisStream>> speakFunc)
        {
          // Make sure there's something to speak
          if (speakFunc == null)
          {
            throw new ArgumentNullException(nameof(speakFunc));
          }
 
          if (synthesizer != null)
          {
            base.AddWorkItem(
              new SpeechEntry()
              {
                Text = text,
                Voice = voice,
                SpeechGenerator = speakFunc
              }
            );
          }
          else
          {
            Debug.LogErrorFormat("Speech not initialized. \"{0}\"", text);
          }
        }
        void ChangeVoice(TextToSpeechVoice voice)
        {
          // Change voice?
          if (voice != TextToSpeechVoice.Default)
          {
            // Get name
            var voiceName = Enum.GetName(typeof(TextToSpeechVoice), voice);
 
            // See if it's never been found or is changing
            if ((voiceInfo == null) || (!voiceInfo.DisplayName.Contains(voiceName)))
            {
              // Search for voice info
              voiceInfo = SpeechSynthesizer.AllVoices.Where(v => v.DisplayName.Contains(voiceName)).FirstOrDefault();
 
              // If found, select
              if (voiceInfo != null)
              {
                synthesizer.Voice = voiceInfo;
              }
              else
              {
                Debug.LogErrorFormat("TTS voice {0} could not be found.", voiceName);
              }
            }
          }
        }
#endif // WINDOWS_UWP

    public void EnterSpeechMode()
    {
        TextToSpeechOn = true;
        this.SpeakText("Entered Speech Mode");
    }

    public void ExitSpeechMode()
    {
        TextToSpeechOn = false;
        this.SpeakText("Exited Speech Mode");
    }
}