using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private static SaveManager instance;
    public static SaveManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("SaveManager");
                instance = go.AddComponent<SaveManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    
    private string saveDirectory;
    private string saveIndexPath;
    private SaveIndex saveIndex;
    private string currentSaveId; // Used when loading a saved experience
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
        saveIndexPath = Path.Combine(saveDirectory, "SaveIndex.json");
        
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }
        
        LoadSaveIndex();
    }
    
    private void LoadSaveIndex()
    {
        if (File.Exists(saveIndexPath))
        {
            string json = File.ReadAllText(saveIndexPath);
            saveIndex = JsonUtility.FromJson<SaveIndex>(json);
        }
        else
        {
            saveIndex = new SaveIndex();
            SaveIndexToDisk();
        }
    }
    
    private void SaveIndexToDisk()
    {
        string json = JsonUtility.ToJson(saveIndex, true);
        File.WriteAllText(saveIndexPath, json);
    }
    
    public SaveIndex GetSaveIndex()
    {
        return saveIndex;
    }
    
    public SaveData GetSaveData(string saveId)
    {
        string savePath = Path.Combine(saveDirectory, $"{saveId}.json");
        
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            return JsonUtility.FromJson<SaveData>(json);
        }
        
        Debug.LogWarning($"SaveManager: Save file not found for ID {saveId}");
        return null;
    }
    
    public void SaveData(SaveData saveData)
    {
        saveData.lastModified = System.DateTime.Now;
        
        string savePath = Path.Combine(saveDirectory, $"{saveData.saveId}.json");
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
        
        // Update index
        saveIndex.AddSave(saveData.saveId);
        SaveIndexToDisk();
        
        Debug.Log($"SaveManager: Saved experience {saveData.saveId}");
    }
    
    public void DeleteSave(string saveId)
    {
        string savePath = Path.Combine(saveDirectory, $"{saveId}.json");
        
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }
        
        saveIndex.RemoveSave(saveId);
        SaveIndexToDisk();
        
        Debug.Log($"SaveManager: Deleted save {saveId}");
    }
    
    public string CreateNewSave(string saveName, string sceneName)
    {
        string saveId = System.Guid.NewGuid().ToString();
        SaveData newSave = new SaveData(saveId, saveName, sceneName);
        SaveData(newSave);
        return saveId;
    }
    
    public void SetCurrentSaveId(string saveId)
    {
        currentSaveId = saveId;
    }
    
    public string GetCurrentSaveId()
    {
        return currentSaveId;
    }
    
    public void ClearCurrentSaveId()
    {
        currentSaveId = null;
    }
    
    public int GetSaveCount()
    {
        return saveIndex.GetSaveCount();
    }
    
    public List<string> GetAllSaveIds()
    {
        return new List<string>(saveIndex.saveIds);
    }
}