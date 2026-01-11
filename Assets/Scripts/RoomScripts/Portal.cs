using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Portal : MonoBehaviour
{
    [Header("Portal Configuration")]
    [SerializeField] private PortalType portalType;
    [SerializeField] private string defaultSceneName; // For default portals
    [SerializeField] private string saveId; // For save portals
    
    [Header("Visual Feedback")]
    [SerializeField] private Material portalActiveMaterial;
    [SerializeField] private Material portalInactiveMaterial;
    [SerializeField] private Renderer portalRenderer;
    
    [Header("Fade Settings")]
    [SerializeField] private bool useFadeTransition = true;
    [SerializeField] private float fadeDuration = 0.5f;
    
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;
    private bool isPlayerNear;
    private bool isLoading;
    
    public enum PortalType
    {
        DefaultScene,
        SavedExperience
    }

    public void SetSaveId(string saveId)
    {
        this.saveId = saveId;
    }
    
    void Start()
    {
        SetupInteractable();
        
        if (portalRenderer != null && portalInactiveMaterial != null)
        {
            portalRenderer.material = portalInactiveMaterial;
        }
    }
    
    void SetupInteractable()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
        {
            interactable = gameObject.AddComponent<XRSimpleInteractable>();
        }
        
        interactable.selectEntered.AddListener(OnPortalSelected);
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
    }
    


    private void OnCollisionEnter(Collision other)
    {
        // TODO FIX: DOES NOT DETECT PLAYER COLLISION, DETECTS CUBES FOR SOME REASON
        Debug.Log(other.gameObject.tag);
        if (other.gameObject.CompareTag("Player") || other.gameObject.GetComponent<CharacterController>() != null)
        {
            if (!isLoading)
            {
                LoadScene();
            }
        }
    }
    
    private void OnPortalSelected(SelectEnterEventArgs args)
    {
        if (!isLoading)
        {
            LoadScene();
        }
    }
    
    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        isPlayerNear = true;
        if (portalRenderer != null && portalActiveMaterial != null)
        {
            portalRenderer.material = portalActiveMaterial;
        }
    }
    
    private void OnHoverExit(HoverExitEventArgs args)
    {
        isPlayerNear = false;
        if (portalRenderer != null && portalInactiveMaterial != null)
        {
            portalRenderer.material = portalInactiveMaterial;
        }
    }
    
    private void LoadScene()
    {
        isLoading = true;
        
        if (portalType == PortalType.DefaultScene)
        {
            if (string.IsNullOrEmpty(defaultSceneName))
            {
                Debug.LogError($"Portal: No default scene specified for portal {gameObject.name}");
                isLoading = false;
                return;
            }

            if (useFadeTransition)
            {
                StartCoroutine(FadeAndLoadDefault(defaultSceneName));
            }
            else
            {
                SaveManager.Instance.LoadScene(defaultSceneName);
            }
        }
        else // SavedExperience
        {
            if (string.IsNullOrEmpty(saveId))
            {
                Debug.LogError($"Portal: No save ID specified for portal {gameObject.name}");
                isLoading = false;
                return;
            }

            if (useFadeTransition)
            {
                StartCoroutine(FadeAndLoadExperience(saveId));
            }
            else
            {
                SaveManager.Instance.LoadExperience(saveId);
            }
        }
    }
    
    private IEnumerator FadeAndLoadDefault(string sceneName)
    {
        if (FadeController.Instance != null)
        {
            yield return StartCoroutine(FadeController.Instance.FadeOut(fadeDuration));
        }
        else
        {
            yield return new WaitForSeconds(fadeDuration);
        }
        
        SaveManager.Instance.LoadScene(sceneName);
    }

    private IEnumerator FadeAndLoadExperience(string saveId)
    {
        if (FadeController.Instance != null)
        {
            yield return StartCoroutine(FadeController.Instance.FadeOut(fadeDuration));
        }
        else
        {
            yield return new WaitForSeconds(fadeDuration);
        }
        
        SaveManager.Instance.LoadExperience(saveId);
    }
    
    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnPortalSelected);
            interactable.hoverEntered.RemoveListener(OnHoverEnter);
            interactable.hoverExited.RemoveListener(OnHoverExit);
        }
    }
}