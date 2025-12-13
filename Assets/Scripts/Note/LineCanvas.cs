using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LineCanvas : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Auto Hide Settings")]
    [Tooltip("Time in seconds before canvas hides when not hovered")]
    public float hideDelay = 1f;

    private float _timer;
    private bool _isHovered = false;
    private GraphicRaycaster _graphicRaycaster;

    // Reference to the NoteLink that this canvas represents
    private NoteLink _associatedLink;

    void Start()
    {
        // Ensure we have a GraphicRaycaster for UI interactions
        _graphicRaycaster = GetComponent<GraphicRaycaster>();
        if (_graphicRaycaster == null)
        {
            _graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
        }

        // Set up the delete button
        SetupDeleteButton();

        // Start the timer when canvas becomes active
        ResetTimer();

        Debug.Log($"[LineCanvas] Auto-hide initialized with {hideDelay}s delay");
    }

    /// <summary>
    /// Sets the NoteLink that this canvas represents. This should be called by the LineController when the canvas is created.
    /// </summary>
    /// <param name="noteLink">The NoteLink associated with this canvas</param>
    public void SetAssociatedLink(NoteLink noteLink)
    {
        _associatedLink = noteLink;
        Debug.Log($"[LineCanvas] Associated link set: {noteLink}");
    }

    private void SetupDeleteButton()
    {
        Transform deleteButtonTransform = transform.Find("Delete Button");

        if (deleteButtonTransform == null)
        {
            Debug.LogWarning($"[LineCanvas] Delete Button not found in children of {gameObject.name}");
            return;
        }

        Button deleteButton = deleteButtonTransform.GetComponent<Button>();
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(DeleteLink);

            Debug.Log($"[LineCanvas] Delete button set up successfully with hover effects");
        }
        else
        {
            Debug.LogWarning($"[LineCanvas] Button component not found on Delete Button");
        }
    }

    private void DeleteLink()
    {
        Debug.Log($"[LineCanvas] Delete button clicked - deleting link: {_associatedLink}");

        if (!_associatedLink.IsValid())
        {
            Debug.LogWarning($"[LineCanvas] Cannot delete link - associated link is not valid: {_associatedLink}");
            return;
        }

        if (NoteLinkManager.Instance == null)
        {
            Debug.LogError($"[LineCanvas] Cannot delete link - NoteLinkManager.Instance is null");
            return;
        }

        // Call the RemoveLink method on the NoteLinkManager
        NoteLinkManager.Instance.RemoveLink(_associatedLink);

        // Hide the canvas since the link (and this canvas) will be destroyed
        HideCanvas();
    }

    void Update()
    {
        // Only count down timer if not being hovered
        if (!_isHovered)
        {
            _timer -= Time.deltaTime;

            if (_timer <= 0f)
            {
                HideCanvas();
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        Debug.Log($"[LineCanvas] Pointer entered canvas - stopping timer");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        ResetTimer();
        Debug.Log($"[LineCanvas] Pointer exited canvas - restarting timer");
    }

    void OnEnable()
    {
        // Reset timer when canvas becomes active
        ResetTimer();
        _isHovered = false;
        Debug.Log($"[LineCanvas] Canvas enabled - timer reset");
    }

    private void ResetTimer()
    {
        _timer = hideDelay;
    }

    private void HideCanvas()
    {
        Debug.Log($"[LineCanvas] Timer expired - hiding canvas");
        gameObject.SetActive(false);
    }

    // Public method to manually reset the timer (useful if called from other scripts)
    public void ResetAutoHideTimer()
    {
        ResetTimer();
        Debug.Log($"[LineCanvas] Timer manually reset");
    }
}