using UnityEngine;
using UnityEngine.UI;

// TODO: add functionality to settings canvas like toggling the Vignette and changing locomotion settings
public class SettingsCanvas : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("[SettingsCanvas] UIManager not found!");
            }
            else
            {
                Debug.Log("[SettingsCanvas] UIManager found automatically");

            }
        }
        SetupBackButton();
        
    }

    void SetupBackButton()
    {

        Transform background = transform.Find("Background");
        if (background == null)
        {
            Debug.LogWarning($"No child named 'Background' found under Hand Canvas on {gameObject.name}");
            return;
        }

        Transform backButtonTransform = background.Find("Back Button");
        if (backButtonTransform == null)
        {
            Debug.LogWarning($"No child named 'Back Button' found under Background on {gameObject.name}");
            return;
        }

        Button backButton = backButtonTransform.GetComponent<Button>();
        if (backButton != null)
        {
            backButton.onClick.AddListener(() => {
                if (uiManager != null)
                {
                    uiManager.HideCanvas("Settings");
                    uiManager.ShowCanvas("Menu");
                }
            });
        }
        else
        {
            Debug.LogWarning($"'Back Button' found on {gameObject.name}, but it doesn't have a Button component!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
