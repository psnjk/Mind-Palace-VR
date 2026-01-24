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
    [SerializeField] private XRFade xrFade;

    // only used for saving when going back to main hub from a fresh room
    public string initialSaveName = null;
    
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
        SetupXRFade();
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

    void SetupXRFade()
    {
        if (xrFade == null)
        {
            xrFade = FindFirstObjectByType<XRFade>();
            if (xrFade == null)
            {
                Debug.LogWarning("Portal: No XRFade component found in scene. Fade transitions will be disabled.");
                useFadeTransition = false;
            }
        }
    }
    


    /*
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
    }*/

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") || other.gameObject.GetComponent<CharacterController>() != null)
        {
            if (!isLoading)
            {
                // Only if going from a room back to the main hub, save the room.
                if (portalType == PortalType.DefaultScene && defaultSceneName == "MainHub")
                {
                    // Check if we have a saveId set on portal, or if SaveManager has a current save ID
                    string idToUse = !string.IsNullOrEmpty(saveId) ? saveId : SaveManager.Instance.GetCurrentSaveId();
                    
                    if (string.IsNullOrEmpty(idToUse))
                    {
                        Debug.Log($"Portal: No save ID found. Creating new save for {gameObject.name}.");
                        SaveManager.Instance.SaveCurrentRoomAsExperience(null, initialSaveName);
                    }
                    else
                    {
                        Debug.Log($"Portal: Updating existing save ID {idToUse} via portal {gameObject.name}.");
                        SaveManager.Instance.SaveCurrentRoomAsExperience(idToUse, null);
                    }
                }
                
                if (useFadeTransition && xrFade != null)
                {
                    isLoading = true;
                    xrFade.OnFadeOutComplete += OnFadeComplete;
                    xrFade.FadeOut();
                }
                else
                {
                    LoadScene();
                }
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
    
    private void OnFadeComplete()
    {
        if (xrFade != null)
        {
            xrFade.OnFadeOutComplete -= OnFadeComplete;
        }
        
        // Load scene directly without additional fade
        if (portalType == PortalType.DefaultScene)
        {
            if (!string.IsNullOrEmpty(defaultSceneName))
            {
                SaveManager.Instance.LoadScene(defaultSceneName);
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(saveId))
            {
                SaveManager.Instance.LoadExperience(saveId);
            }
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
        if (xrFade != null)
        {
            bool fadeComplete = false;
            System.Action onComplete = () => fadeComplete = true;
            xrFade.OnFadeOutComplete += onComplete;
            xrFade.FadeOut();
            
            while (!fadeComplete)
            {
                yield return null;
            }
            
            xrFade.OnFadeOutComplete -= onComplete;
        }
        else
        {
            yield return new WaitForSeconds(fadeDuration);
        }
        
        SaveManager.Instance.LoadScene(sceneName);
    }

    private IEnumerator FadeAndLoadExperience(string saveId)
    {
        if (xrFade != null)
        {
            bool fadeComplete = false;
            System.Action onComplete = () => fadeComplete = true;
            xrFade.OnFadeOutComplete += onComplete;
            xrFade.FadeOut();
            
            while (!fadeComplete)
            {
                yield return null;
            }
            
            xrFade.OnFadeOutComplete -= onComplete;
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