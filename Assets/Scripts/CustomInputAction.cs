using UnityEngine;
using UnityEngine.InputSystem;

public class CustomInputAction : MonoBehaviour
{

    // Menu button
    public InputActionReference menuButton;
    [SerializeField] private UIManager uiManager;
    private readonly string _menuCanvasName = "Menu";
    private readonly string _settingsCanvasName = "Settings";

    // Create Node (Primary Button)
    public InputActionReference createNodeButton;

    // Cancel Note Linking (Secondary Button)
    public InputActionReference cancelNoteLinkingButton;

    [Header("Node Spawning")]
    [Tooltip("Reference to the NodeManager to handle note spawning")]
    [SerializeField] private NodeManager nodeManager;

    // Private variables for primary button detection
    private bool _previewStarted = false;

    void Start()
    {
        menuButton.action.started += ToggleMenu;

        // Subscribe to primary button press and release
        createNodeButton.action.started += OnPrimaryButtonPressed;
        createNodeButton.action.canceled += OnPrimaryButtonReleased;

        cancelNoteLinkingButton.action.canceled += OnSecondaryButtonReleased; 

        // Find NodeManager if not assigned
        if (!nodeManager)
        {
            nodeManager = FindObjectOfType<NodeManager>();
            if (!nodeManager)
            {
                Debug.LogError("[CustomInputAction] NodeManager not found!");
            }
            else
            {
                Debug.Log("[CustomInputAction] NodeManager found automatically");
            }
        }

        // Find UIManager if not assigned
        if (!uiManager)
        {
            uiManager = FindObjectOfType<UIManager>();
            if (!uiManager)
            {
                Debug.LogError("[CustomInputAction] UIManager not found!");
            }
            else
            {
                Debug.Log("[CustomInputAction] UIManager found automatically");
            }
        }
    }

    void ToggleMenu(InputAction.CallbackContext context)
    {
        if (uiManager != null)
        {
            uiManager.HideCanvas(_settingsCanvasName);
            uiManager.ToggleCanvas(_menuCanvasName);
        }
    }

    void OnPrimaryButtonPressed(InputAction.CallbackContext context)
    {
        Debug.Log("[CustomInputAction] Primary Button PRESSED - Starting preview");

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

    void OnPrimaryButtonReleased(InputAction.CallbackContext context)
    {
        Debug.Log("[CustomInputAction] Primary Button RELEASED");

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

    void OnSecondaryButtonReleased(InputAction.CallbackContext context)
    {
        Debug.Log("[CustomInputAction] Secondary Button RELEASED");
        if (NoteLinkManager.Instance && NoteLinkManager.Instance.isGlobalLinkMode)
        {
            NoteLinkManager.Instance.CancelActiveLink();
        }
    }

    void OnDestroy()
    {
        if (createNodeButton != null && createNodeButton.action != null)
        {
            createNodeButton.action.started -= OnPrimaryButtonPressed;
            createNodeButton.action.canceled -= OnPrimaryButtonReleased;
        }

        if (menuButton != null && menuButton.action != null)
        {
            menuButton.action.started -= ToggleMenu;
        }
    }
}
