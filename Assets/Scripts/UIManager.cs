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
        // Initialize canvas states according to startActive
        foreach (var uiCanvas in managedCanvases)
        {
            if (uiCanvas.canvas != null)
            {
                uiCanvas.canvas.gameObject.SetActive(uiCanvas.startActive);
            }
        }
    }

    /// <summary>
    /// Show a specific canvas
    /// </summary>
    /// <param name="canvasName">Name of the canvas to show</param>
    public void ShowCanvas(string canvasName)
    {
        UICanvas targetCanvas = managedCanvases.Find(c => c.name == canvasName);
        if (targetCanvas?.canvas != null)
        {
            targetCanvas.canvas.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Hide a specific canvas
    /// </summary>
    /// <param name="canvasName">Name of the canvas to hide</param>
    public void HideCanvas(string canvasName)
    {
        UICanvas targetCanvas = managedCanvases.Find(c => c.name == canvasName);
        if (targetCanvas?.canvas != null)
        {
            targetCanvas.canvas.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Hide all managed canvases
    /// </summary>
    public void HideAllCanvases()
    {
        foreach (var uiCanvas in managedCanvases)
        {
            if (uiCanvas.canvas != null)
            {
                uiCanvas.canvas.gameObject.SetActive(false);
            }
        }
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
