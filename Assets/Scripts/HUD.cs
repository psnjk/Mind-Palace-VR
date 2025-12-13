using System;
using TMPro;
using UnityEngine;

public class HUD : MonoBehaviour
{
    public static HUD Instance;

    private GameObject microphoneIcon;
    private GameObject transcribeIcon;
    private GameObject messageText;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else 
            Destroy(gameObject);

        microphoneIcon = transform.Find("Microphone Icon").gameObject;
        transcribeIcon = transform.Find("Transcribe Icon").gameObject;
        messageText = transform.Find("Message").gameObject;
        

        if (!microphoneIcon)
        {
            Debug.LogWarning("[HUD] Microphone Icon not found.");
        }

        if (!transcribeIcon)
        {
            Debug.LogWarning("[HUD] Transcribe Icon not found.");
        }

        if (!messageText)
        {
            Debug.LogWarning("[HUD] Message not found.");
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

    /// <summary>
    /// Sets a message that will be shown to the user on the HUD
    /// </summary>
    public void SetMessageText(String message, bool status)
    {
        if (messageText)
        {
            messageText.GetComponent<TMP_Text>().text = message;
            messageText.SetActive(status);
        }
    }
}