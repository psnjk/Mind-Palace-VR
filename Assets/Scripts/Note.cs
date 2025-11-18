using UnityEngine;
using UnityEngine.UI;

public class Note : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find the Delete Button child and assign the delete function to it
        SetupDeleteButton();
   
        // Find the Pin Button child and assign the toggle function to it
        SetupPinButton();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// Deletes this note from the scene.
    /// </summary>
    public void DeleteNote()
    {
        Destroy(gameObject);
    }

    /// <summary>
    /// Toggle the LookAtCamera component
    /// </summary>
    public void toggleLookAtCamera()
    {
        LookAtCamera lookAtCameraComponent = GetComponent<LookAtCamera>();
        if (lookAtCameraComponent != null)
        {
            lookAtCameraComponent.enabled = !lookAtCameraComponent.enabled;
            Debug.Log($"LookAtCamera component on {gameObject.name} is now {(lookAtCameraComponent.enabled ? "enabled" : "disabled")}");
        }
        else
        {
            Debug.LogWarning($"No LookAtCamera component found on {gameObject.name}");
        }
    }

    /// <summary>
    /// Sets up the Delete Button as "Note Canvas"->"Background"->"Delete Button"
    /// </summary>
    private void SetupDeleteButton()
    {
        Transform noteCanvas = transform.Find("Note Canvas");
        if (noteCanvas == null)
        {
            Debug.LogWarning($"No child named 'Note Canvas' found on {gameObject.name}");
            return;
        }

        Transform background = noteCanvas.Find("Background");
        if (background == null)
        {
            Debug.LogWarning($"No child named 'Background' found under Note Canvas on {gameObject.name}");
            return;
        }

        Transform deleteButtonTransform = background.Find("Delete Button");
        if (deleteButtonTransform == null)
        {
            Debug.LogWarning($"No child named 'Delete Button' found under Background on {gameObject.name}");
            return;
        }

        Button deleteButton = deleteButtonTransform.GetComponent<Button>();
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(DeleteNote);
            Debug.Log($"Successfully connected Delete Button on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"Delete Button found on {gameObject.name}, but it doesn't have a Button component!");
        }
    }

    /// <summary>
    /// Sets up the Pin Button as "Note Canvas"->"Background"->"Pin Button"
    /// </summary>
    private void SetupPinButton()
    {
        Transform noteCanvas = transform.Find("Note Canvas");
        if (noteCanvas == null)
        {
            Debug.LogWarning($"No child named 'Note Canvas' found on {gameObject.name}");
            return;
        }

        Transform background = noteCanvas.Find("Background");
        if (background == null)
        {
            Debug.LogWarning($"No child named 'Background' found under Note Canvas on {gameObject.name}");
            return;
        }

        Transform pinButtonTransform = background.Find("Pin Button");
        if (pinButtonTransform == null)
        {
            Debug.LogWarning($"No child named 'Pin Button' found under Background on {gameObject.name}");
            return;
        }

        Button pinButton = pinButtonTransform.GetComponent<Button>();
        if (pinButton != null)
        {
            pinButton.onClick.RemoveAllListeners();
            pinButton.onClick.AddListener(toggleLookAtCamera);
            Debug.Log($"Successfully connected Pin Button on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"Pin Button found on {gameObject.name}, but it doesn't have a Button component!");
        }
    }
}
