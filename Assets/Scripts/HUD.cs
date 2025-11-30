using UnityEngine;

public class HUD : MonoBehaviour
{
    public static HUD Instance;

    private GameObject microphoneIcon;
    private GameObject transcribeIcon;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else 
            Destroy(gameObject);

        microphoneIcon = transform.Find("Microphone Icon").gameObject;
        transcribeIcon = transform.Find("Transcribe Icon").gameObject;

        if (!microphoneIcon)
        {
            Debug.LogWarning("[HUD] Microphone Icon not found.");
        }

        if (!transcribeIcon)
        {
            Debug.LogWarning("[HUD] Transcribe Icon not found.");
        }
    }

    /// <summary>
    /// Sets the microphone icon on the HUD as active or inactive.
    /// </summary>
    /// <param name="status"></param>
    public void SetMicIcon(bool status)
    {
        if (microphoneIcon) microphoneIcon.SetActive(status);
    }

    /// <summary>
    /// Sets the transcribe icon on the HUD as active or inactive.
    /// </summary>
    /// <param name="status"></param>
    public void SetTranscribeIcon(bool status)
    {
        if (transcribeIcon) transcribeIcon.SetActive(status);
    }
}
