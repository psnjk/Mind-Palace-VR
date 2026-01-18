using System.Collections.Generic;
using UnityEngine;

public class HubManager : MonoBehaviour
{
    [Header("Main Hall References")]
    [SerializeField] private GameObject mainHall;
    [SerializeField] private GameObject mainHallSavesWall;
    
    [Header("SavesHallway Prefab")]
    [SerializeField] private GameObject savesHallwayPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private Vector3 firstHallwayPosition = new Vector3(10f, 0f, 0f);
    [SerializeField] private float hallwaySpacing = 10f; // Distance between hallways
    
    private List<GameObject> spawnedHallways = new List<GameObject>();
    
    void Start()
    {
        SetupHub();
    }
    
    void SetupHub()
    {
        // Get save count
        int saveCount = SaveManager.Instance.GetSaveCount();
        
        Debug.Log($"HubManager: Found {saveCount} saves");
        
        // Toggle main hall SavesWall
        if (mainHallSavesWall != null)
        {
            mainHallSavesWall.SetActive(saveCount == 0);
        }
        
        // If no saves then done
        if (saveCount == 0)
        {
            return;
        }
        
        // Spawn hallways
        SpawnSavesHallways(saveCount);
    }
    
    void SpawnSavesHallways(int saveCount)
    {
        List<string> saveIds = SaveManager.Instance.GetAllSaveIds();
        
        // 2 saves per hallway
        int hallwayCount = Mathf.CeilToInt(saveCount / 2f);
        
        for (int i = 0; i < hallwayCount; i++)
        {
            // position
            Vector3 spawnPosition = firstHallwayPosition + Vector3.right * (hallwaySpacing * i);
            
            // Spawn
            GameObject hallway = Instantiate(savesHallwayPrefab, spawnPosition, Quaternion.identity);
            hallway.transform.SetParent(transform);
            hallway.name = $"SavesHallway_{i + 1}";
            spawnedHallways.Add(hallway);
            
            // Get child objects
            Transform floor = hallway.transform.Find("Floor");
            Transform walls = hallway.transform.Find("Walls");
            Transform savesWall = hallway.transform.Find("SavesWall");
            Transform portalExp1 = hallway.transform.Find("PortalExp1");
            Transform savePanelExp1 = hallway.transform.Find("SavePanelExp1");
            Transform portalExp2 = hallway.transform.Find("PortalExp2");
            Transform savePanelExp2 = hallway.transform.Find("SavePanelExp2");
            Transform door = hallway.transform.Find("Door");
            
            // Calculate which saves this hallway displays
            int firstSaveIndex = i * 2;
            int secondSaveIndex = firstSaveIndex + 1;
            
            // Configure first portal
            if (firstSaveIndex < saveCount && portalExp1 != null)
            {
                Portal portal1 = portalExp1.GetComponent<Portal>();
                if (portal1 == null)
                {
                    portal1 = portalExp1.gameObject.AddComponent<Portal>();
                }
                portal1.SetSaveId(saveIds[firstSaveIndex]);
                
                Debug.Log($"HubManager: Portal 1 in hallway {i + 1} -> Save ID: {saveIds[firstSaveIndex]}");

                // Configure save panel 1
                if (savePanelExp1 != null)
                {
                    SaveControlPanel panel1 = savePanelExp1.GetComponent<SaveControlPanel>();
                    if (panel1 != null)
                    {
                        panel1.providedSaveId = saveIds[firstSaveIndex];
                    }
                    else
                    {
                        Debug.LogError($"HubManager: SaveControlPanel component not found on SavePanelExp1 in hallway {i + 1}");
                    }
                }
            }
            
            // Configure second portal (if exists)
            bool hasSecondSave = secondSaveIndex < saveCount;
            if (hasSecondSave && portalExp2 != null)
            {
                Portal portal2 = portalExp2.GetComponent<Portal>();
                if (portal2 == null)
                {
                    portal2 = portalExp2.gameObject.AddComponent<Portal>();
                }
                portal2.SetSaveId(saveIds[secondSaveIndex]);
                
                Debug.Log($"HubManager: Portal 2 in hallway {i + 1} -> Save ID: {saveIds[secondSaveIndex]}");

                // Configure save panel 2
                if (savePanelExp2 != null)
                {
                    savePanelExp2.gameObject.SetActive(true);
                    SaveControlPanel panel2 = savePanelExp2.GetComponent<SaveControlPanel>();
                    if (panel2 != null)
                    {
                        panel2.providedSaveId = saveIds[secondSaveIndex];
                    }
                    else
                    {
                        Debug.LogError($"HubManager: SaveControlPanel component not found on SavePanelExp2 in hallway {i + 1}");
                    }
                }

            }
            
            // door (active if only one save in this hallway)
            if (door != null)
            {
                door.gameObject.SetActive(!hasSecondSave);
            }
            
            // SavesWall (active only for last hallway)
            bool isLastHallway = (i == hallwayCount - 1);
            if (savesWall != null)
            {
                savesWall.gameObject.SetActive(isLastHallway);
            }
            
            Debug.Log($"HubManager: Spawned hallway {i + 1} at {spawnPosition}, Door: {!hasSecondSave}, SavesWall: {isLastHallway}");
        }
    }
    
    // refresh the hub
    public void RefreshHub()
    {
        // Destroy hallways
        foreach (GameObject hallway in spawnedHallways)
        {
            Destroy(hallway);
        }
        spawnedHallways.Clear();
        
        // Setup again
        SetupHub();
    }
}