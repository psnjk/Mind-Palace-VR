using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.InputSystem;

public class LineController : MonoBehaviour
{

    private LineRenderer _lineRenderer;

    public Transform startPoint;
    public Transform endPoint;

    private XRSimpleInteractable _simpleInteractable;
    private MeshCollider _meshCollider;
    private Canvas _lineCanvas;
    private LineCanvas _lineCanvasScript;

    // Reference to the NoteLink that this line represents
    private NoteLink _associatedLink;

    [Header("XR Input")]
    [Tooltip("Input action reference for showing canvas")]
    public InputActionProperty showCanvasAction;

    [Header("Canvas Settings")]
    [Tooltip("Distance to offset canvas forward from hit point")]
    public float canvasOffset = 0.1f;

    public Color normalColor = new Color(1, 1, 1, 1f);
    public Color highlightColor = new Color(0.1f, 0.1f, 0.1f, 1f);

    private bool _isHovered = false;
    private bool _buttonWasPressed = false;
    private IXRHoverInteractor _currentInteractor;

    [Header("Collider Settings")]
    [Tooltip("Distance in Unity units to exclude from collider at start")]
    public float startDeadzoneDistance = 0.05f;

    [Tooltip("Distance in Unity units to exclude from collider at end")]
    public float endDeadzoneDistance = 0.05f;

    /// <summary>
    /// Sets the NoteLink that this LineController represents. This should be called when the line is created.
    /// </summary>
    /// <param name="noteLink">The NoteLink associated with this line</param>
    public void SetAssociatedLink(NoteLink noteLink)
    {
        _associatedLink = noteLink;
        Debug.Log($"[LineController] Associated link set: {noteLink}");

        // If the canvas script is already set up, pass the link to it
        if (_lineCanvasScript != null)
        {
            _lineCanvasScript.SetAssociatedLink(noteLink);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupLineRenderer();

        GenerateMeshCollider();

        SetupSimpleInteractable();

        SetupLineCanvas();
    }

    private void SetupLineRenderer()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (!_lineRenderer)
        {
            Debug.LogError("[LineController] LineRenderer component not found!");
            return;
        }

        _lineRenderer.positionCount = 2;

        // Assign start and end points if they are set
        if (!startPoint || !endPoint) return;
        _lineRenderer.SetPosition(0, startPoint.position);
        _lineRenderer.SetPosition(1, endPoint.position);



        _lineRenderer.startColor = normalColor;
        _lineRenderer.endColor = normalColor;

    }

    private void SetupSimpleInteractable()
    {
        _simpleInteractable = GetComponent<XRSimpleInteractable>();
        if (!_simpleInteractable)
        {
            Debug.LogWarning($"[LineController] No XRSimpleInteractable component found on {gameObject.name}");
            return;
        }


        _simpleInteractable.hoverEntered.AddListener(OnLineHoverEntered);
        _simpleInteractable.hoverExited.AddListener(OnLineHoverExited);
    }

    private void SetupLineCanvas()
    {
        _lineCanvas = transform.Find("Line Canvas").gameObject.GetComponent<Canvas>();
        if (!_lineCanvas)
        {
            Debug.LogWarning($"[LineController] No Line Canvas found in children of {gameObject.name}");
            return;
        }

        _lineCanvasScript = _lineCanvas.GetComponent<LineCanvas>();
        if (!_lineCanvasScript)
        {
            Debug.LogWarning($"[LineController] No LineCanvas script found on Line Canvas");
            return;
        }

        // If we already have an associated link, pass it to the canvas
        if (_associatedLink.IsValid())
        {
            _lineCanvasScript.SetAssociatedLink(_associatedLink);
        }
    }

    private void OnLineHoverEntered(HoverEnterEventArgs args)
    {
        Debug.Log($"[LineController] OnLineHoverEntered() called.");
        _isHovered = true;
        _currentInteractor = args.interactorObject;
        Highlight(true);
        // TODO: make the line highlight or something
    }

    private void OnLineHoverExited(HoverExitEventArgs args)
    {
        Debug.Log($"[LineController] OnLineHoverExited() called.");
        _isHovered = false;
        _currentInteractor = null;
        Highlight(false);

        /*
        // Hide canvas when no longer hovering
        if (_lineCanvas != null)
        {
            _lineCanvas.gameObject.SetActive(false);
            Debug.Log($"[LineController] Canvas hidden after hover exit");
        }
        */
    }

