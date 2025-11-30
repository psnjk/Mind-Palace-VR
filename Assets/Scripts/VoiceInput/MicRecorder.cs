using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

// Code taken from https://www.patreon.com/posts/project-source-107788463 and modified a bit

public class MicRecorder : MonoBehaviour
{
    private const int CLIP_LENGTH = 20;
    private const int CLIP_FREQUENCY = 16000;

    [SerializeField] private InputActionReference leftActivateAction;
    [SerializeField] private InputActionReference leftSelectAction;
    [SerializeField] private InputActionReference rightActivateAction;
    [Space]
    [Header("Used to trim audio:")]
    [SerializeField, Range(0.0f, 0.1f)] private float silenceThreshold = 0.02f; // Threshold to consider as silence
    [SerializeField, Range(0.0f, 1.0f)] private float minSilenceLength = 0.5f; // Minimum length of silence to trim, in seconds
    [Space]
    [Header("Audio Save Settings:")]
    [SerializeField] private bool saveRecordingsToFile = true; // Toggle to save recordings automatically
    [Space]
    [SerializeField] private RunWhisper RunWhisper; // This is the reference to the RunWhisper script

    private AudioClip audioClip; //clip to which the microphone output is recorded
    private string deviceName;  //holds the name of the detected Microphone device
    private bool isRecording;
    private string saveDirectory; // Directory where audio files will be saved

    void Start()
    {
        //Debug.Log("* Hold down the Left Trigger to Record.");
        //Debug.Log("* Release the Left Trigger to stop Recording.");
        //Debug.Log("* Press the Right Trigger to Transcribe.");

        // Setup save directory
        saveDirectory = Path.Combine(Application.persistentDataPath, "Recordings");
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }
        Debug.Log("[MicRecorder] Save directory: " + saveDirectory);


        if (Microphone.devices.Length > 0)
        {
            Debug.Log("[MicRecorder] -> Microphones: " + Microphone.devices.Length);
            deviceName = Microphone.devices[0];

            if (Microphone.devices.Length > 1)
            { //There is more than one Mic available ...
                for (int i = 0; i < Microphone.devices.Length; i++)
                {
                    Debug.Log("[MicRecorder] " + Microphone.devices[i]);
                    string device = Microphone.devices[i].ToUpper();
                    if (device.Contains("ANDROID") || device.Contains("OCULUS"))
                    {
                        deviceName = Microphone.devices[i];
                    }
                }
            }

            // On the MetaQuest the device name should be "Android audio input"
            Debug.Log("[MicRecorder] Microphone Name: " + deviceName);
        }
        else
        {
            Debug.Log("[MicRecorder] No Microphone");
            Debug.LogError("[MicRecorder] -> No Microphone found! :(");
        }

