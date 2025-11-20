using UnityEngine;
using UnityEngine.InputSystem;

public class CustomInputAction : MonoBehaviour
{

    // Menu button
    public InputActionReference menuButton;
    [SerializeField] private UIManager uiManager;
    private readonly string _menuCanvasName = "Menu";

    // Create Node (Click Right Thumbstick)
    public InputActionReference createNodeButton;

    [Header("Node Spawning")]
    [Tooltip("Reference to the NodeManager to handle note spawning")]
    [SerializeField] private NodeManager nodeManager;

    // Private variables for thumbstick detection
    private bool _previewStarted = false;

    void Start()
    {
        menuButton.action.started += ToggleMenu;

        // Subscribe to thumbstick press and release
        createNodeButton.action.started += OnThumbstickPressed;
        createNodeButton.action.canceled += OnThumbstickReleased;

        // Find NodeManager if not assigned
        if (nodeManager == null)
        {
            nodeManager = FindObjectOfType<NodeManager>();
            if (nodeManager == null)
            {
                Debug.LogError("[CustomInputAction] NodeManager not found!");
            }
            else
            {
                Debug.Log("[CustomInputAction] NodeManager found automatically");
            }
        }
    }

    void ToggleMenu(InputAction.CallbackContext context)
    {
        if (uiManager != null)
        {
            uiManager.ToggleCanvas(_menuCanvasName);
        }
    }

    void OnThumbstickPressed(InputAction.CallbackContext context)
    {
        Debug.Log("[CustomInputAction] Thumbstick PRESSED - Starting preview");

        if (nodeManager != null)
        {
            nodeManager.StartNodePreview();
            _previewStarted = true;
            Debug.Log("[CustomInputAction] Preview started successfully");
        }
        else
        {
            Debug.LogError("[CustomInputAction] NodeManager is null!");
        }
    }

    void OnThumbstickReleased(InputAction.CallbackContext context)
    {
        Debug.Log("[CustomInputAction] Thumbstick RELEASED");

        if (nodeManager != null && _previewStarted)
        {
            GameObject spawnedNote = nodeManager.FinishNodeCreation();
            if (spawnedNote != null)
            {
                Debug.Log("[CustomInputAction] Note spawned from preview successfully");
            }
            else
            {
                Debug.LogWarning("[CustomInputAction] Failed to spawn note from preview");
            }
        }
        else
        {
            Debug.Log("[CustomInputAction] No preview to finish or NodeManager is null");
        }

        _previewStarted = false;
    }

    void OnDestroy()
    {
        if (createNodeButton != null && createNodeButton.action != null)
        {
            createNodeButton.action.started -= OnThumbstickPressed;
            createNodeButton.action.canceled -= OnThumbstickReleased;
        }

        if (menuButton != null && menuButton.action != null)
        {
            menuButton.action.started -= ToggleMenu;
        }
    }
}
