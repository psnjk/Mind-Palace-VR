using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main save index that stores references to all user saves
/// This is saved as "SaveIndex.json"
/// </summary>
[Serializable]
public class SaveIndex
{
    public List<string> saveIds = new List<string>(); // List of all save IDs
    
    public SaveIndex()
    {
        saveIds = new List<string>();
    }
    
    public void AddSave(string saveId)
    {
        if (!saveIds.Contains(saveId))
        {
            saveIds.Add(saveId);
        }
    }
    
    public void RemoveSave(string saveId)
    {
        saveIds.Remove(saveId);
    }
    
    public int GetSaveCount()
    {
        return saveIds.Count;
    }
}


[Serializable]
public class SaveData
{
    public string saveId;
    public string saveName; // User given name
    public string sceneName; // Which base room scene this is (Room1, etc.)
    public DateTime createdDate;
    public DateTime lastModified;
    
    // Room customization data
    public List<NoteData> notes = new List<NoteData>();
    public List<ObjectData> objects = new List<ObjectData>();
    public List<NoteLinkData> links = new List<NoteLinkData>();
    
    public SaveData(string id, string name, string scene)
    {
        saveId = id;
        saveName = name;
        sceneName = scene;
        createdDate = DateTime.Now;
        lastModified = DateTime.Now;
    }
}

/// <summary>
/// Data for a note placed in the room
/// </summary>
[Serializable]
public class NoteData
{
    public string noteId;
    public string content;
    public string prefabName; // The name of the prefab to instantiate
    public Vector3Data position;
    public QuaternionData rotation;
    public Vector3Data scale;
    public string colorHex; // Optional: for note color (keep for backwards compatibility or general use)
    public int colorTheme; // enum NoteColorTheme
    public bool isPinned;
    public bool followCamera;
    public bool useSmartFollow;
    public float followDistance;
    public float heightOffset;
    public Vector3Data localOffset;
    
    // Customization fields
    public float fontSize;
    public int textAlignHorizontal; // enum NoteTextAlignHorizontal
    public int textAlignVertical; // enum NoteTextAlignVertical
    
    public NoteData(string id, string text, string prefab, Vector3 pos, Quaternion rot, Vector3 scl, bool pinned = true, 
        bool follow = false, bool smartFollow = true, float dist = 2f, float height = 0f, Vector3? offset = null)
    {
        noteId = id;
        content = text;
        prefabName = prefab;
        position = new Vector3Data(pos);
        rotation = new QuaternionData(rot);
        scale = new Vector3Data(scl);
        isPinned = pinned;
        followCamera = follow;
        useSmartFollow = smartFollow;
        followDistance = dist;
        heightOffset = height;
        localOffset = new Vector3Data(offset ?? new Vector3(0, 0, 2f));
    }
}

[Serializable]
public class NoteLinkData
{
    public string sourceNoteId;
    public int sourceAttachPoint;
    public string targetNoteId;
    public int targetAttachPoint;
    public string colorHex;

    public NoteLinkData(string sourceId, int sourcePoint, string targetId, int targetPoint, string color = "#FFFFFF")
    {
        sourceNoteId = sourceId;
        sourceAttachPoint = sourcePoint;
        targetNoteId = targetId;
        targetAttachPoint = targetPoint;
        colorHex = color;
    }
}

/// <summary>
/// Data for objects placed/modified in the room
/// </summary>
[Serializable]
public class ObjectData
{
    public string objectId;
    public string prefabName; // Which prefab this object is
    public Vector3Data position;
    public QuaternionData rotation;
    public Vector3Data scale;
    public bool isActive;
    
    public ObjectData(string id, string prefab, Vector3 pos, Quaternion rot, Vector3 scl, bool active = true)
    {
        objectId = id;
        prefabName = prefab;
        position = new Vector3Data(pos);
        rotation = new QuaternionData(rot);
        scale = new Vector3Data(scl);
        isActive = active;
    }
}

/// <summary>
/// Serializable Vector3
/// </summary>
[Serializable]
public class Vector3Data
{
    public float x, y, z;
    
    public Vector3Data(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }
    
    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

/// <summary>
/// Serializable Quaternion
/// </summary>
[Serializable]
public class QuaternionData
{
    public float x, y, z, w;
    
    public QuaternionData(Quaternion quaternion)
    {
        x = quaternion.x;
        y = quaternion.y;
        z = quaternion.z;
        w = quaternion.w;
    }
    
    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
}