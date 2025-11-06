using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("The camera to look at. If null, will try to find Camera.main")]
    public Camera targetCamera;
    
    [Header("Look At Options")]
    [Tooltip("Whether to look at the camera on all axes or only Y axis (for UI elements)")]
    public bool lookAtOnAllAxes = false;
    
    [Tooltip("Smooth the rotation over time")]
    public bool smoothRotation = true;
    
    [Tooltip("Speed of rotation smoothing")]
    [Range(1f, 20f)]
    public float rotationSpeed = 10f;
    
    [Tooltip("Offset the look direction (useful for adjusting canvas orientation)")]
    public Vector3 lookOffset = Vector3.zero;

    private Transform cameraTransform;

    void Start()
    {        
        if (targetCamera == null)
        {
            // Look for XR Origin camera or any camera tagged as "Main Camera" (the one in XR rig) 
            GameObject cameraObject = GameObject.FindGameObjectWithTag("Main Camera");
            if (cameraObject != null)
            {
                targetCamera = cameraObject.GetComponent<Camera>();
            }
        }
        
        if (targetCamera != null)
        {
            cameraTransform = targetCamera.transform;
        }
        else
        {
            Debug.LogWarning("LookAtCamera: No camera found! Make sure your camera is tagged as 'Main Camera' or assign it manually.");
        }
    }

    void Update()
    {
        if (cameraTransform == null) return;
        
        // Calculate direction FROM camera TO canvas (so canvas faces the camera)
        Vector3 directionFromCamera = transform.position - cameraTransform.position;
        
        // Apply look offset if specified
        directionFromCamera += lookOffset;
        
        Quaternion targetRotation;
        
        if (lookAtOnAllAxes)
        {
            // Look at camera on all axes
            targetRotation = Quaternion.LookRotation(directionFromCamera);
        }
        else
        {
            // Lock the other axes and only look at camera on Y axis
            directionFromCamera.y = 0;
            targetRotation = Quaternion.LookRotation(directionFromCamera);
        }
        
        // Apply rotation
        if (smoothRotation)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            transform.rotation = targetRotation;
        }
    }
}
