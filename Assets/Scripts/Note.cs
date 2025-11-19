using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Note : MonoBehaviour
{
    private bool _lookAtCamera = true;
    private LookAtCamera _lookAtCameraComponent;
    private Rigidbody _rigidBody;
    private XRGrabInteractable _grabInteractable;

    private string _instanceID;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _instanceID = gameObject.GetInstanceID().ToString();
        _lookAtCameraComponent = GetComponent<LookAtCamera>();
        _rigidBody = GetComponent<Rigidbody>();
        _grabInteractable = GetComponent<XRGrabInteractable>();

        if (_lookAtCameraComponent == null)
        {
            Debug.LogWarning($"[Note] No LookAtCamera component found on {gameObject.name}");
        }
        else
        {
            _lookAtCameraComponent.enabled = _lookAtCamera;
        }

        if (_rigidBody == null)
        {
            Debug.LogWarning($"[Note] No RigidBody component found on {gameObject.name}");
        }

        if (_grabInteractable == null)
        {
            Debug.LogWarning($"[Note] No XRGrabInteractable component found on {gameObject.name}");
        }
        else
        {
            // Set up the grab interactable events
            _grabInteractable.selectEntered.AddListener(OnSelectEntered);
            _grabInteractable.selectExited.AddListener(OnSelectExited);
        }

        // Find the Delete Button child and assign the delete function to it
        SetupDeleteButton();

        // Find the Pin Button child and assign the toggle function to it
        SetupPinButton();
    }

    void OnDestroy()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            _grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        OnGrab();
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        OnRelease();
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Due to some XR Interaction Toolkit quirks, we need to have explicit onGrab and onRelease methods to handle some XR Grab Interactable and RigidBody parameters to make sure that the notes don't go through other objects when being grabbed but still look at the camera when released and not fly out in zero gravity.
    /// </summary>
    private void OnGrab()
    {
        Debug.Log($"[Note] Note was grabbed: {_instanceID}");

        _lookAtCameraComponent.enabled = false;

        _rigidBody.isKinematic = false;
        _rigidBody.interpolation = RigidbodyInterpolation.Interpolate;

        _grabInteractable.movementType = XRGrabInteractable.MovementType.VelocityTracking;
    }

    /// <summary>
    /// On release, we restore the note to how it was before.
    /// </summary>
    private void OnRelease()
    {
        Debug.Log($"[Note] Note was released: {_instanceID}");

        _lookAtCameraComponent.enabled = _lookAtCamera; // restore to what it was before grab

        _rigidBody.isKinematic = true;
        _rigidBody.interpolation = RigidbodyInterpolation.None;

        _grabInteractable.movementType = XRGrabInteractable.MovementType.Instantaneous;

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
    public void ToggleLookAtCamera()
    {
        if (_lookAtCameraComponent != null)
        {
            _lookAtCamera = !_lookAtCamera;
            _lookAtCameraComponent.enabled = _lookAtCamera;
            Debug.Log($"[Note] LookAtCamera component on {gameObject.name} is now {(_lookAtCameraComponent.enabled ? "enabled" : "disabled")}");
        }
        else
        {
            Debug.LogWarning($"[Note] No LookAtCamera component found on {gameObject.name}");
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
            Debug.LogWarning($"[Note] No child named 'Note Canvas' found on {gameObject.name}");
            return;
        }

        Transform background = noteCanvas.Find("Background");
        if (background == null)
        {
            Debug.LogWarning($"[Note] No child named 'Background' found under Note Canvas on {gameObject.name}");
            return;
        }

        Transform deleteButtonTransform = background.Find("Delete Button");
        if (deleteButtonTransform == null)
        {
            Debug.LogWarning($"[Note] No child named 'Delete Button' found under Background on {gameObject.name}");
            return;
        }

        Button deleteButton = deleteButtonTransform.GetComponent<Button>();
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(DeleteNote);
            Debug.Log($"[Note] Successfully connected Delete Button on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[Note] Delete Button found on {gameObject.name}, but it doesn't have a Button component!");
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
            Debug.LogWarning($"[Note] No child named 'Note Canvas' found on {gameObject.name}");
            return;
        }

        Transform background = noteCanvas.Find("Background");
        if (background == null)
        {
            Debug.LogWarning($"[Note] No child named 'Background' found under Note Canvas on {gameObject.name}");
            return;
        }

        Transform pinButtonTransform = background.Find("Pin Button");
        if (pinButtonTransform == null)
        {
            Debug.LogWarning($"[Note] No child named 'Pin Button' found under Background on {gameObject.name}");
            return;
        }

        Button pinButton = pinButtonTransform.GetComponent<Button>();
        if (pinButton != null)
        {
            pinButton.onClick.RemoveAllListeners();
            pinButton.onClick.AddListener(ToggleLookAtCamera);
            Debug.Log($"[Note] Successfully connected Pin Button on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[Note] Pin Button found on {gameObject.name}, but it doesn't have a Button component!");
        }
    }
}
