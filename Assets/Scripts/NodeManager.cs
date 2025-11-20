using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

public enum NodeType
{
    Note,
    Cube,
    Switchable
}

public class NodeManager : MonoBehaviour
{
    [Header("Node Selection")]
    [Tooltip("Currently selected node for spawning, will be set from the Hand Menu")]
    public NodeType selectedNode = NodeType.Note;

    [Header("Spawn Settings")]
    [Tooltip("The prefab to spawn as a note")]
    public GameObject notePrefab;

    [Tooltip("Cube Prefab to spawn as test")]
    public GameObject cubePrefab;

    [Tooltip("Switchable Prefab to spawn as test")]
    public GameObject switchablePrefab;

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
        if (nodesParent == null)
        {
            GameObject parentObj = new GameObject("Spawned Nodes");
            nodesParent = parentObj.transform;
        }

        // Validate setup
        if (notePrefab == null)
        {
            Debug.LogError("[NodeManager] No note prefab assigned!");
        }

        if (cubePrefab == null)
        {
            Debug.LogError("[NodeManager] No cube prefab assigned!");
        }

        if (rightControllerTransform == null)
        {
            Debug.LogWarning("[NodeManager] Right controller transform not assigned. Auto-discovery disabled.");
        }
        else
        {
            Debug.Log("[NodeManager] Right controller assigned: " + rightControllerTransform.name);
        }
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
    /// Get the currently selected prefab based on selectedTool
    /// </summary>
    private GameObject GetSelectedPrefab()
    {
        switch (selectedNode)
        {
            case NodeType.Note:
                return notePrefab;
            case NodeType.Cube:
                return cubePrefab;
            case NodeType.Switchable:
                return switchablePrefab;
            default:
                return notePrefab;
        }
    }

    /// <summary>
    /// Set the selected node type
    /// </summary>
    public void SetSelectedNode(NodeType nodeType)
    {
        selectedNode = nodeType;
        Debug.Log($"[NodeManager] Selected node changed to: {selectedNode}");
    }

    /// <summary>
    /// Start showing the object preview based on selected node
    /// </summary>
    public void StartNodePreview()
    {
        Debug.Log($"[NodeManager] StartPreview called for node: {selectedNode}");

        if (_isPreviewActive)
        {
            Debug.LogWarning("[NodeManager] Preview already active, ignoring request");
            return;
        }

        GameObject selectedPrefab = GetSelectedPrefab();
        if (selectedPrefab == null)
        {
            Debug.LogError($"[NodeManager] Cannot start preview: No prefab assigned for {selectedNode}!");
            return;
        }

        if (rightControllerTransform == null)
        {
            Debug.LogError("[NodeManager] Cannot start preview: Right controller transform not assigned!");
            return;
        }

        // Calculate spawn position
        Vector3 spawnPosition = rightControllerTransform.position + rightControllerTransform.TransformDirection(spawnOffset);
        Debug.Log($"[NodeManager] Creating {selectedNode} preview at position: {spawnPosition}");

        // Create preview object
        _currentPreview = Instantiate(selectedPrefab, spawnPosition, rightControllerTransform.rotation, nodesParent);
        Debug.Log($"[NodeManager] {selectedNode} preview object instantiated");

        //MakeTransparent(_currentPreview);

        // Disable colliders
        Collider[] colliders = _currentPreview.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        _isPreviewActive = true;
        Debug.Log($"[NodeManager] {selectedNode} preview is now active");
    }

    /// <summary>
    /// Spawn the actual object at preview position based on selected node
    /// </summary>
    public GameObject FinishNodeCreation()
    {
        Debug.Log($"[NodeManager] FinishCreation called for node: {selectedNode}");

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
        Debug.Log($"[NodeManager] Finishing {selectedNode} creation at position: {finalPosition}");

        // Destroy the preview
        DestroyImmediate(_currentPreview);
        _currentPreview = null;
        _isPreviewActive = false;
        Debug.Log("[NodeManager] Preview destroyed");

        // Create the actual object
        GameObject selectedPrefab = GetSelectedPrefab();
        GameObject actualObject = Instantiate(selectedPrefab, finalPosition, finalRotation, nodesParent);
        Debug.Log($"[NodeManager] Actual {selectedNode} created successfully");

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
