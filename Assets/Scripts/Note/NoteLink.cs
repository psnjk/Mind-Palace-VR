using System;


[Serializable]
public struct NoteLink
{
    public string sourceNoteId;
    public AttachPoint sourceAttachPoint;
    public string targetNoteId;
    public AttachPoint targetAttachPoint;

    public NoteLink(string sourceNoteId, AttachPoint sourceAttachPoint, string targetNoteId, AttachPoint targetAttachPoint)
    {
        this.sourceNoteId = sourceNoteId;
        this.sourceAttachPoint = sourceAttachPoint;
        this.targetNoteId = targetNoteId;
        this.targetAttachPoint = targetAttachPoint;
    }

    public override string ToString()
    {
        return $"{sourceNoteId}:{sourceAttachPoint} -> {targetNoteId}:{targetAttachPoint}";
    }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(sourceNoteId) && 
               !string.IsNullOrEmpty(targetNoteId) && 
               sourceAttachPoint != AttachPoint.None && 
               targetAttachPoint != AttachPoint.None;
    }

    public bool InvolvestNote(string noteId)
    {
        return sourceNoteId == noteId || targetNoteId == noteId;
    }
}