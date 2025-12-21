using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public enum AttachPoint
{
    None = -1,
    Top = 0,
    TopRight = 1,
    Right = 2,
    BottomRight = 3,
    Bottom = 4,
    BottomLeft = 5,
    Left = 6,
    TopLeft = 7
}

public class NoteLinkable : MonoBehaviour
{
    public string NoteID { get; set; }

    private GameObject _handle;
    private GameObject _attachPointsCanvas;
    private GameObject _linkButton;

    private bool _isLocalLinkMode = false;

    // Dictionary to store attach point buttons
    private Dictionary<AttachPoint, Button> _attachPointButtons = new Dictionary<AttachPoint, Button>();

    // Dictionary to store the physical attach point transforms with which the lines will be rendered
    private Dictionary<AttachPoint, Transform> _attachPointTransforms = new Dictionary<AttachPoint, Transform>();

    private void Awake()
    {
        //NoteID = System.Guid.NewGuid().ToString();
        //Debug.Log($"[NoteLinkable] NoteID: {NoteID}");

        // Register with the manager
        //if (NoteLinkManager.Instance)
        //{
        //    NoteLinkManager.Instance.RegisterNote(this);
        //}
    }

    private void OnDestroy()
    {
        // Unregister from the manager when this note is destroyed
        if (NoteLinkManager.Instance)
        {
            NoteLinkManager.Instance.UnregisterNote(this);
        }
    }

    private void Start()
    {
        // Setup the handle
        GetHandle();

        // Setup the attach points canvas
        GetAttachPointsCanvas();
        _attachPointsCanvas?.gameObject.SetActive(false);

        // Setup the link button
        SetupLinkButton();

        // Setup the attach points buttons
        SetupAttachPointButtons();

        // Setup the attach points transforms
        SetupAttachPointTransforms();
    }

    private void GetHandle()
    {
        Transform handleTransform = transform.Find("Handle");
        if (handleTransform)
        {
            _handle = handleTransform.gameObject;
            Debug.Log($"[NoteLinkable] Handle child found for {NoteID}");
        }
        else
        {
            Debug.LogWarning($"[NoteLinkable] Handle not found for {NoteID}");
        }
    }

    private void GetAttachPointsCanvas()
    {
        Transform canvasTransform = transform.Find("Attach Points Canvas");
        if (canvasTransform)
        {
            _attachPointsCanvas = canvasTransform.gameObject;
            Debug.Log($"[NoteLinkable] AttachPointsCanvas child found for {NoteID}");
        }
        else
        {
            Debug.LogWarning($"[NoteLinkable] AttachPointsCanvas not found for {NoteID}");
        }
    }

    /// <summary>
    /// Local Link Mode determines if the note will show attach points for linking and hide the handle when the link button is clicked.
    /// </summary>
    private void ToggleLocalLinkMode()
    {
        _isLocalLinkMode = !_isLocalLinkMode;
        _handle?.SetActive(!_isLocalLinkMode);
        //_attachPointsCanvas?.SetActive(!_isLocalLinkMode);
        if (!_isLocalLinkMode)
        {
            _attachPointsCanvas?.GetComponent<UIScaleOut>()?.Hide();
        }
        else
        {
            ShowAllAttachPointButtons();
            _attachPointsCanvas?.GetComponent<UIScaleOut>()?.Show();
        }
    }

    public void SetLocalLinkMode(bool active)
    {
        Debug.Log($"[NoteLinkable] SetLocalLinkMode called for {NoteID}, setting to: {active}");
        Debug.Log($"[NoteLinkable] Handle exists: {_handle != null}, AttachPointsCanvas exists: {_attachPointsCanvas != null}");

        _isLocalLinkMode = active;
        _handle?.SetActive(!_isLocalLinkMode);
        //_attachPointsCanvas?.SetActive(_isLocalLinkMode);
        if (!_isLocalLinkMode)
        {
            _attachPointsCanvas?.GetComponent<UIScaleOut>()?.Hide();
        }
        else
        {
            ShowAllAttachPointButtons();
            _attachPointsCanvas?.GetComponent<UIScaleOut>()?.Show();
        }

        Debug.Log($"[NoteLinkable] After SetLocalLinkMode - Handle active: {_handle?.activeInHierarchy}, AttachPointsCanvas active: {_attachPointsCanvas?.activeInHierarchy}");
    }

    /// <summary>
    /// Sets up the Link Button as "Note Canvas"->"Background"->"Link Button"
    /// </summary>
    private void SetupLinkButton()
    {
        Transform noteCanvas = transform.Find("Note Canvas");
        if (noteCanvas == null)
        {
            Debug.LogWarning($"[NoteLinkable] No child named 'Note Canvas' found on {gameObject.name}");
            return;
        }

        Transform background = noteCanvas.Find("Background");
        if (background == null)
        {
            Debug.LogWarning(
               $"[NoteLinkable] No child named 'Background' found under Note Canvas on {gameObject.name}");
            return;
        }

        Transform linkButtonTransform = background.Find("Link Button");
        if (linkButtonTransform == null)
        {
            Debug.LogWarning(
                  $"[NoteLinkable] No child named 'Link Button' found under Background on {gameObject.name}");
            return;
        }

        _linkButton = linkButtonTransform.gameObject;

        Button linkButton = linkButtonTransform.GetComponent<Button>();
        if (linkButton != null)
        {
            linkButton.onClick.RemoveAllListeners();
            linkButton.onClick.AddListener(ToggleLocalLinkMode);

            // Add ExpandingButton component if it doesn't exist
            ExpandingButton expandingButton = linkButtonTransform.GetComponent<ExpandingButton>();
            if (expandingButton == null)
            {
                expandingButton = linkButtonTransform.gameObject.AddComponent<ExpandingButton>();
            }

            Debug.Log($"[NoteLinkable] Successfully connected Link Button on {gameObject.name}");
        }
        else
        {
            Debug.LogWarning(
              $"[NoteLinkable] Link Button found on {gameObject.name}, but it doesn't have a Button component!");
        }
    }

