using UnityEngine;
using UnityEngine.UI;

public class MicButtonAssignOnClick : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Verify this is the correct GameObject
        if (this.gameObject.name == "Mic")
        {
            AssignVoiceInputToThisButton();
        }
        else
        {
            Debug.LogWarning($"[MicButtonAssignOnClick] This script is intended for the 'Mic' GameObject, but it's attached to '{this.gameObject.name}'. Skipping assignment.");
        }
    }

    private void AssignVoiceInputToThisButton()
    {
        // Get the Button component on THIS specific GameObject
        Button button = this.GetComponent<Button>();
        
        if (button == null)
        {
            Debug.LogError($"[MicButtonAssignOnClick] No Button component found on {this.gameObject.name}!");
            return;
        }

        // Verify this GameObject has the XR Keyboard Key component
        Component xrKeyboardKey = this.GetComponent("XR Keyboard Key");
        if (xrKeyboardKey == null)
        {
            Debug.LogWarning($"[MicButtonAssignOnClick] No 'XR Keyboard Key' component found on {this.gameObject.name}. This script is intended for XR keyboard keys.");
        }

        // Find the VoiceInput component in the scene
        VoiceInput voiceInput = FindObjectOfType<VoiceInput>();
 
        if (voiceInput == null)
        {
            Debug.LogError("[MicButtonAssignOnClick] No VoiceInput component found in the scene!");
            return;
        }

        // Add the VoiceInput method to the button's OnClick event
        button.onClick.AddListener(voiceInput.OnVoiceInputButtonClicked);
     
        Debug.Log($"[MicButtonAssignOnClick] Successfully assigned OnVoiceInputButtonClicked to the 'Mic' button's OnClick event");
    }

    // Update is called once per frame
    void Update()
    {
 
    }
}
