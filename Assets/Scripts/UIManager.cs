using UnityEngine;
using System.Collections.Generic;

/// <summary>
///  UI Manager to handle all UI operations.
/// </summary>
public class UIManager : MonoBehaviour
{
    [System.Serializable]
    public class UICanvas
    {
        public string name;
        public Canvas canvas;
        public bool startActive = false;
    }

    [SerializeField]
    [Tooltip("List of all UI canvases managed by the UI Manager")]
    private List<UICanvas> managedCanvases = new List<UICanvas>();

    private void Start()
    {
        Debug.Log($"[UIManager] Initializing {managedCanvases.Count} managed canvases");

        // Find all canvases in the scene if none are assigned
        if (managedCanvases.Count == 0)
        {
            SetupCanvases();
        }

        // Initialize canvas states according to startActive
        foreach (var uiCanvas in managedCanvases)
        {
            if (uiCanvas.canvas != null)
            {
                uiCanvas.canvas.gameObject.SetActive(uiCanvas.startActive);
                Debug.Log($"[UIManager] Canvas '{uiCanvas.name}' initialized - Active: {uiCanvas.startActive}");
            }
            else
            {
                Debug.LogWarning($"[UIManager] Canvas '{uiCanvas.name}' has null canvas reference!");
            }
        }

        Debug.Log("[UIManager] Initialization complete");
    }

    /// <summary>
    /// Setups the canvases from the scene.
    /// </summary>
    private void SetupCanvases()
    {
        // Clear existing managed canvases
        managedCanvases.Clear();

        // Find all Canvas components in the scene (including inactive ones)
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);

        Debug.Log($"[UIManager] Found {allCanvases.Length} total canvases in scene");

        // Search for the specific canvases we want to manage
        foreach (Canvas canvas in allCanvases)
        {
            string canvasName = canvas.name;

            // Settings Canvas
            if (canvasName == "Settings Canvas")
            {
                UICanvas settingsCanvas = new UICanvas
                {
                    name = "Settings",
                    canvas = canvas,
                    startActive = false
                };
                managedCanvases.Add(settingsCanvas);
                Debug.Log($"[UIManager] Found and added Settings Canvas: {canvas.name}");
            }
            else if (canvasName == "Hand Canvas")
            {
                UICanvas menuCanvas = new UICanvas
                {
                    name = "Menu",
                    canvas = canvas,
                    startActive = false
                };
                managedCanvases.Add(menuCanvas);
                Debug.Log($"[UIManager] Found and added Hand Canvas as Menu: {canvas.name}");
            }
            else if (canvasName == "Console Canvas")
            {
                UICanvas consoleCanvas = new UICanvas
                {
                    name = "Console",
                    canvas = canvas,
                    startActive = false
                };
                managedCanvases.Add(consoleCanvas);
                Debug.Log($"[UIManager] Found and added Console Canvas: {canvas.name}");
            }
        }

        Debug.Log($"[UIManager] SetupCanvases complete - Added {managedCanvases.Count} managed canvases");
    }

    /// <summary>
    /// Toggle a specific canvas
    /// </summary>
    /// <param name="canvasName">Name of the canvas to toggle</param>
    public void ToggleCanvas(string canvasName)
    {
        Debug.Log($"[UIManager] ToggleCanvas called for: '{canvasName}'");

        UICanvas targetCanvas = managedCanvases.Find(c => c.name == canvasName);
        if (targetCanvas?.canvas != null)
        {
            bool isActive = targetCanvas.canvas.gameObject.activeSelf;
            targetCanvas.canvas.gameObject.SetActive(!isActive);
            Debug.Log($"[UIManager] Canvas '{canvasName}' toggled from {isActive} to {!isActive}");
        }
        else
        {
            Debug.LogWarning($"[UIManager] Canvas '{canvasName}' not found or has null canvas reference!");
        }
    }

    /// <summary>
    /// Show a specific canvas
    /// </summary>
    /// <param name="canvasName">Name of the canvas to show</param>
    public void ShowCanvas(string canvasName)
    {
        Debug.Log($"[UIManager] ShowCanvas called for: '{canvasName}'");

        UICanvas targetCanvas = managedCanvases.Find(c => c.name == canvasName);
        if (targetCanvas?.canvas != null)
        {
            targetCanvas.canvas.gameObject.SetActive(true);
            Debug.Log($"[UIManager] Canvas '{canvasName}' shown");
        }
        else
        {
            Debug.LogWarning($"[UIManager] Canvas '{canvasName}' not found or has null canvas reference!");
        }
    }

    /// <summary>
    /// Hide a specific canvas
    /// </summary>
    /// <param name="canvasName">Name of the canvas to hide</param>
    public void HideCanvas(string canvasName)
    {
        Debug.Log($"[UIManager] HideCanvas called for: '{canvasName}'");

        UICanvas targetCanvas = managedCanvases.Find(c => c.name == canvasName);
        if (targetCanvas?.canvas != null)
        {
            targetCanvas.canvas.gameObject.SetActive(false);
            Debug.Log($"[UIManager] Canvas '{canvasName}' hidden");
        }
        else
        {
            Debug.LogWarning($"[UIManager] Canvas '{canvasName}' not found or has null canvas reference!");
        }
    }

    /// <summary>
    /// Hide all managed canvases
    /// </summary>
    public void HideAllCanvases()
    {
        Debug.Log("[UIManager] HideAllCanvases called");

        foreach (var uiCanvas in managedCanvases)
        {
            if (uiCanvas.canvas != null)
            {
                uiCanvas.canvas.gameObject.SetActive(false);
                Debug.Log($"[UIManager] Canvas '{uiCanvas.name}' hidden");
            }
            else
            {
                Debug.LogWarning($"[UIManager] Canvas '{uiCanvas.name}' has null canvas reference!");
            }
        }

        Debug.Log("[UIManager] HideAllCanvases complete");
    }

    /// <summary>
    /// Show only the specified canvas and hide all others
    /// </summary>
    /// <param name="canvasName">Name of the canvas to show exclusively</param>
    public void ShowCanvasExclusive(string canvasName)
    {
        foreach (var uiCanvas in managedCanvases)
        {
            if (uiCanvas.canvas != null)
            {
                uiCanvas.canvas.gameObject.SetActive(uiCanvas.name == canvasName);
            }
        }
    }
}