    /// <summary>
    /// Sets up all attach point buttons in the AttachPointsCanvas
    /// </summary>
    private void SetupAttachPointButtons()
    {
        if (_attachPointsCanvas == null)
        {
            Debug.LogWarning($"[NoteLinkable] AttachPointsCanvas not found, cannot setup attach point buttons for {NoteID}");
            return;
        }

        // Find and setup each attach point button
        foreach (AttachPoint attachPoint in System.Enum.GetValues(typeof(AttachPoint)))
        {
            if (attachPoint == AttachPoint.None) continue; // Skip None value

            string buttonName = attachPoint.ToString();
            Transform buttonTransform = _attachPointsCanvas.transform.Find(buttonName);

            if (buttonTransform != null)
            {
                Button button = buttonTransform.GetComponent<Button>();
                if (button != null)
                {
                    // Store reference to button
                    _attachPointButtons[attachPoint] = button;

                    // Setup button click event
                    AttachPoint currentAttachPoint = attachPoint; // Capture for closure
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnAttachPointButtonClicked(currentAttachPoint));

                    // Add ExpandingButton component if it doesn't exist
                    ExpandingButton expandingButton = buttonTransform.GetComponent<ExpandingButton>();
                    if (expandingButton == null)
                    {
                        expandingButton = buttonTransform.gameObject.AddComponent<ExpandingButton>();
                    }

                    Debug.Log($"[NoteLinkable] Successfully setup {buttonName} button for {NoteID}");
                }
                else
                {
                    Debug.LogWarning($"[NoteLinkable] {buttonName} found but missing Button component for {NoteID}");
                }
            }
            else
            {
                Debug.LogWarning($"[NoteLinkable] {buttonName} button not found in AttachPointsCanvas for {NoteID}");
            }
        }
    }

    /// <summary>
    /// Called when an attach point button is clicked
    /// </summary>
    /// <param name="attachPoint">The attach point that was clicked</param>
    private void OnAttachPointButtonClicked(AttachPoint attachPoint)
    {
        Debug.Log($"[NoteLinkable] Attach point {attachPoint} clicked on note {NoteID}");

        if (NoteLinkManager.Instance)
        {
            if (NoteLinkManager.Instance.isGlobalLinkMode) // if the note is the end of a link
            {
                NoteLinkManager.Instance.EndLink(this, attachPoint);
            }
            else // if the link was started on the note
            {
                NoteLinkManager.Instance.StartLink(this, attachPoint);
                HideAllAttachPointButtonsExcept(attachPoint);
            }
        }
        else
        {
            Debug.LogError($"[NoteLinkable] NoteLinkManager.Instance is null when trying to start link for {NoteID}");
        }
    }

    /// <summary>
    /// Cancels any active link operation involving this note
    /// </summary>
    public void CancelLinkOperation()
    {
        if (NoteLinkManager.Instance && NoteLinkManager.Instance.isGlobalLinkMode)
        {
            Debug.Log($"[NoteLinkable] Manually canceling link operation for note {NoteID}");
            NoteLinkManager.Instance.CancelActiveLink();
        }
    }

    /// <summary>
    /// Sets up all attach point transforms from the "Attach Points Transforms" child object
    /// </summary>
    private void SetupAttachPointTransforms()
    {
        Transform attachPointsTransforms = transform.Find("Attach Points Transforms");
        if (attachPointsTransforms == null)
        {
            Debug.LogWarning($"[NoteLinkable] 'Attach Points Transforms' child not found for {NoteID}");
            return;
        }

        // Find and setup each attach point transform
        foreach (AttachPoint attachPoint in System.Enum.GetValues(typeof(AttachPoint)))
        {
            if (attachPoint == AttachPoint.None) continue; // Skip None value

            string transformName = attachPoint.ToString();
            Transform attachPointTransform = attachPointsTransforms.Find(transformName);

            if (attachPointTransform != null)
            {
                _attachPointTransforms[attachPoint] = attachPointTransform;
                Debug.Log($"[NoteLinkable] Successfully setup {transformName} transform for {NoteID}");
            }
            else
            {
                Debug.LogWarning($"[NoteLinkable] {transformName} transform not found under 'Attach Points Transforms' for {NoteID}");
            }
        }
    }

    /// <summary>
    /// Gets the transform for a specific attach point
    /// </summary>
    /// <param name="attachPoint">The attach point to get the transform for</param>
    /// <returns>The transform for the attach point, or null if not found</returns>
    public Transform GetAttachPointTransform(AttachPoint attachPoint)
    {
        _attachPointTransforms.TryGetValue(attachPoint, out Transform transform);
        return transform;
    }

    /// <summary>
    /// Deactivates all attach point buttons except the specified one, used for start link to indicate selection
    /// </summary>
    /// <param name="attachPoint"></param>
    private void HideAllAttachPointButtonsExcept(AttachPoint attachPoint)
    {
        foreach (var buttonPair in _attachPointButtons)
        {
            if (buttonPair.Key != attachPoint)
            {
                buttonPair.Value.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Shows all attach point buttons, used to reset the UI
    /// </summary>
    public void ShowAllAttachPointButtons()
    {
        foreach (var buttonPair in _attachPointButtons)
        {
            buttonPair.Value.gameObject.SetActive(true);
        }
    }
}
