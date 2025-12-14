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

/// <summary>
/// Individual save file data
/// Each save is stored as "{saveId}.json"
/// </summary>
[Serializable]
public class SaveData
{
    public string saveId;
    public string saveName; // User-friendly name
    public string sceneName; // Which base room scene this is (MindPalaceRoom1, etc.)
    public DateTime createdDate;
    public DateTime lastModified;
    
    // Room customization data
    public List<NoteData> notes = new List<NoteData>();
    public List<ObjectData> objects = new List<ObjectData>();
    
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
    public Vector3Data position;
    public QuaternionData rotation;
    public Vector3Data scale;
    public string colorHex; // Optional: for note color
    
    public NoteData(string id, string text, Vector3 pos, Quaternion rot, Vector3 scl)
    {
        noteId = id;
        content = text;
        position = new Vector3Data(pos);
        rotation = new QuaternionData(rot);
        scale = new Vector3Data(scl);
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