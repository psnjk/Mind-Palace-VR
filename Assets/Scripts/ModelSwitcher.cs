using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ModelSwitcher : MonoBehaviour
{
    [Header("Model Prefabs")]
    [Tooltip("List of model prefabs to cycle through")]
    public GameObject[] modelPrefabs;
    
    [Header("XR Input")]
    [Tooltip("Input action reference for switching models")]
    public InputActionProperty switchAction;
    
    
    [SerializeField] 
    [Tooltip("Initial model index")]
    private int currentModelIndex = 0;
    
    private GameObject _currentModelInstance;
    private MeshCollider _meshCollider;
    private XRBaseInteractable _interactable;
    private bool _isHovered = false;
    private bool _buttonWasPressed = false;
    private ParticleSystem _poofParticleSystem;
    private Rigidbody _rigidbody;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }

    private void OnValidate()
    {
        currentModelIndex = Mathf.Clamp(currentModelIndex, 0, modelPrefabs.Length-1);
    }

    void Start()
    {

        _meshCollider = GetComponent<MeshCollider>();
        _interactable = GetComponent<XRBaseInteractable>();
        _poofParticleSystem = GetComponent<ParticleSystem>();
        _rigidbody = GetComponent<Rigidbody>();
        
        if (_meshCollider == null)
        {
            Debug.LogError("ModelSwitcher requires a MeshCollider component!");
            return;
        }
        
        if (modelPrefabs == null || modelPrefabs.Length == 0)
        {
            Debug.LogError("ModelSwitcher requires at least one model prefab!");
            return;
        }
        
        // Subscribe to hover events
        if (_interactable != null)
        {
            _interactable.hoverEntered.AddListener(OnHoverEntered);
            _interactable.hoverExited.AddListener(OnHoverExited);
        }
        

        SwitchToModel(currentModelIndex);
    }
    
    void OnDestroy()
    {
        if (_interactable != null)
        {
            _interactable.hoverEntered.RemoveListener(OnHoverEntered);
            _interactable.hoverExited.RemoveListener(OnHoverExited);
        }
    }
    
    void OnHoverEntered(HoverEnterEventArgs args)
    {
        _isHovered = true;
    }
    
    void OnHoverExited(HoverExitEventArgs args)
    {
        _isHovered = false;
    }
    
    void Update()
    {
        // Check if object is hovered and button is pressed
        if (_isHovered && switchAction.action != null)
        {
            bool isPressed = switchAction.action.ReadValue<float>() > 0.5f;
            
            // Detect button press (was not pressed, now is pressed)
            if (isPressed && !_buttonWasPressed)
            {
                SwitchToNextModel();
            }
            
            _buttonWasPressed = isPressed;
        }
        else
        {
            _buttonWasPressed = false;
        }
    }
    
    void SwitchToNextModel()
    {
        currentModelIndex = (currentModelIndex + 1) % modelPrefabs.Length;
        _poofParticleSystem.Play();
        _rigidbody.MovePosition(transform.position + Vector3.up * 0.5f);
        SwitchToModel(currentModelIndex);
    }
    
    void SwitchToModel(int index)
    {
        // Destroy old model instance
        if (_currentModelInstance != null)
        {
            Destroy(_currentModelInstance);
        }
        
        // Instantiate new model as child
        _currentModelInstance = Instantiate(modelPrefabs[index], transform);
        _currentModelInstance.transform.localPosition = Vector3.zero;
        _currentModelInstance.transform.localRotation = Quaternion.identity;
        _currentModelInstance.transform.localScale = Vector3.one;
        
        // Update mesh collider
        MeshFilter meshFilter = _currentModelInstance.GetComponent<MeshFilter>();
        if (meshFilter && meshFilter.sharedMesh)
        {
            _meshCollider.sharedMesh = meshFilter.sharedMesh;
        }
        else
        {
            Debug.LogWarning($"Model prefab at index {index} doesn't have a MeshFilter or mesh!");
        }
    }
}