using TMPro;
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
    private NoteLinkable _noteLinkable;

    private string _instanceID;

    public bool IsPinned => _lookAtCamera;

    public TMP_InputField inputField;

    private void Awake()
    {
        // add listener for input field selection
        inputField.onSelect.AddListener(OnFieldSelect);
    }

    /// <summary>
    /// Used to notify the InputFieldFocusManager that this field has been selected to insert the result of the speech transcription on the field.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnFieldSelect(string _)
    {
        Debug.Log($"[Note] onFieldSelect ran for {_instanceID}");
        InputFieldFocusManager.Instance.SetFocusedField(inputField);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _instanceID = gameObject.GetInstanceID().ToString();
        _lookAtCameraComponent = GetComponent<LookAtCamera>();
        _rigidBody = GetComponent<Rigidbody>();
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _noteLinkable = GetComponent<NoteLinkable>();

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

        if (!_noteLinkable)
        {
            Debug.LogWarning($"[Note] No NoteLinkable component found on {gameObject.name}");
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

    [ContextMenu("Test Insert Text")]
    public void TestInsertText()
    {
        string textToInsert = "Hello Hello Man";
        if (inputField.isFocused)
        {
            int pos = inputField.caretPosition;
            string original = inputField.text;

            // Insert at caret
            inputField.text = original.Insert(pos, textToInsert);

            // Move caret to the end of inserted text
            inputField.caretPosition = pos + textToInsert.Length;
            inputField.selectionAnchorPosition = inputField.caretPosition;
            inputField.selectionFocusPosition = inputField.caretPosition;
        }
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

        // Deactivate any line colliders while moving the note to avoid interference
        NoteLinkManager.Instance.DeactivateLineCollidersForNote(_noteLinkable.NoteID);
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

        // Reactivate any line colliders after moving the note
        NoteLinkManager.Instance.RegenerateLineCollidersForNote(_noteLinkable.NoteID);
    }

    /// <summary>
    /// Deletes this note from the scene.
    /// </summary>
    public void DeleteNote()
    {
        Debug.Log($"[Note] DeleteNote called for {_instanceID}");

        // Get the NoteLinkable component if it exists
        NoteLinkable noteLinkable = GetComponent<NoteLinkable>();
        if (noteLinkable != null && NoteLinkManager.Instance != null)
        {
            Debug.Log($"[Note] Manually unregistering note {noteLinkable.NoteID} before deletion");

            // Cancel any active link operation if this note is involved
            noteLinkable.CancelLinkOperation();

            // Explicitly unregister the note to ensure immediate cleanup
            NoteLinkManager.Instance.UnregisterNote(noteLinkable);
        }

        // Destroy the GameObject (this will also trigger OnDestroy methods)
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
            //deleteButton.onClick.AddListener(DeleteNote);

            // Add ExpandingButton component if it doesn't exist
            ExpandingButton expandingButton = deleteButtonTransform.GetComponent<ExpandingButton>();
            if (expandingButton == null)
            {
                expandingButton = deleteButtonTransform.gameObject.AddComponent<ExpandingButton>();
            }

            LongPressButton longPressButton = deleteButtonTransform.GetComponent<LongPressButton>();
            if (longPressButton == null)
            {
                Debug.LogWarning($"[Note] No LongPressButton component found on Delete Button of {gameObject.name}");
            } else
            {
                longPressButton.OnDeletePressed += DeleteNote;
            }

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
            pinButton.onClick.AddListener(() => PlayerAudioManager.Instance?.Play());

            // Add ExpandingButton component if it doesn't exist
            ExpandingButton expandingButton = pinButtonTransform.GetComponent<ExpandingButton>();
            if (expandingButton == null)
            {
                expandingButton = pinButtonTransform.gameObject.AddComponent<ExpandingButton>();
            }

            Debug.Log($"[Note] Successfully connected Pin Button on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[Note] Pin Button found on {gameObject.name}, but it doesn't have a Button component!");
        }
    }
}