    // Update is called once per frame
    void Update()
    {
        if (!startPoint || !endPoint) return;

        // Check if positions have changed
        Vector3 newStartPos = startPoint.position;
        Vector3 newEndPos = endPoint.position;
        Vector3 oldStartPos = _lineRenderer.GetPosition(0);
        Vector3 oldEndPos = _lineRenderer.GetPosition(1);

        bool positionsChanged = Vector3.Distance(newStartPos, oldStartPos) > 0.001f || Vector3.Distance(newEndPos, oldEndPos) > 0.001f;

        if (positionsChanged)
        {
            // Update line positions
            _lineRenderer.SetPosition(0, newStartPos);
            _lineRenderer.SetPosition(1, newEndPos);

        }

        // Check if object is hovered and button is pressed
        if (_isHovered && showCanvasAction.action != null && _currentInteractor != null)
        {
            bool isPressed = showCanvasAction.action.ReadValue<float>() > 0.5f;

            // Detect button press (was not pressed, now is pressed)
            if (isPressed && !_buttonWasPressed)
            {
                ShowCanvasAtInteractionPoint();
            }

            _buttonWasPressed = isPressed;
        }
        else
        {
            _buttonWasPressed = false;
        }
    }

    private void ShowCanvasAtInteractionPoint()
    {
        Debug.Log($"[LineController] Button pressed while hovering - showing canvas");

        if (_lineCanvas == null)
        {
            Debug.LogWarning($"[LineController] Canvas not found");
            return;
        }

        Vector3 hitPoint = Vector3.zero;
        bool hitPointFound = false;

        // Simple "gun shooting" style raycast from the interactor
        Vector3 rayOrigin = _currentInteractor.transform.position;
        Vector3 rayDirection = _currentInteractor.transform.forward;

        Debug.Log($"[LineController] Casting sphere ray from {rayOrigin} in direction {rayDirection}");


        // Use SphereCast to detect hit on the line
        if (Physics.SphereCast(rayOrigin, 0.1f, rayDirection, out RaycastHit hit, Mathf.Infinity))
        {
            Debug.Log($"[LineController] SphereCast hit: {hit.collider.name} at point {hit.point}");
            if (hit.collider == _meshCollider)
            {
                hitPoint = hit.point;
                hitPointFound = true;
                Debug.Log($"[LineController] Hit our line at point: {hitPoint}");
            }
        }

        if (hitPointFound)
        {
            // Orient the canvas to face the interactor first
            Vector3 directionToInteractor = (rayOrigin - hitPoint).normalized;
            _lineCanvas.transform.rotation = Quaternion.LookRotation(-directionToInteractor);

            // Position the canvas at the hit point, offset forward by the specified distance
            Vector3 offsetPosition = hitPoint + directionToInteractor * canvasOffset;
            _lineCanvas.transform.position = offsetPosition;

            // Show the canvas
            _lineCanvas.gameObject.SetActive(true);

            Debug.Log($"[LineController] Canvas shown at interaction point: {hitPoint}, offset to: {offsetPosition}");
        }
        else
        {
            Debug.LogWarning($"[LineController] SphereCast didn't hit our line - no canvas shown");
        }
    }

    /// <summary>
    /// Get the closest point on the line segment to a given point
    /// </summary>
    private Vector3 GetClosestPointOnLine(Vector3 point)
    {
        Vector3 startPos = startPoint.position;
        Vector3 endPos = endPoint.position;

        Vector3 lineDirection = (endPos - startPos).normalized;
        Vector3 toPoint = point - startPos;
        float projectionLength = Vector3.Dot(toPoint, lineDirection);

        // Clamp to line segment
        float lineLength = Vector3.Distance(startPos, endPos);
        projectionLength = Mathf.Clamp(projectionLength, 0, lineLength);

        return startPos + lineDirection * projectionLength;
    }

    /// <summary>
    /// Get the closest point on the line to a ray (when the ray doesn't intersect anything)
    /// </summary>
    private Vector3 GetClosestPointOnLineToRay(Vector3 rayOrigin, Vector3 rayDirection)
    {
        Vector3 startPos = startPoint.position;
        Vector3 endPos = endPoint.position;
        Vector3 lineDirection = (endPos - startPos).normalized;

        // Find the closest points between the ray and the line
        Vector3 toStart = startPos - rayOrigin;
        float rayDot = Vector3.Dot(rayDirection, rayDirection);
        float lineDot = Vector3.Dot(lineDirection, lineDirection);
        float rayLineDot = Vector3.Dot(rayDirection, lineDirection);

        float denominator = rayDot * lineDot - rayLineDot * rayLineDot;

        if (Mathf.Abs(denominator) < 0.0001f)
        {
            // Lines are parallel, just use the start point
            return GetClosestPointOnLine(rayOrigin);
        }

        float t = (Vector3.Dot(toStart, lineDirection) * rayLineDot - Vector3.Dot(toStart, rayDirection) * lineDot) / denominator;

        // Get point on line
        float lineT = (Vector3.Dot(toStart, rayDirection) + t * rayLineDot) / lineDot;
        float lineLength = Vector3.Distance(startPos, endPos);
        lineT = Mathf.Clamp(lineT, 0, lineLength);

        return startPos + lineDirection * lineT;
    }

