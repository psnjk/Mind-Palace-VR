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

    [Header("Follow Options")]
    [Tooltip("Make the object follow the camera/user around")]
    public bool followCamera = false;

    [Tooltip("Distance to maintain from the camera")]
    [Range(0.5f, 10f)]
    public float followDistance = 2f;

    [Tooltip("Height offset from camera (useful for UI positioning)")]
    public float heightOffset = 0f;

    [Tooltip("Smooth the movement over time")]
    public bool smoothMovement = true;

    [Tooltip("Speed of movement smoothing")]
    [Range(1f, 20f)]
    public float movementSpeed = 5f;

    [Tooltip("Only follow on Y axis (useful to prevent canvas from moving up/down)")]
    public bool followOnlyHorizontally = true;

    [Header("Advanced Follow Settings")]
    [Tooltip("Use initial offset positioning instead of forward-based following")]
    public bool useInitialOffset = false;

    [Tooltip("Local offset from camera (applied in camera's local space for responsive following)")]
    public Vector3 localOffset = new Vector3(0, 0, 2f);

    [Header("Smart Follow Settings")]
    [Tooltip("Enable smart following that only moves when looking away from the canvas")]
    public bool useSmartFollow = true;

    [Tooltip("Angle threshold before canvas starts following (degrees). Larger values = more tolerance for looking around")]
    [Range(5f, 90f)]
    public float followThresholdAngle = 30f;

    [Tooltip("Additional angle beyond threshold before maximum follow speed (degrees)")]
    [Range(5f, 45f)]
    public float followRampAngle = 20f;

    [Tooltip("Delay before starting to follow when looking away (seconds)")]
    [Range(0f, 2f)]
    public float followDelay = 0.3f;

    private Transform _cameraTransform;
    private Vector3 _initialOffset;
    private bool _hasInitialOffset = false;

    // Smart follow variables
    private Vector3 _lastFollowPosition;
    private float _timeOutsideThreshold = 0f;
    private bool _isFollowingActive = false;

    void Start()
    {
        if (targetCamera == null)
        {
            // Look for XR Origin camera or any camera tagged as "MainCamera" (the one in XR rig) 
            GameObject cameraObject = GameObject.FindGameObjectWithTag("MainCamera");
            if (cameraObject != null)
            {
                targetCamera = cameraObject.GetComponent<Camera>();
            }
        }

        if (targetCamera != null)
        {
            _cameraTransform = targetCamera.transform;

            // Calculate initial offset if follow is enabled
            if (followCamera && !_hasInitialOffset)
            {
                _initialOffset = transform.position - _cameraTransform.position;
                _hasInitialOffset = true;
            }

            // Initialize smart follow position
            _lastFollowPosition = transform.position;
        }
        else
        {
            Debug.LogWarning("LookAtCamera: No camera found! Make sure your camera is tagged as 'MainCamera' or assign it manually.");
        }
    }

    void Update()
    {
        if (!_cameraTransform) return;

        // Handle following behavior first
        if (followCamera)
        {
            if (useSmartFollow)
            {
                UpdateSmartFollowPosition();
            }
            else
            {
                UpdateFollowPosition();
            }
        }

        // Then handle look-at behavior
        UpdateLookAtRotation();
    }

    private void UpdateSmartFollowPosition()
    {
        // Calculate where the canvas should be based on current follow settings
        Vector3 idealPosition = CalculateIdealPosition();

        // Calculate the angle between camera forward and direction to canvas
        Vector3 directionToCanvas = (transform.position - _cameraTransform.position).normalized;
        Vector3 cameraForward = _cameraTransform.forward;

        if (followOnlyHorizontally)
        {
            directionToCanvas.y = 0;
            cameraForward.y = 0;
            directionToCanvas.Normalize();
            cameraForward.Normalize();
        }

        float angleToCanvas = Vector3.Angle(cameraForward, directionToCanvas);

        // Check if we're outside the threshold
        if (angleToCanvas > followThresholdAngle)
        {
            _timeOutsideThreshold += Time.deltaTime;

            // Start following after delay
            if (_timeOutsideThreshold >= followDelay)
            {
                _isFollowingActive = true;

                // Calculate follow strength based on how far outside threshold we are
                float excessAngle = angleToCanvas - followThresholdAngle;
                float followStrength = Mathf.Clamp01(excessAngle / followRampAngle);

                // Apply stronger movement the further we look away
                float dynamicSpeed = movementSpeed * (1f + followStrength * 2f);

                Vector3 targetPosition = Vector3.Lerp(_lastFollowPosition, idealPosition, followStrength);

                if (smoothMovement)
                {
                    transform.position = Vector3.Lerp(transform.position, targetPosition, dynamicSpeed * Time.deltaTime);
                }
                else
                {
                    transform.position = targetPosition;
                }

                _lastFollowPosition = transform.position;
            }
        }
        else
        {
            // Reset timer when back in comfort zone
            _timeOutsideThreshold = 0f;
            _isFollowingActive = false;
        }
    }

    private Vector3 CalculateIdealPosition()
    {
        Vector3 targetPosition;

        if (useInitialOffset && _hasInitialOffset)
        {
            // Use the initial offset as a base (old behavior)
            targetPosition = _cameraTransform.position + _initialOffset;
        }
        else
        {
            // New responsive following using local offset
            Vector3 worldOffset = _cameraTransform.TransformPoint(localOffset) - _cameraTransform.position;

            if (followOnlyHorizontally)
            {
                // Keep the Y component from the height offset setting
                worldOffset.y = 0;
                targetPosition = _cameraTransform.position + worldOffset;
                targetPosition.y = _cameraTransform.position.y + heightOffset;
            }
            else
            {
                targetPosition = _cameraTransform.position + worldOffset;
                targetPosition.y += heightOffset;
            }
        }

        return targetPosition;
    }

    private void UpdateFollowPosition()
    {
        Vector3 targetPosition = CalculateIdealPosition();

        // Apply position
        if (smoothMovement)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, movementSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    private void UpdateLookAtRotation()
    {
        // Calculate direction FROM camera TO canvas (so canvas faces the camera)
        Vector3 directionFromCamera = transform.position - _cameraTransform.position;

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

    [ContextMenu("Reset Initial Offset")]
    public void ResetInitialOffset()
    {
        if (_cameraTransform)
        {
            _initialOffset = transform.position - _cameraTransform.position;
            _hasInitialOffset = true;
        }
    }

    [ContextMenu("Set Local Offset from Current Position")]
    public void SetLocalOffsetFromCurrentPosition()
    {
        if (_cameraTransform)
        {
            localOffset = _cameraTransform.InverseTransformPoint(transform.position);
        }
    }
}
