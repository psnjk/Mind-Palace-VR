using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

// Code taken from https://www.patreon.com/posts/project-source-107788463 and modified a bit

public class VoiceInput : MonoBehaviour
{

   
    private const int CLIP_FREQUENCY = 16000; // sampling rate
    private float _timer = 0f;


    [SerializeField] private RunWhisper RunWhisper; // This is the reference to the RunWhisper script
    [Header("Used to trim audio:")]
    [SerializeField, Range(0.0f, 0.1f)] private float silenceThreshold = 0.02f; // Threshold to consider as silence
    [SerializeField, Range(0.0f, 1.0f)] private float minSilenceLength = 0.5f; // Minimum length of silence to trim, in seconds
    [Space]
    [Header("Audio Save Settings:")]

    [Tooltip("Maximum clip length in seconds")]
    [Range(1, 30)]
    [SerializeField]
    private int CLIP_LENGTH = 20; // seconds
    [SerializeField] private bool saveRecordingsToFile = true; // Toggle to save recordings for debug purposes
    [Space]

    private AudioClip audioClip; //clip to which the microphone output is recorded
    private string deviceName;  //holds the name of the detected Microphone device
    private bool _isRecording;
    private bool _isTranscribing;
    private string _saveDirectory; // Directory where audio files will be saved

    private TMP_InputField _focusedField;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Setup save directory
        _saveDirectory = Path.Combine(Application.persistentDataPath, "Recordings");
        if (!Directory.Exists(_saveDirectory))
        {
            Directory.CreateDirectory(_saveDirectory);
        }
        Debug.Log($"[VoiceInput] Save directory: {_saveDirectory}");

        int numberOfMicrophones = Microphone.devices.Length;

        if (numberOfMicrophones > 0)
        {
            Debug.Log($"[VoiceInput] -> Number of detected microphones: {numberOfMicrophones}");
            // take the first one as default
            deviceName = Microphone.devices[0];

            // if we have more than one, find out which one is on the oculus headset to avoid using the PC's mic
            if (numberOfMicrophones > 1)
            {
                for (int i = 0; i < numberOfMicrophones; i++)
                {
                    Debug.Log("[VoiceInput] " + Microphone.devices[i]);
                    string device = Microphone.devices[i].ToUpper();
                    if (device.Contains("ANDROID") || device.Contains("OCULUS"))
                    {
                        deviceName = Microphone.devices[i];
                    }
                }
            }

            Debug.Log("[VoiceInput] Microphone Name: " + deviceName);
        }
        else
        {
            Debug.LogWarning("[VoiceInput] No Microphone");
        }
    }

    [ContextMenu("Speech to text")]
    public void OnVoiceInputButtonClicked()
    {
        if (!_isRecording && !_isTranscribing)
        {
            // lock the currently focused input field
            _focusedField = InputFieldFocusManager.Instance.GetFocusedField();
            StartRecording();
        }
        else
        {
            StopAndTranscribe();
        }
    }



    /// <summary>
    /// Starts recording for CLIP_LENGTH seconds at CLIP_FREQUENCY sampling rate and saves the result in audioClip 
    /// </summary>
    private void StartRecording()
    {
        Debug.Log("[VoiceInput] -> StartRecording()");
        audioClip = Microphone.Start(deviceName, false, CLIP_LENGTH, CLIP_FREQUENCY);
        _isRecording = true;
        HUD.Instance.SetMicIcon(true);
        _timer = 0f;
    }

    /// <summary>
    /// Stops recording 
    /// </summary>
    private void StopRecording()
    {
        if (_isRecording)
        {
            Debug.Log($"[VoiceInput] -> StopRecording() - {PrintAudioClipDetail(audioClip)}");
            Microphone.End(deviceName);
            audioClip.name = "Recording";
            _isRecording = false;
            HUD.Instance.SetMicIcon(false);
        }
    }

    private async void StopAndTranscribe()
    {
        try
        {
            StopRecording();
            TrimSilence();

            // if the clip is stereo, make it mono since that Whisper only accepts mono
            if (audioClip.channels > 1)
            {
                ConvertToMono();
            }

            // save recording to file if requested
            if (saveRecordingsToFile && audioClip != null)
            {
                SaveAudioClipToFile();
            }

            _isTranscribing = true;
            HUD.Instance.SetTranscribeIcon(true);
            string outputText = await TranscribeUsingWhisper();
            _isTranscribing = false;
            HUD.Instance.SetTranscribeIcon(false);

            // write output to focused input field
            InputFieldFocusManager.Instance.InsertTextInField(outputText.Trim(), _focusedField);
        }
        catch (Exception e)
        {
            Debug.LogError($"[VoiceInput] Error occured during StopAndTranscribe:\n{e}");
            _isTranscribing = false;
            HUD.Instance.SetTranscribeIcon(false);
        }
    }

    private async Task<string> TranscribeUsingWhisper()
    {
        string outputText = "";
        if (RunWhisper != null && !_isRecording && audioClip != null)
        {
            if (RunWhisper.IsReady)
            {
                Debug.Log("[VoiceInput] -> Starting transcription with Whisper");
                outputText = await RunWhisper.Transcribe(audioClip);
                DestroyImmediate(audioClip);
                Debug.Log("[VoiceInput] Audio clip successfully cleaned from memory");
            }
            else
            {
                Debug.LogWarning("[VoiceInput] Whisper is not ready (still initializing or transcribing)");
            }
        }
        else
        {
            if (RunWhisper == null)
                Debug.LogError("[VoiceInput] RunWhisper reference is null!");
            if (_isRecording)
                Debug.LogWarning("[VoiceInput] Cannot transcribe while recording!");
            if (audioClip == null)
                Debug.LogWarning("[VoiceInput] No audio clip to transcribe!");
        }

        return outputText;
    }



    private string PrintAudioClipDetail(AudioClip clip)
    {
        string details = "clip secs: " + audioClip.length + ", samp: " + audioClip.samples + ", chan: " + audioClip.channels + ", freq: " + audioClip.frequency;
        return details;
    }


    // Update is called once per frame
    void Update()
    {
        if (_isRecording)
        {
            _timer += Time.deltaTime;

            if (_timer >= CLIP_LENGTH)
            {
                Debug.Log("[VoiceInput] Max time reached, stopping recording.");
                StopAndTranscribe();
            }
        }
    }

    public void TrimSilence()
    {
        if (!_isRecording)
        {
            if (audioClip is null)
            {
                Debug.LogError("[VoiceInput] -> m_clip is NULL! :(");
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
                Debug.Log("[VoiceInput] -> TrimSilence() - " + PrintAudioClipDetail(audioClip));
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
        Debug.Log("[VoiceInput] -> ConvertToMono() - " + PrintAudioClipDetail(audioClip));
    }

    /// <summary>
    /// Saves the current audio clip to a WAV file
    /// </summary>
    private void SaveAudioClipToFile()
    {
        if (audioClip == null)
        {
            Debug.LogError("[VoiceInput] Cannot save - audioClip is null!");
            return;
        }

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"Recording_{timestamp}.wav";
        string filePath = Path.Combine(_saveDirectory, fileName);

        try
        {
            SaveWav(filePath, audioClip);
            Debug.Log($"[VoiceInput] Audio saved to: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[VoiceInput] Failed to save audio: {e.Message}");
        }
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



}