    /// <summary>
    /// Generates a MeshCollider along the line which will be used by XRSimpleInteractable for hover and selection.
    /// Supports deadzones at the extremities where no collider will be generated.
    /// </summary>
    public void GenerateMeshCollider()
    {
        _meshCollider = GetComponent<MeshCollider>();

        if (!_meshCollider)
        {
            _meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        _meshCollider.enabled = true;

        // Store original line positions
        Vector3 originalStart = _lineRenderer.GetPosition(0);
        Vector3 originalEnd = _lineRenderer.GetPosition(1);

        // Calculate line properties
        Vector3 lineDirection = (originalEnd - originalStart);
        float totalLength = lineDirection.magnitude;
        Vector3 normalizedDirection = lineDirection.normalized;

        // Ensure deadzones don't exceed total line length
        float totalDeadzoneDistance = startDeadzoneDistance + endDeadzoneDistance;
        if (totalDeadzoneDistance >= totalLength)
        {
            Debug.LogWarning($"[LineController] Total deadzone distance ({totalDeadzoneDistance:F3}) >= line length ({totalLength:F3}). Adjusting deadzones.");
            float scale = (totalLength * 0.9f) / totalDeadzoneDistance;
            startDeadzoneDistance *= scale;
            endDeadzoneDistance *= scale;
            Debug.LogWarning($"[LineController] Adjusted deadzones: start={startDeadzoneDistance:F3}, end={endDeadzoneDistance:F3}");
        }

        // Calculate new start and end positions excluding deadzones
        Vector3 colliderStart = originalStart + normalizedDirection * startDeadzoneDistance;
        Vector3 colliderEnd = originalEnd - normalizedDirection * endDeadzoneDistance;

        // Temporarily modify line renderer positions for mesh generation
        _lineRenderer.SetPosition(0, colliderStart);
        _lineRenderer.SetPosition(1, colliderEnd);

        // Generate mesh with the modified positions
        Mesh mesh = new Mesh();
        _lineRenderer.BakeMesh(mesh, true);

        Debug.Log($"[LineController] Generated mesh has {mesh.vertexCount} vertices and {mesh.triangles.Length / 3} triangles");
        Debug.Log($"[LineController] Deadzone distances: start={startDeadzoneDistance:F3}u, end={endDeadzoneDistance:F3}u");
        Debug.Log($"[LineController] Collider length: {Vector3.Distance(colliderStart, colliderEnd):F3}u / {totalLength:F3}u total");

        // Restore original line positions
        _lineRenderer.SetPosition(0, originalStart);
        _lineRenderer.SetPosition(1, originalEnd);

        // Check if mesh is valid
        if (mesh.vertexCount == 0)
        {
            Debug.LogError("[LineController] Generated mesh has no vertices! Check LineRenderer settings or deadzone values.");
            return;
        }

        // if you need collisions on both sides of the line, simply duplicate & flip facing the other direction!
        // This can be optimized to improve performance ;)
        int[] meshIndices = mesh.GetIndices(0);
        int[] newIndices = new int[meshIndices.Length * 2];

        int j = meshIndices.Length - 1;
        for (int i = 0; i < meshIndices.Length; i++)
        {
            newIndices[i] = meshIndices[i];
            newIndices[meshIndices.Length + i] = meshIndices[j];
            j--;
        }
        mesh.SetIndices(newIndices, MeshTopology.Triangles, 0);

        _meshCollider.sharedMesh = mesh;

        Debug.Log($"[LineController] MeshCollider generated successfully with deadzones. Bounds: {_meshCollider.bounds}");
        float colliderLength = Vector3.Distance(colliderStart, colliderEnd);
        Debug.Log($"[LineController] Collider covers {colliderLength:F3}u of {totalLength:F3}u total line length");
    }


    /// <summary>
    /// Deactivates the MeshCollider component: used to disable interaction temporarily when moving the note associated with the link since otherwise it interferes with the movement.
    /// </summary>
    public void DeactivateMeshCollider()
    {
        if (_meshCollider)
        {
            _meshCollider.enabled = false;
        }
    }


    /// <summary>
    /// Used to highlight the line when the user hovers over it to indicate it is interactable.
    /// </summary>
    /// <param name="status"></param>
    private void Highlight(bool status)
    {
        Color targetColor = status ? highlightColor : normalColor;
        _lineRenderer.startColor = targetColor;
        _lineRenderer.endColor = targetColor;
    }

}
