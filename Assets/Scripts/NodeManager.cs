using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

public class NodeManager : MonoBehaviour
{
    [Header("Node Selection")]
    [Tooltip("Currently selected node index for spawning, will be set from the Hand Menu")]
    public int selectedNodeIndex = 0;

    [Header("Spawnable Prefabs")]
    public List<GameObject> spawnablePrefabs;

    [Tooltip("Reference to the Right Controller (ActionBasedController or XRBaseController)")]
    public Transform rightControllerTransform;

    [Tooltip("Offset from controller position to spawn the object")]
    public Vector3 spawnOffset = new Vector3(0f, 0.1f, 0.3f);

    [Tooltip("Parent object for spawned objects (for organization)")]
    public Transform nodesParent;

    // Preview system variables
    private GameObject _currentPreview;
    private bool _isPreviewActive = false;


    void Start()
    {
        // Create notes parent if it doesn't exist
        if (!nodesParent)
        {
            GameObject parentObj = new GameObject("Spawned Nodes");
            nodesParent = parentObj.transform;
        }

        // Assign the right controller transform if not assigned
        if (!rightControllerTransform)
        {
            SetupRightControllerTransform();
        }

        if (spawnablePrefabs.Count == 0)
        {
            SetupSpawnablePrefabs();
        }

        // Validate initial selected index
        ValidateSelectedIndex();
    }

    /// <summary>
    /// Find the spawnable prefabs in Resources folder and adds them to the list
    /// </summary>
    private void SetupSpawnablePrefabs()
    {
        GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>("Spawnables");
        spawnablePrefabs = new List<GameObject>(loadedPrefabs);
        Debug.Log("[NodeManager] Loaded " + spawnablePrefabs.Count + " spawnable prefabs.");

        // Validate selected index after loading prefabs
        ValidateSelectedIndex();
    }

    /// <summary>
    /// Validates and clamps the selected index to be within valid range
    /// </summary>
    private void ValidateSelectedIndex()
    {
        if (spawnablePrefabs.Count > 0)
        {
            selectedNodeIndex = Mathf.Clamp(selectedNodeIndex, 0, spawnablePrefabs.Count - 1);
        }
        else
        {
            selectedNodeIndex = 0;
        }
    }

    /// <summary>
    /// Finds the right controller transform if not assigned
    /// </summary>
    private void SetupRightControllerTransform()
    {
        // Search ALL scene objects, active or inactive (since in Unity this looks to be the only way to find inactive objects, because the controllers are inactive at the start of the scene, up until the user picks them up)
        var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (var t in allTransforms)
        {
            if (t.CompareTag("RightController"))
            {
                rightControllerTransform = t;
                Debug.Log("[NodeManager] Found Right Controller (inactive allowed).");
                return;
            }
        }

        Debug.LogError("[NodeManager] Could not find Right Controller!");
    }

    void Update()
    {
        // Update preview position if preview is active
        if (_isPreviewActive && _currentPreview != null && rightControllerTransform != null)
        {
            // Calculate new position based on current controller position
            Vector3 newPosition = rightControllerTransform.position + rightControllerTransform.TransformDirection(spawnOffset);

            // Update preview position and rotation to follow controller
            _currentPreview.transform.position = newPosition;
            _currentPreview.transform.rotation = rightControllerTransform.rotation;
        }
    }

    /// <summary>
    /// Get the currently selected prefab based on selectedNodeIndex
    /// </summary>
    private GameObject GetSelectedPrefab()
    {
        if (spawnablePrefabs.Count == 0)
        {
            Debug.LogError("[NodeManager] No spawnable prefabs available!");
            return null;
        }

        if (selectedNodeIndex < 0 || selectedNodeIndex >= spawnablePrefabs.Count)
        {
            Debug.LogError($"[NodeManager] Selected index {selectedNodeIndex} is out of range! Valid range: 0-{spawnablePrefabs.Count - 1}");
            return null;
        }

        return spawnablePrefabs[selectedNodeIndex];
    }

    /// <summary>
    /// Get the name of the currently selected prefab
    /// </summary>
    public string GetSelectedPrefabName()
    {
        GameObject selectedPrefab = GetSelectedPrefab();
        return selectedPrefab != null ? selectedPrefab.name : "None";
    }

    /// <summary>
    /// Set the selected node index
    /// </summary>
    public void SetSelectedNodeIndex(int index)
    {
        if (spawnablePrefabs.Count == 0)
        {
            Debug.LogWarning("[NodeManager] No spawnable prefabs available!");
            return;
        }

        if (index < 0 || index >= spawnablePrefabs.Count)
        {
            Debug.LogWarning($"[NodeManager] Index {index} is out of range! Valid range: 0-{spawnablePrefabs.Count - 1}");
            return;
        }

        selectedNodeIndex = index;
        string prefabName = GetSelectedPrefabName();
        Debug.Log($"[NodeManager] Selected node changed to index {selectedNodeIndex}: {prefabName}");
    }

