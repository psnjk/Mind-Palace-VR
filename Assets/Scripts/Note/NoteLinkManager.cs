using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NoteLinkManager : MonoBehaviour
{
    public static NoteLinkManager Instance;

    private Dictionary<string, NoteLinkable> notes = new();

    private List<NoteLink> links = new();

    // Dictionary to track visual line GameObjects for each NoteLink
    private Dictionary<NoteLink, GameObject> linkVisuals = new();

    public bool isGlobalLinkMode = false;

    public GameObject linePrefab;

    [Tooltip("Parent object for spawned lines (for organization)")]
    public Transform linesParent;

    private NoteLinkable _tempStartNote;
    private AttachPoint _tempStartAttachPoint;

    private void Awake()
    {
        // Handle singleton pattern with proper cleanup
        if (Instance != null && Instance != this)
        {
            Debug.Log("[NoteLinkManager] Destroying duplicate NoteLinkManager instance");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Clear any existing notes when a new instance is created
        notes.Clear();
        links.Clear();
        linkVisuals.Clear();
        isGlobalLinkMode = false;
        _tempStartNote = null;
        _tempStartAttachPoint = AttachPoint.None;

        Debug.Log("[NoteLinkManager] NoteLinkManager instance initialized and cleared");
    }

    private void Start()
    {
        if (!linesParent)
        {
            GameObject parentObj = new GameObject("Spawned Lines");
            linesParent = parentObj.transform;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("[NoteLinkManager] NoteLinkManager instance destroyed and cleared");
        }
    }

    public void RegisterNote(NoteLinkable note)
    {
        if (!notes.ContainsKey(note.NoteID))
        {
            notes.Add(note.NoteID, note);
            Debug.Log($"[NoteLinkManager] registered note with ID: {note.NoteID}, GameObject name: {note.gameObject.name}");
            Debug.Log($"[NoteLinkManager] Total notes now: {notes.Count}");
        }
        else
        {
            Debug.LogWarning($"[NoteLinkManager] Attempted to register duplicate note ID: {note.NoteID}, GameObject name: {note.gameObject.name}");
        }
    }

    /// <summary>
    /// Unregisters a note from the manager and handles cleanup if it was involved in an active link
    /// </summary>
    public void UnregisterNote(NoteLinkable note)
    {
        if (notes.ContainsKey(note.NoteID))
        {
            // Log how many links will be affected
            int linkCount = GetLinkCountForNote(note.NoteID);
            if (linkCount > 0)
            {
                Debug.Log($"[NoteLinkManager] Note {note.NoteID} has {linkCount} associated links that will be removed");
            }

            notes.Remove(note.NoteID);
            Debug.Log($"[NoteLinkManager] Unregistered note with ID: {note.NoteID}");

            // Check if this note was the start of an active link
            if (_tempStartNote == note)
            {
                Debug.LogWarning($"[NoteLinkManager] Start note {note.NoteID} was deleted during linking. Canceling link operation.");
                CancelActiveLink();
            }

            // Remove any existing links involving this note
            RemoveLinksForNote(note.NoteID);
        }
        else
        {
            Debug.LogWarning($"[NoteLinkManager] Attempted to unregister note {note.NoteID} that was not registered");
        }
    }

    /// <summary>
    /// Cancels the current active link operation and resets the manager state
    /// </summary>
    public void CancelActiveLink()
    {
        Debug.Log("[NoteLinkManager] Canceling active link operation");

        _tempStartNote = null;
        _tempStartAttachPoint = AttachPoint.None;
        isGlobalLinkMode = false;

        // Disable local link mode on all notes
        DisableLocalLinkModeAll();
        
        HUD.Instance.SetMessageText("",false);
    }

    /// <summary>
    /// Validates that the temporary start note still exists and is valid
    /// </summary>
    private bool ValidateStartNote()
    {
        if (_tempStartNote == null)
        {
            if (isGlobalLinkMode)
            {
                Debug.LogWarning("[NoteLinkManager] Start note is null but still in global link mode. Cleaning up.");
                CancelActiveLink();
            }
            return false;
        }

        // Check if the GameObject has been destroyed
        if (_tempStartNote.gameObject == null)
        {
            Debug.LogWarning("[NoteLinkManager] Start note GameObject has been destroyed. Cleaning up.");
            CancelActiveLink();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Removes all links that involve a specific note
    /// </summary>
    private void RemoveLinksForNote(string noteId)
    {
        var linksToRemove = links.Where(link => link.InvolvestNote(noteId)).ToList();

        foreach (var linkToRemove in linksToRemove)
        {
            // Destroy the visual line GameObject if it exists
            if (linkVisuals.TryGetValue(linkToRemove, out GameObject lineObject))
            {
                if (lineObject != null)
                {
                    Debug.Log($"[NoteLinkManager] Destroying visual line for deleted link: {linkToRemove}");
                    Destroy(lineObject);
                }
                linkVisuals.Remove(linkToRemove);
            }

            links.Remove(linkToRemove);
            Debug.Log($"[NoteLinkManager] Removed link involving deleted note {noteId}: {linkToRemove}");
        }
    }

    public void StartLink(NoteLinkable startNote, AttachPoint startAttachPoint)
    {
        Debug.Log($"[NoteLinkManager] started link on {startNote.NoteID} on attach point {startAttachPoint}");

        isGlobalLinkMode = true;

        // set the temporary variables to later conclude the link
        _tempStartNote = startNote;
        _tempStartAttachPoint = startAttachPoint;

        // Put all other notes in the scene except the startNote into local link mode
        LocalLinkModeAllExcept(startNote);

        HUD.Instance.SetMessageText("Please select which note to link to.\nPress B to cancel linking.", true);
    }

    public void EndLink(NoteLinkable endNote, AttachPoint endAttachPoint)
    {
        // Validate that we still have a valid start note
        if (!ValidateStartNote())
        {
            Debug.Log("[NoteLinkManager] Cannot end link - start note probably got deleted");
            return;
        }

        if (_tempStartNote.NoteID == endNote.NoteID)
        {
            Debug.LogWarning($"[NoteLinkManager] Cannot link note {_tempStartNote.NoteID} to itself. Link aborted.");
            return;
        }

        Debug.Log($"[NoteLinkManager] ended link on {endNote.NoteID} on attach point {endAttachPoint}");

        var newLink = new NoteLink(_tempStartNote.NoteID, _tempStartAttachPoint, endNote.NoteID, endAttachPoint);

        // Check if this link already exists
        if (links.Any(existingLink =>
            existingLink.sourceNoteId == newLink.sourceNoteId &&
            existingLink.sourceAttachPoint == newLink.sourceAttachPoint &&
            existingLink.targetNoteId == newLink.targetNoteId &&
            existingLink.targetAttachPoint == newLink.targetAttachPoint))
        {
            Debug.LogWarning($"[NoteLinkManager] Link already exists: {newLink}. Link creation aborted.");

            // Clean up the link mode state
            _tempStartNote = null;
            _tempStartAttachPoint = AttachPoint.None;
            isGlobalLinkMode = false;
            DisableLocalLinkModeAll();
            HUD.Instance.SetMessageText("",false);
            return;
        }

        links.Add(newLink);

        _tempStartNote = null;
        _tempStartAttachPoint = AttachPoint.None;
        isGlobalLinkMode = false;

        // Disable local link mode on all notes
        DisableLocalLinkModeAll();

        // print all links for debugging
        PrintAllLinks();

        // create the visual link between the two notes
        CreateLine(newLink);

        HUD.Instance.SetMessageText("",false);
    }

    private void LocalLinkModeAllExcept(NoteLinkable note)
    {
        Debug.Log($"[NoteLinkManager] LocalLinkModeAllExcept called, excluding note: {note.NoteID}");

        // Clean up any destroyed notes first
        CleanupDestroyedNotes();

        Debug.Log($"[NoteLinkManager] Total registered notes after cleanup: {notes.Count}");

        // List all notes for debugging
        ListAllRegisteredNotes();

        foreach (var registeredNote in notes.Values)
        {
            if (registeredNote != note)
            {
                Debug.Log($"[NoteLinkManager] Setting local link mode to true for note: {registeredNote.NoteID}");
                registeredNote.SetLocalLinkMode(true);
            }
            else
            {
                Debug.Log($"[NoteLinkManager] Skipping note (it's the start note): {registeredNote.NoteID}");
            }
        }
    }

    private void DisableLocalLinkModeAll()
    {
        Debug.Log("[NoteLinkManager] Disabling local link mode on all notes");

        // Clean up any destroyed notes first
        CleanupDestroyedNotes();

        Debug.Log($"[NoteLinkManager] Total registered notes after cleanup: {notes.Count}");

        // List all notes for debugging
        ListAllRegisteredNotes();
        foreach (var registeredNote in notes.Values)
        {
            Debug.Log($"[NoteLinkManager] Disabling local link mode for note: {registeredNote.NoteID}");
            registeredNote.SetLocalLinkMode(false);
        }
    }

    // Clean up destroyed notes from the dictionary
    private void CleanupDestroyedNotes()
    {
        // Clean up destroyed visual lines first
        CleanupDestroyedVisualLines();

        var keysToRemove = new List<string>();

        foreach (var kvp in notes)
        {
            if (kvp.Value == null)
            {
                keysToRemove.Add(kvp.Key);
                Debug.Log($"[NoteLinkManager] Found destroyed note with ID: {kvp.Key}");
            }
        }

        foreach (var key in keysToRemove)
        {
            notes.Remove(key);
            Debug.Log($"[NoteLinkManager] Removed destroyed note with ID: {key}");
        }

        if (keysToRemove.Count > 0)
        {
            Debug.Log($"[NoteLinkManager] Cleaned up {keysToRemove.Count} destroyed notes");
        }
    }

    // Get all links for a specific note (both outgoing and incoming)
    public List<(AttachPoint attachPoint, string linkedNoteId, AttachPoint linkedAttachPoint)> GetAllLinksForNote(string noteId)
    {
        var outgoingLinks = links
            .Where(link => link.sourceNoteId == noteId)
    .Select(link => (link.sourceAttachPoint, link.targetNoteId, link.targetAttachPoint));

        var incomingLinks = links
                .Where(link => link.targetNoteId == noteId)
              .Select(link => (AttachPoint.None, link.sourceNoteId, link.sourceAttachPoint)); // Note: AttachPoint.None indicates this is an incoming link

        return outgoingLinks.Concat(incomingLinks).ToList();
    }

    /// <summary>
    /// Gets the count of all links involving a specific note
    /// </summary>
    public int GetLinkCountForNote(string noteId)
    {
        return links.Count(link => link.InvolvestNote(noteId));
    }

    // Method to list all registered notes for debugging
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ListAllRegisteredNotes()
    {
        Debug.Log($"[NoteLinkManager] === All Registered Notes ({notes.Count}) ===");
        foreach (var kvp in notes)
        {
            var note = kvp.Value;
            if (note != null)
            {
                Debug.Log($"[NoteLinkManager] ID: {kvp.Key}, GameObject: {note.gameObject.name}, Scene: {note.gameObject.scene.name}");
            }
            else
            {
                Debug.Log($"[NoteLinkManager] ID: {kvp.Key}, GameObject: NULL (destroyed)");
            }
        }
        Debug.Log("[NoteLinkManager] === End of List ===");
    }

    // Method to print all links for debugging
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void PrintAllLinks()
    {
        Debug.Log($"[NoteLinkManager] === All Links ({links.Count}) ===");

        if (links.Count == 0)
        {
            Debug.Log("[NoteLinkManager] No links found");
        }
        else
        {
            for (int i = 0; i < links.Count; i++)
            {
                var link = links[i];
                Debug.Log($"[NoteLinkManager] Link {i}: {link}");
            }
        }

        Debug.Log("[NoteLinkManager] === End of Links ===");
    }

    /// <summary>
    /// Cleans up any destroyed visual line GameObjects from the linkVisuals dictionary
    /// </summary>
    private void CleanupDestroyedVisualLines()
    {
        var linksToRemove = new List<NoteLink>();

        foreach (var kvp in linkVisuals)
        {
            if (kvp.Value == null) // GameObject has been destroyed
            {
                linksToRemove.Add(kvp.Key);
                Debug.Log($"[NoteLinkManager] Found destroyed visual line for link: {kvp.Key}");
            }
        }

        foreach (var linkToRemove in linksToRemove)
        {
            linkVisuals.Remove(linkToRemove);
            Debug.Log($"[NoteLinkManager] Cleaned up destroyed visual line reference for link: {linkToRemove}");
        }

        if (linksToRemove.Count > 0)
        {
            Debug.Log($"[NoteLinkManager] Cleaned up {linksToRemove.Count} destroyed visual line references");
        }
    }

    /// <summary>
    /// Removes a specific link and its visual representation
    /// </summary>
    public void RemoveLink(NoteLink linkToRemove)
    {
        if (links.Contains(linkToRemove))
        {
            // Destroy the visual line GameObject if it exists
            if (linkVisuals.TryGetValue(linkToRemove, out GameObject lineObject))
            {
                if (lineObject != null)
                {
                    Debug.Log($"[NoteLinkManager] Destroying visual line for removed link: {linkToRemove}");
                    Destroy(lineObject);
                }
                linkVisuals.Remove(linkToRemove);
            }

            links.Remove(linkToRemove);
            Debug.Log($"[NoteLinkManager] Manually removed link: {linkToRemove}");
        }
        else
        {
            Debug.LogWarning($"[NoteLinkManager] Attempted to remove link that doesn't exist: {linkToRemove}");
        }
    }

    private void CreateLine(NoteLink link)
    {
        if (linePrefab == null)
        {
            Debug.LogError("[NoteLinkManager] Line prefab is not assigned!");
            return;
        }

        // Get the source and target notes
        if (!notes.TryGetValue(link.sourceNoteId, out NoteLinkable sourceNote))
        {
            Debug.LogError($"[NoteLinkManager] Source note {link.sourceNoteId} not found when creating line");
            return;
        }

        if (!notes.TryGetValue(link.targetNoteId, out NoteLinkable targetNote))
        {
            Debug.LogError($"[NoteLinkManager] Target note {link.targetNoteId} not found when creating line");
            return;
        }

        // Get the attach point transforms
        Transform sourceTransform = sourceNote.GetAttachPointTransform(link.sourceAttachPoint);
        Transform targetTransform = targetNote.GetAttachPointTransform(link.targetAttachPoint);

        if (sourceTransform == null)
        {
            Debug.LogError($"[NoteLinkManager] Source attach point transform {link.sourceAttachPoint} not found for note {link.sourceNoteId}");
            return;
        }

        if (targetTransform == null)
        {
            Debug.LogError($"[NoteLinkManager] Target attach point transform {link.targetAttachPoint} not found for note {link.targetNoteId}");
            return;
        }

        // Instantiate the line prefab
        GameObject lineObject = Instantiate(linePrefab, linesParent);
        lineObject.name = $"Line_{link.sourceNoteId}_{link.targetNoteId}";

        // Get the LineController component and set the attach points
        LineController lineController = lineObject.GetComponent<LineController>();
        if (lineController != null)
        {
            lineController.startPoint = sourceTransform;
            lineController.endPoint = targetTransform;

            // Store the relationship between the link and its visual representation
            linkVisuals[link] = lineObject;

            Debug.Log($"[NoteLinkManager] Successfully created line between {link.sourceNoteId}:{link.sourceAttachPoint} and {link.targetNoteId}:{link.targetAttachPoint}");
        }
        else
        {
            Debug.LogError("[NoteLinkManager] LineController component not found on instantiated line prefab!");
            Destroy(lineObject);
        }
    }
}
