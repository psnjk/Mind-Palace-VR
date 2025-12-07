using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NoteLinkManager : MonoBehaviour
{
    public static NoteLinkManager Instance;

    private Dictionary<string, NoteLinkable> notes = new();

    private List<(string noteId, AttachPoint attachPoint, string linkedNoteId, AttachPoint linkedAttachPoint)> links = new();


    public bool isGlobalLinkMode = false;

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
        isGlobalLinkMode = false;
        _tempStartNote = null;
        _tempStartAttachPoint = AttachPoint.None;

        Debug.Log("[NoteLinkManager] NoteLinkManager instance initialized and cleared");
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
        var linksToRemove = links.Where(link => link.noteId == noteId || link.linkedNoteId == noteId).ToList();

        foreach (var linkToRemove in linksToRemove)
        {
            links.Remove(linkToRemove);
            Debug.Log($"[NoteLinkManager] Removed link involving deleted note {noteId}: {linkToRemove.noteId}:{linkToRemove.attachPoint} -> {linkToRemove.linkedNoteId}:{linkToRemove.linkedAttachPoint}");
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

        // TODO: On the startNote, hide all attach points except the one that was clicked and disable it to avoid linking to itself
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
        links.Add((_tempStartNote.NoteID, _tempStartAttachPoint, endNote.NoteID, endAttachPoint));
        _tempStartNote = null;
        _tempStartAttachPoint = AttachPoint.None;
        isGlobalLinkMode = false;

        // Disable local link mode on all notes
        DisableLocalLinkModeAll();

        // print all links for debugging
        PrintAllLinks();
    }


    /// <summary>
    /// Rendered the link as line or similar between the two notes
    /// </summary>
    private void CreateLink()
    {

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
                  .Where(link => link.noteId == noteId)
                    .Select(link => (link.attachPoint, link.linkedNoteId, link.linkedAttachPoint));

        var incomingLinks = links
       .Where(link => link.linkedNoteId == noteId)
         .Select(link => (AttachPoint.None, link.noteId, link.attachPoint)); // Note: AttachPoint.None indicates this is an incoming link

        return outgoingLinks.Concat(incomingLinks).ToList();
    }

    /// <summary>
    /// Gets the count of all links involving a specific note
    /// </summary>
    public int GetLinkCountForNote(string noteId)
    {
        return links.Count(link => link.noteId == noteId || link.linkedNoteId == noteId);
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
                Debug.Log($"[NoteLinkManager] Link {i}: {link.noteId}:{link.attachPoint} -> {link.linkedNoteId}:{link.linkedAttachPoint}");
            }
        }

        Debug.Log("[NoteLinkManager] === End of Links ===");
    }
}
