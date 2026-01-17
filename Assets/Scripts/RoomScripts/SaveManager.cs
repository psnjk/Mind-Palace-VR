using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private bool isExperienceLoading;

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
        
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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

    public void LoadScene(string sceneName)
    {
        currentSaveId = null;
        isExperienceLoading = false;
        SceneManager.LoadScene(sceneName);
    }

    public void LoadExperience(string saveId)
    {
        SaveData saveData = GetSaveData(saveId);
        if (saveData == null) return;

        currentSaveId = saveId;
        isExperienceLoading = true;
        SceneManager.LoadScene(saveData.sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isExperienceLoading && currentSaveId != null)
        {
            RestoreExperience(currentSaveId);
            isExperienceLoading = false;
        }
    }

    private void RestoreExperience(string saveId)
    {
        SaveData saveData = GetSaveData(saveId);
        if (saveData == null) return;

        Debug.Log($"Restoring experience: {saveData.saveName}");

        // 1. Restore Notes
        Dictionary<string, NoteLinkable> spawnedNotes = new Dictionary<string, NoteLinkable>();
        
        // Find the parent for spawned nodes
        Transform nodesParent = null;
        NodeManager nodeManager = FindFirstObjectByType<NodeManager>();
        if (nodeManager != null)
        {
            nodesParent = nodeManager.nodesParent;
        }

        // Fallback: Try to find by name or create it if NodeManager didn't provide it
        if (nodesParent == null)
        {
            GameObject parentObj = GameObject.Find("Spawned Nodes");
            if (parentObj == null)
            {
                parentObj = new GameObject("Spawned Nodes");
            }
            nodesParent = parentObj.transform;
            
            // If we found a NodeManager but it didn't have nodesParent set, update it
            if (nodeManager != null)
            {
                nodeManager.nodesParent = nodesParent;
            }
        }

        Debug.Log($"SaveManager: Starting to restore {saveData.notes.Count} notes");

        int noteIndex = 0;
        foreach (NoteData nData in saveData.notes)
        {
            noteIndex++;
            Debug.Log($"SaveManager: [{noteIndex}/{saveData.notes.Count}] Restoring note with content: '{nData.content}', ID: {nData.noteId}, Prefab: {nData.prefabName}");
            
            string prefabPath = "Spawnables/" + (string.IsNullOrEmpty(nData.prefabName) ? "Note" : nData.prefabName);
            GameObject notePrefab = Resources.Load<GameObject>(prefabPath);
            
            Debug.Log($"SaveManager: [{noteIndex}] Prefab load result: {(notePrefab != null ? "SUCCESS" : "FAILED")} for path: {prefabPath}");
            
            if (notePrefab == null)
            {
                Debug.LogError($"SaveManager: [{noteIndex}] Could not find prefab at {prefabPath}. Falling back to Note.");
                notePrefab = Resources.Load<GameObject>("Spawnables/Note");
                Debug.Log($"SaveManager: [{noteIndex}] Fallback prefab load result: {(notePrefab != null ? "SUCCESS" : "FAILED")}");
            }

            if (notePrefab != null)
            {
                Debug.Log($"SaveManager: [{noteIndex}] Instantiating note at position: {nData.position.ToVector3()}");
                GameObject noteObj = Instantiate(notePrefab, nData.position.ToVector3(), nData.rotation.ToQuaternion(), nodesParent);
                noteObj.transform.localScale = nData.scale.ToVector3();
                Debug.Log($"SaveManager: [{noteIndex}] Note instantiated successfully. GameObject: {noteObj.name}");
                
                Note note = noteObj.GetComponent<Note>();
                Debug.Log($"SaveManager: [{noteIndex}] Note component: {(note != null ? "FOUND" : "NOT FOUND")}");
                if (note != null && note.inputField != null)
                {
                    note.inputField.text = nData.content;
                }

                if (note != null && !nData.isPinned)
                {
                    note.ToggleLookAtCamera();
                }

                // Restore LookAtCamera settings
                LookAtCamera lookAt = noteObj.GetComponent<LookAtCamera>();
                if (lookAt != null)
                {
                    lookAt.followCamera = nData.followCamera;
                    lookAt.useSmartFollow = nData.useSmartFollow;
                    lookAt.followDistance = nData.followDistance;
                    lookAt.heightOffset = nData.heightOffset;
                    lookAt.localOffset = nData.localOffset.ToVector3();
                }

                // Restore Customizations
                NoteCustomizable customizable = noteObj.GetComponent<NoteCustomizable>();
                if (customizable != null)
                {
                    try
                    {
                        customizable.fontSize = nData.fontSize;
                        customizable.textAlignHorizontal = (NoteTextAlignHorizontal)nData.textAlignHorizontal;
                        customizable.textAlignVertical = (NoteTextAlignVertical)nData.textAlignVertical;
                        customizable.UpdateTextAlignment();
                        Debug.Log($"SaveManager: [{noteIndex}] Customizations applied successfully");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"SaveManager: [{noteIndex}] Failed to apply customizations: {e.Message}");
                    }
                }

                // Restore Color Theme
                NoteColorable colorable = noteObj.GetComponent<NoteColorable>();
                if (colorable != null)
                {
                    try
                    {
                        // Update color theme and apply it
                        var themeField = typeof(NoteColorable).GetField("colorTheme", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (themeField != null)
                        {
                            themeField.SetValue(colorable, (NoteColorTheme)nData.colorTheme);
                        }
                        colorable.ApplyColorTheme();
                        Debug.Log($"SaveManager: [{noteIndex}] Color theme applied successfully");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"SaveManager: [{noteIndex}] Failed to apply color theme: {e.Message}");
                    }
                }

                NoteLinkable linkable = noteObj.GetComponent<NoteLinkable>();
                if (linkable != null)
                {
                    linkable.SetNoteID(nData.noteId);
                    spawnedNotes[nData.noteId] = linkable;
                    
                    // Register with NoteLinkManager if it exists
                    if (NoteLinkManager.Instance != null)
                    {
                        NoteLinkManager.Instance.RegisterNote(linkable);
                    }
                    Debug.Log($"SaveManager: [{noteIndex}] NoteLinkable registered with ID: {nData.noteId}");
                }
                else
                {
                    Debug.LogWarning($"SaveManager: [{noteIndex}] NoteLinkable component not found on note");
                }
                
                Debug.Log($"SaveManager: [{noteIndex}] ✓ Note '{nData.content}' restored successfully!");
            }
            else
            {
                Debug.LogError($"SaveManager: [{noteIndex}] ✗ Failed to load prefab for note '{nData.content}'. Note skipped.");
            }
        }
        
        Debug.Log($"SaveManager: Finished restoring all {noteIndex} notes. Total spawned: {spawnedNotes.Count}");

        // 2. Restore Links
        if (NoteLinkManager.Instance != null)
        {
            foreach (NoteLinkData lData in saveData.links)
            {
                Color linkColor = Color.white;
                if (!string.IsNullOrEmpty(lData.colorHex))
                {
                    ColorUtility.TryParseHtmlString(lData.colorHex, out linkColor);
                }

                NoteLinkManager.Instance.CreateLink(
                    lData.sourceNoteId,
                    (AttachPoint)lData.sourceAttachPoint,
                    lData.targetNoteId,
                    (AttachPoint)lData.targetAttachPoint,
                    linkColor
                );
            }
        }
        
        // 3. Restore Objects (Placeholder)
        // Similar to notes but for other spawned objects
    }
    
    
    public void SaveCurrentRoomAsExperience(string experienceName)
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string saveId = System.Guid.NewGuid().ToString();
        SaveData newData = new SaveData(saveId, experienceName, sceneName);

            // 1. Collect Notes
        Note[] allNotes = FindObjectsByType<Note>(FindObjectsSortMode.None);
        foreach (Note note in allNotes)
        {
            NoteLinkable linkable = note.GetComponent<NoteLinkable>();
            if (linkable == null) continue;

            LookAtCamera lookAt = note.GetComponent<LookAtCamera>();
            bool follow = false;
            bool smartFollow = true;
            float dist = 2f;
            float height = 0f;
            Vector3 localOffset = new Vector3(0, 0, 2f);

            if (lookAt != null)
            {
                follow = lookAt.followCamera;
                smartFollow = lookAt.useSmartFollow;
                dist = lookAt.followDistance;
                height = lookAt.heightOffset;
                localOffset = lookAt.localOffset;
            }

            // Extract prefab name (remove (Clone) and spaces)
            string prefabName = note.gameObject.name.Replace("(Clone)", "").Trim();

            NoteData nData = new NoteData(
                linkable.NoteID,
                note.inputField.text,
                prefabName,
                note.transform.position,
                note.transform.rotation,
                note.transform.localScale,
                note.IsPinned,
                follow,
                smartFollow,
                dist,
                height,
                localOffset
            );

            // Capture Customizations
            NoteCustomizable customizable = note.GetComponent<NoteCustomizable>();
            if (customizable != null)
            {
                nData.fontSize = customizable.fontSize;
                nData.textAlignHorizontal = (int)customizable.textAlignHorizontal;
                nData.textAlignVertical = (int)customizable.textAlignVertical;
            }

            // Capture Color Theme
            NoteColorable colorable = note.GetComponent<NoteColorable>();
            if (colorable != null)
            {
                var themeField = typeof(NoteColorable).GetField("colorTheme", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (themeField != null)
                {
                    nData.colorTheme = (int)themeField.GetValue(colorable);
                }
            }

            newData.notes.Add(nData);
        }

        // 2. Collect Links
        if (NoteLinkManager.Instance != null)
        {
            // We need a way to access the private links list or a public getter in NoteLinkManager
            // For now, assuming we add a public getter or use the existing structure
            var allLines = NoteLinkManager.Instance.GetComponentsInChildren<LineController>();
            foreach (var line in allLines)
            {
                var link = line.AssociatedLink;
                string colorHex = "#" + ColorUtility.ToHtmlStringRGBA(line.normalColor);
                
                newData.links.Add(new NoteLinkData(
                    link.sourceNoteId,
                    (int)link.sourceAttachPoint,
                    link.targetNoteId,
                    (int)link.targetAttachPoint,
                    colorHex
                ));
            }
        }

        // 3. Collect Objects (by a specific tag or component for spawned objects)
        // a placeholder for custom objects spawned by user
        // we might add them later
        // GameObject[] spawnedObjects = GameObject.FindGameObjectsWithTag("SpawnedObject");
        // foreach (GameObject obj in spawnedObjects)
        // {
        //     newData.objects.Add(new ObjectData(
        //         System.Guid.NewGuid().ToString(),
        //         obj.name.Replace("(Clone)", "").Trim(),
        //         obj.transform.position,
        //         obj.transform.rotation,
        //         obj.transform.localScale
        //     ));
        // }

        SaveData(newData);
        Debug.Log($"[SaveManager] Experience '{experienceName}' saved successfully with ID: {saveId}");
    }
    
}