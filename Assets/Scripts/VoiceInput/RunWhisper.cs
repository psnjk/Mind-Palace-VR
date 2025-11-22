using System.Collections.Generic;
using UnityEngine;
using Unity.InferenceEngine;
using System.Text;
using Unity.Collections;
using Newtonsoft.Json;

// Code and models taken from: https://huggingface.co/unity/inference-engine-whisper-tiny/tree/main

public class RunWhisper : MonoBehaviour
{
    Worker decoder1, decoder2, encoder, spectrogram;
    Worker argmax;

    public AudioClip audioClip;

    // This is how many tokens you want. It can be adjusted.
    const int maxTokens = 100;

    // Special tokens see added tokens file for details
    const int END_OF_TEXT = 50257;
    const int START_OF_TRANSCRIPT = 50258;
    const int ENGLISH = 50259;
    const int GERMAN = 50261;
    const int FRENCH = 50265;
    const int TRANSCRIBE = 50359; //for speech-to-text in specified language
    const int TRANSLATE = 50358;  //for speech-to-text then translate to English
    const int NO_TIME_STAMPS = 50363;
    const int START_TIME = 50364;

    int numSamples;
    string[] tokens;

    int tokenCount = 0;
    NativeArray<int> outputTokens;

    // Used for special character decoding
    int[] whiteSpaceCharacters = new int[256];

    Tensor<float> encodedAudio;

    bool transcribe = false;
    string outputString = "";
    bool isInitialized = false;
    bool isTranscribing = false;

    // Maximum size of audioClip (30s at 16kHz)
    const int maxSamples = 30 * 16000;

    public ModelAsset audioDecoder1, audioDecoder2;
    public ModelAsset audioEncoder;
    public ModelAsset logMelSpectro;

    public async void Start()
    {
        await InitializeWhisper();
    }

    public async Awaitable InitializeWhisper()
    {
        if (isInitialized) return;

        Debug.Log("[RunWhisper] Initializing Whisper models...");

        SetupWhiteSpaceShifts();
        GetTokens();

        decoder1 = new Worker(ModelLoader.Load(audioDecoder1), BackendType.GPUCompute);
        decoder2 = new Worker(ModelLoader.Load(audioDecoder2), BackendType.GPUCompute);

        FunctionalGraph graph = new FunctionalGraph();
        var input = graph.AddInput(DataType.Float, new DynamicTensorShape(1, 1, 51865));
        var amax = Functional.ArgMax(input, -1, false);
        var selectTokenModel = graph.Compile(amax);
        argmax = new Worker(selectTokenModel, BackendType.GPUCompute);

        encoder = new Worker(ModelLoader.Load(audioEncoder), BackendType.GPUCompute);
        spectrogram = new Worker(ModelLoader.Load(logMelSpectro), BackendType.GPUCompute);

        outputTokens = new NativeArray<int>(maxTokens, Allocator.Persistent);
        lastToken = new NativeArray<int>(1, Allocator.Persistent);

        isInitialized = true;
        Debug.Log("[RunWhisper] Whisper initialization complete!");
    }

