using UnityEngine;

public class MockSaveGenerator : MonoBehaviour
{
    [Header("Mock Save Settings")]
    [SerializeField] private int numberOfMockSaves = 3;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool clearAllSavesFirst = false;
    
    [Header("Default Scene Names")]
    [SerializeField] private string[] defaultSceneNames = new string[]
    {
        "MindPalaceRoom1",
        "MindPalaceRoom2",
        "MindPalaceRoom3"
    };
    
    void Start()
    {
        if (generateOnStart)
        {
            GenerateMockSaves();
        }
    }
    
    [ContextMenu("Generate Mock Saves")]
    public void GenerateMockSaves()
    {
        if (clearAllSavesFirst)
        {
            ClearAllSaves();
        }
        
        Debug.Log($"MockSaveGenerator: Creating {numberOfMockSaves} mock saves...");
        
        for (int i = 0; i < numberOfMockSaves; i++)
        {
            // Pick a random default scene
            string sceneName = defaultSceneNames[i % defaultSceneNames.Length];
            
            // Create mock save data
            string saveId = System.Guid.NewGuid().ToString();
            SaveData mockSave = new SaveData(saveId, $"Test Experience {i + 1}", sceneName);
            
            // Add some mock notes (optional)
            mockSave.notes.Add(new NoteData(
                System.Guid.NewGuid().ToString(),
                $"This is a test note for save {i + 1}",
                new Vector3(0, 1.5f, 2),
                Quaternion.identity,
                Vector3.one
            ));
            
            // Add some mock objects (optional)
            mockSave.objects.Add(new ObjectData(
                System.Guid.NewGuid().ToString(),
                "TestObject",
                new Vector3(1, 0, 1),
                Quaternion.identity,
                Vector3.one,
                true
            ));
            
            // Save it
            SaveManager.Instance.SaveData(mockSave);
            
            Debug.Log($"MockSaveGenerator: Created save {i + 1} - '{mockSave.saveName}' (ID: {saveId}) for scene '{sceneName}'");
        }
        
        Debug.Log($"MockSaveGenerator: Done! Total saves: {SaveManager.Instance.GetSaveCount()}");
    }
    
    [ContextMenu("Clear All Saves")]
    public void ClearAllSaves()
    {
        Debug.Log("MockSaveGenerator: Clearing all saves...");
        
        var saveIds = SaveManager.Instance.GetAllSaveIds();
        foreach (string id in saveIds)
        {
            SaveManager.Instance.DeleteSave(id);
        }
        
        Debug.Log($"MockSaveGenerator: Cleared {saveIds.Count} saves");
    }
    
    [ContextMenu("Print Current Saves")]
    public void PrintCurrentSaves()
    {
        int count = SaveManager.Instance.GetSaveCount();
        Debug.Log($"MockSaveGenerator: Current save count: {count}");
        
        var saveIds = SaveManager.Instance.GetAllSaveIds();
        for (int i = 0; i < saveIds.Count; i++)
        {
            SaveData data = SaveManager.Instance.GetSaveData(saveIds[i]);
            if (data != null)
            {
                Debug.Log($"  Save {i + 1}: '{data.saveName}' -> {data.sceneName} (ID: {data.saveId})");
            }
        }
    }
}