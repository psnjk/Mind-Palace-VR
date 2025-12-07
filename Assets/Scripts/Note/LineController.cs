using UnityEngine;

public class LineController : MonoBehaviour
{

    private LineRenderer _lineRenderer;

    public Transform startPoint;
    public Transform endPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupLineRenderer();
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

    }


    // Update is called once per frame
    void Update()
    {
        if (!startPoint || !endPoint) return;
        // TODO: optimize it to only set positions when startPoint or endPoint move
        _lineRenderer.SetPosition(0, startPoint.position);
        _lineRenderer.SetPosition(1, endPoint.position);

    }
}