    public async void Transcribe(AudioClip inputClip)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[RunWhisper] Whisper not initialized, initializing now...");
            await InitializeWhisper();
        }

        if (isTranscribing)
        {
            Debug.LogWarning("[RunWhisper] Already transcribing, please wait...");
            return;
        }

        if (inputClip == null)
        {
            Debug.LogError("[RunWhisper] Input audio clip is null!");
            return;
        }

        Debug.Log($"[RunWhisper] Starting transcription of clip: {inputClip.name} ({inputClip.length}s)");

        isTranscribing = true;
        audioClip = inputClip;
        outputString = "";

        // Reset transcription state
        tokenCount = 3;
        outputTokens[0] = START_OF_TRANSCRIPT;
        outputTokens[1] = ENGLISH;
        outputTokens[2] = TRANSCRIBE;

        try
        {
            LoadAudio();
            EncodeAudio();
            transcribe = true;

            // Initialize tensors
            tokensTensor = new Tensor<int>(new TensorShape(1, maxTokens));
            ComputeTensorData.Pin(tokensTensor);
            tokensTensor.Reshape(new TensorShape(1, tokenCount));
            tokensTensor.dataOnBackend.Upload<int>(outputTokens, tokenCount);

            lastToken[0] = NO_TIME_STAMPS;
            lastTokenTensor = new Tensor<int>(new TensorShape(1, 1), new[] { NO_TIME_STAMPS });

            // Run transcription loop
            while (transcribe && tokenCount < (outputTokens.Length - 1))
            {
                m_Awaitable = InferenceStep();
                await m_Awaitable;
            }

            Debug.Log($"[RunWhisper] Transcription complete: '{outputString}'");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RunWhisper] Transcription failed: {e.Message}");
        }
        finally
        {
            isTranscribing = false;
            // Clean up tensors
            if (tokensTensor != null)
            {
                tokensTensor.Dispose();
                tokensTensor = null;
            }
            if (lastTokenTensor != null)
            {
                lastTokenTensor.Dispose();
                lastTokenTensor = null;
            }
            if (audioInput != null)
            {
                audioInput.Dispose();
                audioInput = null;
            }
        }
    }

    public bool IsReady => isInitialized && !isTranscribing;
    public string LastTranscription => outputString;

    Awaitable m_Awaitable;

    NativeArray<int> lastToken;
    Tensor<int> lastTokenTensor;
    Tensor<int> tokensTensor;
    Tensor<float> audioInput;

    void LoadAudio()
    {
        numSamples = audioClip.samples;
        var data = new float[maxSamples];
        numSamples = maxSamples;
        audioClip.GetData(data, 0);
        audioInput = new Tensor<float>(new TensorShape(1, numSamples), data);
    }

    void EncodeAudio()
    {
        spectrogram.Schedule(audioInput);
        var logmel = spectrogram.PeekOutput() as Tensor<float>;
        encoder.Schedule(logmel);
        encodedAudio = encoder.PeekOutput() as Tensor<float>;
    }
    async Awaitable InferenceStep()
    {
        decoder1.SetInput("input_ids", tokensTensor);
        decoder1.SetInput("encoder_hidden_states", encodedAudio);
        decoder1.Schedule();

        var past_key_values_0_decoder_key = decoder1.PeekOutput("present.0.decoder.key") as Tensor<float>;
        var past_key_values_0_decoder_value = decoder1.PeekOutput("present.0.decoder.value") as Tensor<float>;
        var past_key_values_1_decoder_key = decoder1.PeekOutput("present.1.decoder.key") as Tensor<float>;
        var past_key_values_1_decoder_value = decoder1.PeekOutput("present.1.decoder.value") as Tensor<float>;
        var past_key_values_2_decoder_key = decoder1.PeekOutput("present.2.decoder.key") as Tensor<float>;
        var past_key_values_2_decoder_value = decoder1.PeekOutput("present.2.decoder.value") as Tensor<float>;
        var past_key_values_3_decoder_key = decoder1.PeekOutput("present.3.decoder.key") as Tensor<float>;
        var past_key_values_3_decoder_value = decoder1.PeekOutput("present.3.decoder.value") as Tensor<float>;

        var past_key_values_0_encoder_key = decoder1.PeekOutput("present.0.encoder.key") as Tensor<float>;
        var past_key_values_0_encoder_value = decoder1.PeekOutput("present.0.encoder.value") as Tensor<float>;
        var past_key_values_1_encoder_key = decoder1.PeekOutput("present.1.encoder.key") as Tensor<float>;
        var past_key_values_1_encoder_value = decoder1.PeekOutput("present.1.encoder.value") as Tensor<float>;
        var past_key_values_2_encoder_key = decoder1.PeekOutput("present.2.encoder.key") as Tensor<float>;
        var past_key_values_2_encoder_value = decoder1.PeekOutput("present.2.encoder.value") as Tensor<float>;
        var past_key_values_3_encoder_key = decoder1.PeekOutput("present.3.encoder.key") as Tensor<float>;
        var past_key_values_3_encoder_value = decoder1.PeekOutput("present.3.encoder.value") as Tensor<float>;

        decoder2.SetInput("input_ids", lastTokenTensor);
        decoder2.SetInput("past_key_values.0.decoder.key", past_key_values_0_decoder_key);
        decoder2.SetInput("past_key_values.0.decoder.value", past_key_values_0_decoder_value);
        decoder2.SetInput("past_key_values.1.decoder.key", past_key_values_1_decoder_key);
        decoder2.SetInput("past_key_values.1.decoder.value", past_key_values_1_decoder_value);
        decoder2.SetInput("past_key_values.2.decoder.key", past_key_values_2_decoder_key);
        decoder2.SetInput("past_key_values.2.decoder.value", past_key_values_2_decoder_value);
        decoder2.SetInput("past_key_values.3.decoder.key", past_key_values_3_decoder_key);
        decoder2.SetInput("past_key_values.3.decoder.value", past_key_values_3_decoder_value);

        decoder2.SetInput("past_key_values.0.encoder.key", past_key_values_0_encoder_key);
        decoder2.SetInput("past_key_values.0.encoder.value", past_key_values_0_encoder_value);
        decoder2.SetInput("past_key_values.1.encoder.key", past_key_values_1_encoder_key);
        decoder2.SetInput("past_key_values.1.encoder.value", past_key_values_1_encoder_value);
        decoder2.SetInput("past_key_values.2.encoder.key", past_key_values_2_encoder_key);
        decoder2.SetInput("past_key_values.2.encoder.value", past_key_values_2_encoder_value);
        decoder2.SetInput("past_key_values.3.encoder.key", past_key_values_3_encoder_key);
        decoder2.SetInput("past_key_values.3.encoder.value", past_key_values_3_encoder_value);

        decoder2.Schedule();

        var logits = decoder2.PeekOutput("logits") as Tensor<float>;
        argmax.Schedule(logits);
        using var t_Token = await argmax.PeekOutput().ReadbackAndCloneAsync() as Tensor<int>;
        int index = t_Token[0];

        outputTokens[tokenCount] = lastToken[0];
        lastToken[0] = index;
        tokenCount++;
        tokensTensor.Reshape(new TensorShape(1, tokenCount));
        tokensTensor.dataOnBackend.Upload<int>(outputTokens, tokenCount);
        lastTokenTensor.dataOnBackend.Upload<int>(lastToken, 1);

        if (index == END_OF_TEXT)
        {
            transcribe = false;
        }
        else if (index < tokens.Length)
        {
            outputString += GetUnicodeText(tokens[index]);
        }

        Debug.Log(outputString);
    }

    // Tokenizer
    public TextAsset vocabAsset;
    void GetTokens()
    {
        var vocab = JsonConvert.DeserializeObject<Dictionary<string, int>>(vocabAsset.text);
        tokens = new string[vocab.Count];
        foreach (var item in vocab)
        {
            tokens[item.Value] = item.Key;
        }
    }

    string GetUnicodeText(string text)
    {
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text));
        return Encoding.UTF8.GetString(bytes);
    }

    string ShiftCharacterDown(string text)
    {
        string outText = "";
        foreach (char letter in text)
        {
            outText += ((int)letter <= 256) ? letter : (char)whiteSpaceCharacters[(int)(letter - 256)];
        }
        return outText;
    }

    void SetupWhiteSpaceShifts()
    {
        for (int i = 0, n = 0; i < 256; i++)
        {
            if (IsWhiteSpace((char)i)) whiteSpaceCharacters[n++] = i;
        }
    }

    bool IsWhiteSpace(char c)
    {
        return !(('!' <= c && c <= '~') || ('�' <= c && c <= '�') || ('�' <= c && c <= '�'));
    }

    private void OnDestroy()
    {
        if (decoder1 != null) decoder1.Dispose();
        if (decoder2 != null) decoder2.Dispose();
        if (encoder != null) encoder.Dispose();
        if (spectrogram != null) spectrogram.Dispose();
        if (argmax != null) argmax.Dispose();

        if (outputTokens.IsCreated) outputTokens.Dispose();
        if (lastToken.IsCreated) lastToken.Dispose();

        if (audioInput != null) audioInput.Dispose();
        if (lastTokenTensor != null) lastTokenTensor.Dispose();
        if (tokensTensor != null) tokensTensor.Dispose();
        if (encodedAudio != null) encodedAudio.Dispose();
    }
}