    /// <summary>
    /// Get the total number of spawnable prefabs
    /// </summary>
    public int GetSpawnablePrefabCount()
    {
        return spawnablePrefabs.Count;
    }

    /// <summary>
    /// Get the name of a prefab at a specific index
    /// </summary>
    public string GetPrefabNameAtIndex(int index)
    {
        if (index < 0 || index >= spawnablePrefabs.Count)
        {
            return "Invalid Index";
        }

        return spawnablePrefabs[index] != null ? spawnablePrefabs[index].name : "Null Prefab";
    }

    /// <summary>
    /// Start showing the object preview based on selected node index
    /// </summary>
    public void StartNodePreview()
    {
        string prefabName = GetSelectedPrefabName();
        Debug.Log($"[NodeManager] StartPreview called for prefab: {prefabName} (index: {selectedNodeIndex})");

        if (_isPreviewActive)
        {
            Debug.LogWarning("[NodeManager] Preview already active, ignoring request");
            return;
        }

        GameObject selectedPrefab = GetSelectedPrefab();
        if (selectedPrefab == null)
        {
            Debug.LogError($"[NodeManager] Cannot start preview: No prefab available at index {selectedNodeIndex}!");
            return;
        }

        if (rightControllerTransform == null)
        {
            Debug.LogError("[NodeManager] Cannot start preview: Right controller transform not assigned!");
            return;
        }

        // Calculate spawn position
        Vector3 spawnPosition = rightControllerTransform.position + rightControllerTransform.TransformDirection(spawnOffset);
        Debug.Log($"[NodeManager] Creating {prefabName} preview at position: {spawnPosition}");

        // Create preview object
        _currentPreview = Instantiate(selectedPrefab, spawnPosition, rightControllerTransform.rotation, nodesParent);
        Debug.Log($"[NodeManager] {prefabName} preview object instantiated");

        //MakeTransparent(_currentPreview);

        // Disable colliders
        Collider[] colliders = _currentPreview.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        _isPreviewActive = true;
        Debug.Log($"[NodeManager] {prefabName} preview is now active");
    }

    /// <summary>
    /// Spawn the actual object at preview position based on selected node index
    /// </summary>
    public GameObject FinishNodeCreation()
    {
        string prefabName = GetSelectedPrefabName();
        Debug.Log($"[NodeManager] FinishCreation called for prefab: {prefabName} (index: {selectedNodeIndex})");

        if (!_isPreviewActive)
        {
            Debug.LogWarning("[NodeManager] No active preview to finish");
            return null;
        }

        if (_currentPreview == null)
        {
            Debug.LogWarning("[NodeManager] Current preview is null");
            return null;
        }

        // Store preview position and rotation
        Vector3 finalPosition = _currentPreview.transform.position;
        Quaternion finalRotation = _currentPreview.transform.rotation;
        Debug.Log($"[NodeManager] Finishing {prefabName} creation at position: {finalPosition}");

        // Destroy the preview
        DestroyImmediate(_currentPreview);
        _currentPreview = null;
        _isPreviewActive = false;
        Debug.Log("[NodeManager] Preview destroyed");

        // Create the actual object
        GameObject selectedPrefab = GetSelectedPrefab();
        GameObject actualObject = Instantiate(selectedPrefab, finalPosition, finalRotation, nodesParent);
        Debug.Log($"[NodeManager] Actual {prefabName} created successfully");


        // If the spawned object is a note, generate an ID for it and register it with NoteLinkManager
        var note = actualObject.GetComponent<NoteLinkable>();
        if (note)
            note.NoteID = System.Guid.NewGuid().ToString();
            NoteLinkManager.Instance.RegisterNote(note);

        return actualObject;
    }

    /// <summary>
    /// Cancel the preview without spawning
    /// </summary>
    public void CancelNodePreview()
    {
        Debug.Log("[NodeManager] CancelNodePreview called");

        if (_isPreviewActive && _currentPreview != null)
        {
            DestroyImmediate(_currentPreview);
            _currentPreview = null;
            _isPreviewActive = false;
            Debug.Log("[NodeManager] Preview cancelled");
        }
    }

    /// <summary>
    /// Make the preview object transparent
    /// </summary>
    void MakeTransparent(GameObject obj)
    {
        // TODO
    }
}
