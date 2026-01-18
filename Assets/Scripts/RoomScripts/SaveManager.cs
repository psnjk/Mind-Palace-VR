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
        bool isNewSave = !File.Exists(savePath);
        
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(savePath, json);
        
        // Only add to index if it's a new save
        if (isNewSave)
        {
            saveIndex.AddSave(saveData.saveId);
            SaveIndexToDisk();
            Debug.Log($"SaveManager: Created new save {saveData.saveId}");
        }
        else
        {
            Debug.Log($"SaveManager: Updated existing save {saveData.saveId}");
        }
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
            StartCoroutine(RestoreExperienceCoroutine(currentSaveId));
            isExperienceLoading = false;
        }
    }

    private System.Collections.IEnumerator RestoreExperienceCoroutine(string saveId)
    {
        SaveData saveData = GetSaveData(saveId);
        if (saveData == null) yield break;

        Debug.Log($"Restoring experience: {saveData.saveName}");
        
        // Wait for scene to be fully loaded
        yield return new WaitForEndOfFrame();

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

                // Restore LookAtCamera settings and enabled state
                LookAtCamera lookAt = noteObj.GetComponent<LookAtCamera>();
                if (lookAt != null)
                {
                    lookAt.followCamera = nData.followCamera;
                    lookAt.useSmartFollow = nData.useSmartFollow;
                    lookAt.followDistance = nData.followDistance;
                    lookAt.heightOffset = nData.heightOffset;
                    lookAt.localOffset = nData.localOffset.ToVector3();
                    lookAt.enabled = !nData.isPinned;
                }
                else
                {
                    Debug.LogWarning($"SaveManager: [{noteIndex}] LookAtCamera component not found on note");
                }
                
                // Set the Note's internal _lookAtCamera field via reflection to prevent Start() from overriding
                if (note != null)
                {
                    var lookAtField = typeof(Note).GetField("_lookAtCamera", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (lookAtField != null)
                    {
                        lookAtField.SetValue(note, !nData.isPinned);
                    }
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
                
                Debug.Log($"SaveManager: [{noteIndex}] Note '{nData.content}' restored successfully!");
            }
            else
            {
                Debug.LogError($"SaveManager: [{noteIndex}] Failed to load prefab for note '{nData.content}'. Note skipped.");
            }
        }
        
        Debug.Log($"SaveManager: Finished restoring all {noteIndex} notes. Total spawned: {spawnedNotes.Count}");

        // Wait for all notes to be fully initialized with their attach points, otherwise links will fail to be restored 
        yield return StartCoroutine(WaitForNotesInitialization(spawnedNotes));

        // 2. Restore Links
        if (NoteLinkManager.Instance != null)
        {
            Debug.Log($"SaveManager: Starting to restore {saveData.links.Count} links");
            
            int linkIndex = 0;
            foreach (NoteLinkData lData in saveData.links)
            {
                linkIndex++;
                Debug.Log($"SaveManager: [{linkIndex}/{saveData.links.Count}] Restoring link from {lData.sourceNoteId}:{lData.sourceAttachPoint} to {lData.targetNoteId}:{lData.targetAttachPoint}");
                
                Color linkColor = Color.white;
                if (!string.IsNullOrEmpty(lData.colorHex))
                {
                    bool colorParsed = ColorUtility.TryParseHtmlString(lData.colorHex, out linkColor);
                    Debug.Log($"SaveManager: [{linkIndex}] Color parsed: {colorParsed}, Color: {linkColor}, Hex: {lData.colorHex}");
                }

                NoteLinkManager.Instance.CreateLink(
                    lData.sourceNoteId,
                    (AttachPoint)lData.sourceAttachPoint,
                    lData.targetNoteId,
                    (AttachPoint)lData.targetAttachPoint,
                    linkColor
                );
                
                Debug.Log($"SaveManager: [{linkIndex}] CreateLink called");
            }
            
            Debug.Log($"SaveManager: Finished restoring all {linkIndex} links");
        }
        else
        {
            Debug.LogError("SaveManager: NoteLinkManager.Instance is null! Cannot restore links.");
        }
        
        // 3. Restore Objects (Placeholder)
        // Similar to notes but for other spawned objects
    }

    private System.Collections.IEnumerator WaitForNotesInitialization(Dictionary<string, NoteLinkable> spawnedNotes)
    {
        Debug.Log($"SaveManager: Waiting for {spawnedNotes.Count} notes to initialize...");
        
        float timeout = 5f; // 5 second timeout
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            bool allNotesReady = true;
            
            foreach (var kvp in spawnedNotes)
            {
                NoteLinkable note = kvp.Value;
                
                // Check if note still exists
                if (note == null || note.gameObject == null)
                {
                    Debug.LogWarning($"SaveManager: Note {kvp.Key} was destroyed during initialization");
                    allNotesReady = false;
                    break;
                }
                
                // Check if note is registered in NoteLinkManager
                if (NoteLinkManager.Instance == null)
                {
                    allNotesReady = false;
                    break;
                }
                
                // Verify attach points are initialized by checking if we can get them
                bool attachPointsReady = true;
                try
                {
                    Transform topPoint = note.GetAttachPointTransform(AttachPoint.Top);
                    Transform bottomPoint = note.GetAttachPointTransform(AttachPoint.Bottom);
                    Transform leftPoint = note.GetAttachPointTransform(AttachPoint.Left);
                    Transform rightPoint = note.GetAttachPointTransform(AttachPoint.Right);
                    
                    if (topPoint == null || bottomPoint == null || leftPoint == null || rightPoint == null)
                    {
                        attachPointsReady = false;
                    }
                }
                catch
                {
                    attachPointsReady = false;
                }
                
                if (!attachPointsReady)
                {
                    allNotesReady = false;
                    break;
                }
            }
            
            if (allNotesReady)
            {
                Debug.Log($"SaveManager: All notes initialized successfully after {elapsed:F2} seconds");
                yield break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Debug.LogWarning($"SaveManager: Timed out waiting for notes to initialize after {timeout} seconds. Proceeding anyway...");
    }
    
    /// <summary>
    /// Saves the current room as a new experience or updates an existing one. If saveId is null, creates a new save.
    /// </summary> 
    public void SaveCurrentRoomAsExperience(string saveId = null)
    {
        string experienceName;
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (string.IsNullOrEmpty(saveId))
        {
            saveId = System.Guid.NewGuid().ToString();
            experienceName = $"{sceneName}_{System.DateTime.Now:yyyyMMdd_HHmmss}"; 
        }
        else
        {
            // Update existing save - preserve the experience name
            SaveData existingSave = GetSaveData(saveId);
            if (existingSave != null)
            {
                experienceName = existingSave.saveName;
            }
            else
            {
                // Fallback if save doesn't exist
                experienceName = $"{sceneName}_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            }
        }
        
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
                lookAt == null || !lookAt.enabled,
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
            var allLinksWithVisuals = NoteLinkManager.Instance.GetAllLinksWithVisuals();
            foreach (var (link, lineObj) in allLinksWithVisuals)
            {
                string colorHex = "#FFFFFFFF"; // Default white
                
                if (lineObj != null)
                {
                    LineController lineController = lineObj.GetComponent<LineController>();
                    if (lineController != null)
                    {
                        colorHex = "#" + ColorUtility.ToHtmlStringRGBA(lineController.normalColor);
                    }
                }
                
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
        
        // Set this as the current save ID so future saves update it
        SetCurrentSaveId(saveId);
        
        Debug.Log($"[SaveManager] Experience '{experienceName}' saved successfully with ID: {saveId}");
    }

    public string GetSaveName(string saveId)
    {
        SaveData saveData = GetSaveData(saveId);
        return saveData != null ? saveData.saveName : null;
    }

    public bool UpdateSaveName(string saveId, string newSaveName)
    {
        SaveData saveData = GetSaveData(saveId);
        if (saveData != null)
        {
            saveData.saveName = newSaveName;
            SaveData(saveData);
            Debug.Log($"SaveManager: Updated save name for {saveId} to '{newSaveName}'");
            return true;
        }
        else
        {
            Debug.LogWarning($"SaveManager: Cannot update save name. Save with ID {saveId} not found.");
            return false;
        }
    }
    
    
}