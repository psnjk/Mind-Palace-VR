using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

// Enum to specify the location of the Save Control Panel
public enum SaveControlPanelLocation
{
    Hub,
    Room,
}

public class SaveControlPanel : MonoBehaviour
{

    public SaveControlPanelLocation location;
    public String mainHubSceneName = "MainHub";
    private string currentSaveId;

    public string providedSaveId;

    private TMP_InputField saveNameInputField;

    private Button saveNameButton;
    private Button deleteButton;

    public Portal exitPortal;

    [SerializeField] private float fadeDuration = 0.5f;



    void Start()
    {
        SetupSaveNameInputField();
        SetupSaveNameButton();
        SetupDeleteButton();
    }

    void SetupSaveNameInputField()
    {
        // fetch the input field component
        try
        {
            saveNameInputField = transform.Find("Save Control Screen").Find("Background").Find("Save Name Input Field").GetComponent<TMP_InputField>(); 
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SaveControlPanel] Error setting up save name input field: " + ex.Message);
            return;
        }

        if (location == SaveControlPanelLocation.Hub)
        {
            // Setup input field for hub location
            // ID will be provided externally
            saveNameInputField.text = SaveManager.Instance.GetSaveName(providedSaveId);
            if (saveNameInputField.text == null)
            {
                Debug.LogError("[SaveControlPanel] Could not retrieve save name for provided save ID: " + providedSaveId);
            }

        }
        else if (location == SaveControlPanelLocation.Room)
        {
            currentSaveId = SaveManager.Instance.GetCurrentSaveId();
            saveNameInputField.text = SaveManager.Instance.GetSaveName(currentSaveId);
            if (saveNameInputField.text == null)
            {
                Debug.LogError("[SaveControlPanel] Could not retrieve save name for current save ID: " + currentSaveId);
            }
        }
    }

    void SetupSaveNameButton()
    {
        try
        {
            saveNameButton = transform.Find("Save Control Screen").Find("Background").Find("Save Name Button").GetComponent<Button>();
        } 
        catch (System.Exception ex)
        {
            Debug.LogError("[SaveControlPanel] Error setting up save name button: " + ex.Message);            
            return;
        } 

        saveNameButton.onClick.AddListener(() =>
        {
            PlayerAudioManager.Instance?.Play();
            string newSaveName = saveNameInputField.text;
            // edge case when then user creates a new room and no save exists yet, then create then the name must be set when exiting the portal
            if (location == SaveControlPanelLocation.Room && string.IsNullOrEmpty(currentSaveId))
            {
                exitPortal.initialSaveName = newSaveName;
                StartCoroutine(FlashButtonGreen());
                return;
            }

            string targetSaveId = location == SaveControlPanelLocation.Hub ? providedSaveId : currentSaveId;
            bool success = SaveManager.Instance.UpdateSaveName(targetSaveId, newSaveName); 
            if (!success)
            {
                Debug.LogError($"[SaveControlPanel] Failed to update save name for Save ID {targetSaveId} to '{newSaveName}'");
                return;
            }
            Debug.Log($"[SaveControlPanel] Updated save name for Save ID {targetSaveId} to '{newSaveName}'");
            StartCoroutine(FlashButtonGreen());
        });
    }

    private System.Collections.IEnumerator FlashButtonGreen()
    {
        Image buttonImage = saveNameButton.GetComponent<Image>();
        if (buttonImage == null)
        {
            Debug.LogWarning("[SaveControlPanel] Button has no Image component to flash");
            yield break;
        }

        Color originalColor = buttonImage.color;
        buttonImage.color = Color.green;
        
        yield return new WaitForSeconds(1f);
        
        buttonImage.color = originalColor;
    }

    void SetupDeleteButton()
    {
        try {
            deleteButton = transform.Find("Save Control Screen").Find("Background").Find("Delete Button").GetComponent<Button>();
            LongPressButton longPress = deleteButton.gameObject.GetComponent<LongPressButton>();
            longPress.OnDeletePressed += () =>
            {
                string targetSaveId = location == SaveControlPanelLocation.Hub ? providedSaveId : currentSaveId;
                SaveManager.Instance.DeleteSave(targetSaveId);
                // Teleport user to hub
                StartCoroutine(FadeAndLoadDefault(mainHubSceneName));
            };
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SaveControlPanel] Error setting up delete button: " + ex.Message);            
            return;
        }
    }

    private IEnumerator FadeAndLoadDefault(string sceneName)
    {
        if (FadeController.Instance != null)
        {
            yield return StartCoroutine(FadeController.Instance.FadeOut(fadeDuration));
        }
        else
        {
            yield return new WaitForSeconds(fadeDuration);
        }
        
        SaveManager.Instance.LoadScene(sceneName);
    }

    void Update()
    {
        
    }
}