        leftActivateAction.action.performed += OnLeftActivateAction;        //Start Recording
        leftActivateAction.action.canceled += OnLeftActivateActionCanceled; //End Recording
        leftSelectAction.action.performed += OnLeftSelectAction;        // Playback
        rightActivateAction.action.performed += OnRightActivateAction;  // Send Recording
    }

    // Update is called once per frame
    void Update()
    {
        if (isRecording)
        {
            if (Microphone.GetPosition(deviceName) >= audioClip.samples)
            {
                StopRecording();
            }
        }
    }

    private void OnLeftActivateAction(InputAction.CallbackContext obj)
    {
        //Debug.Log("-> OnLeftActivateAction()");
        StartRecording();
    }

    private void OnLeftActivateActionCanceled(InputAction.CallbackContext obj)
    {
        StopRecording();
        TrimSilence();

        if (audioClip.channels > 1)
        { //We have a stereo recording...
          //We want to feed a mono audioClip to the 'Whisper' model.
            ConvertToMono();
        }

        // Save the recording to file for debugging
        if (saveRecordingsToFile && audioClip != null)
        {
            SaveAudioClipToFile();
        }
    }

    private void OnLeftSelectAction(InputAction.CallbackContext obj)
    {
       // PlayRecording();
    }

    private void OnRightActivateAction(InputAction.CallbackContext obj)
    {
        TranscribeUsingWhisper();
    }

    private void StartRecording()
    {
        //if (RunWhisper.IsReady && !isRecording)
        //{ //Only start recording if Whisper is ready
        Debug.Log("[MicRecorder] -> StartRecording()");
        audioClip = Microphone.Start(deviceName, false, CLIP_LENGTH, CLIP_FREQUENCY);
        isRecording = true;
        //}
    }

    private void StopRecording()
    {
        if (isRecording)
        {
            Debug.Log("[MicRecorder] -> StopRecording() - " + PrintAudioClipDetail(audioClip));
            Microphone.End(deviceName);
            audioClip.name = "Recording";
            isRecording = false;
        }
    }

    public void TrimSilence()
    {
        if (!isRecording)
        {
            if (audioClip is null)
            {
                Debug.LogError("[MicRecorder] -> m_clip is NULL! :(");
                return;
            }

            int channels = audioClip.channels;
            int frequency = audioClip.frequency;
            int samples = audioClip.samples;

            float[] audioData = new float[samples * channels];
            audioClip.GetData(audioData, 0);

            bool isSilent = false;
            float silenceStart = 0;
            var trimmedSamples = new List<float>();

            for (int i = 0; i < audioData.Length; i += channels)
            {
                float volume = Mathf.Abs(audioData[i]); // Simple volume estimation
                if (volume < silenceThreshold)
                {
                    if (!isSilent)
                    {
                        isSilent = true;
                        silenceStart = i / (float)(frequency * channels);
                    }
                }
                else
                {
                    if (isSilent)
                    {
                        float silenceDuration = i / (float)(frequency * channels) - silenceStart;
                        if (silenceDuration < minSilenceLength)
                        {
                            // Add the "silence" back, as it's too short to be considered true silence
                            for (int j = (int)(silenceStart * frequency * channels); j < i; j++)
                            {
                                trimmedSamples.Add(audioData[j]);
                            }
                        }
                        isSilent = false;
                    }
                    else
                    {
                        trimmedSamples.Add(audioData[i]);
                    }
                }
            }

            if (trimmedSamples.Count > 0)
            {// Create a new AudioClip
                AudioClip trimmedClip = AudioClip.Create(audioClip.name + "_Trimmed", trimmedSamples.Count, channels, frequency, false);
                trimmedClip.SetData(trimmedSamples.ToArray(), 0);
                audioClip = trimmedClip; // Replace the old clip with the trimmed clip
                Debug.Log("[MicRecorder] -> TrimSilence() - " + PrintAudioClipDetail(audioClip));
            }
        }
    }

    public void ConvertToMono()
    {
        int channels = audioClip.channels; // Typically 2 for stereo
        int samples = audioClip.samples;   // Number of samples per channel

        float[] stereoData = new float[samples * channels];
        audioClip.GetData(stereoData, 0);

        float[] monoData = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float sum = 0f;

            // Sum all the channel values for this sample
            for (int j = 0; j < channels; j++)
            {
                sum += stereoData[i * channels + j];
            }

            // Average the sum to get the mono sample value
            monoData[i] = sum / channels;
        }

        // Create a new AudioClip in mono and set the data
        AudioClip monoClip = AudioClip.Create(audioClip.name + "_Mono", samples, 1, audioClip.frequency, false);
        monoClip.SetData(monoData, 0);
        audioClip = monoClip; // Replace the old clip with the trimmed clip
        Debug.Log("[MicRecorder] -> ConvertToMono() - " + PrintAudioClipDetail(audioClip));
    }

    private string PrintAudioClipDetail(AudioClip clip)
    {
        string details = "clip secs: " + audioClip.length + ", samp: " + audioClip.samples + ", chan: " + audioClip.channels + ", freq: " + audioClip.frequency;
        return details;
    }

    private void TranscribeUsingWhisper()
    {
        if (RunWhisper != null && !isRecording && audioClip != null)
        {
            if (RunWhisper.IsReady)
            {
                Debug.Log("[MicRecorder] -> Starting transcription with Whisper");
                RunWhisper.Transcribe(audioClip);
                DestroyImmediate(audioClip);
                Debug.Log("[MicRecorder] Audio clip successfully cleaned from memory");
            }
            else
            {
                Debug.LogWarning("[MicRecorder] Whisper is not ready (still initializing or transcribing)");
            }
        }
        else
        {
            if (RunWhisper == null)
                Debug.LogError("[MicRecorder] RunWhisper reference is null!");
            if (isRecording)
                Debug.LogWarning("[MicRecorder] Cannot transcribe while recording!");
            if (audioClip == null)
                Debug.LogWarning("[MicRecorder] No audio clip to transcribe!");
        }
    }

    /// <summary>
    /// Saves the current audio clip to a WAV file
    /// </summary>
    private void SaveAudioClipToFile()
    {
        if (audioClip == null)
        {
            Debug.LogError("[MicRecorder] Cannot save - audioClip is null!");
            return;
        }

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"Recording_{timestamp}.wav";
        string filePath = Path.Combine(saveDirectory, fileName);

        try
        {
            SaveWav(filePath, audioClip);
            Debug.Log($"[MicRecorder] Audio saved to: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MicRecorder] Failed to save audio: {e.Message}");
        }
    }

    /// <summary>
    /// Public method to manually save the current recording
    /// </summary>
    public void SaveCurrentRecording()
    {
        SaveAudioClipToFile();
    }

    /// <summary>
    /// Get the last transcription result from Whisper
    /// </summary>
    public string GetLastTranscription()
    {
        if (RunWhisper != null)
        {
            return RunWhisper.LastTranscription;
        }
        return "";
    }

    /// <summary>
    /// Check if Whisper is ready for transcription
    /// </summary>
    public bool IsWhisperReady()
    {
        return RunWhisper != null && RunWhisper.IsReady;
    }

    /// <summary>
    /// Saves an AudioClip as a WAV file
    /// </summary>
    private static void SaveWav(string filePath, AudioClip clip)
    {
        // Get audio data
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        // Convert to 16-bit PCM
        short[] intData = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(samples[i] * 32767);
        }

        // Create WAV file
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        using (var writer = new BinaryWriter(fileStream))
        {
            // WAV Header
            writer.Write("RIFF".ToCharArray()); // ChunkID
            writer.Write((uint)(36 + intData.Length * 2)); // ChunkSize
            writer.Write("WAVE".ToCharArray()); // Format

            // fmt subchunk
            writer.Write("fmt ".ToCharArray()); // Subchunk1ID
            writer.Write((uint)16); // Subchunk1Size (16 for PCM)
            writer.Write((ushort)1); // AudioFormat (1 for PCM)
            writer.Write((ushort)clip.channels); // NumChannels
            writer.Write((uint)clip.frequency); // SampleRate
            writer.Write((uint)(clip.frequency * clip.channels * 2)); // ByteRate
            writer.Write((ushort)(clip.channels * 2)); // BlockAlign
            writer.Write((ushort)16); // BitsPerSample

            // data subchunk
            writer.Write("data".ToCharArray()); // Subchunk2ID
            writer.Write((uint)(intData.Length * 2)); // Subchunk2Size

            // Write audio data
            foreach (short sample in intData)
            {
                writer.Write(sample);
            }
        }
    }

    void OnDestroy()
    {
        leftActivateAction.action.performed -= OnLeftActivateAction;
        leftActivateAction.action.canceled -= OnLeftActivateActionCanceled;
        leftSelectAction.action.performed -= OnLeftSelectAction;
        rightActivateAction.action.performed -= OnRightActivateAction;
    }
}