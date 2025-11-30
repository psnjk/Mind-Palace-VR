using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class DebugDisplay : MonoBehaviour
{
    private List<string> logEntries = new List<string>();
    public TMP_Text display;
    public ScrollRect scrollRect;
    public int maxLogEntries = 100;

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string formattedLog = $"[{timestamp}] [{type}] {logString}";
        
        logEntries.Add(formattedLog);
        
        if (logEntries.Count > maxLogEntries)
        {
            logEntries.RemoveAt(0);
        }
        
        // Update the display
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (display != null)
        {
            display.text = string.Join("\n", logEntries);
            
            // auto scroll to bottom
            StartCoroutine(ScrollToBottom());
        }
    }

    private System.Collections.IEnumerator ScrollToBottom()
    {
        // Wait for end of frame to ensure layout is updated
        yield return new WaitForEndOfFrame();
        
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
        else
        {
            // If no ScrollRect is assigned, try to find one in the parent hierarchy
            ScrollRect foundScrollRect = GetComponentInParent<ScrollRect>();
            if (foundScrollRect != null)
            {
                foundScrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize with a welcome message
        if (display != null)
        {
            display.text = "[Debug Display Initialized]";
        }
    }

}
