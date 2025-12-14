using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class FadeController : MonoBehaviour
{
    private static FadeController instance;
    public static FadeController Instance => instance;
    
    [SerializeField] private Canvas fadeCanvas;
    [SerializeField] private Image fadeImage;
    [SerializeField] private Color fadeColor = Color.black;
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        SetupFadeCanvas();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        StartCoroutine(FadeIn(0.5f));
    }
    
    void SetupFadeCanvas()
    {
        if (fadeCanvas == null)
        {
            GameObject canvasGO = new GameObject("FadeCanvas");
            canvasGO.transform.SetParent(transform);
            
            fadeCanvas = canvasGO.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999;
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        if (fadeImage == null)
        {
            GameObject imageGO = new GameObject("FadeImage");
            imageGO.transform.SetParent(fadeCanvas.transform, false);
            
            fadeImage = imageGO.AddComponent<Image>();
            fadeImage.color = fadeColor;
            
            RectTransform rt = fadeImage.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }
        

        Color c = fadeImage.color;
        c.a = 0f;
        fadeImage.color = c;
        fadeCanvas.gameObject.SetActive(false);
    }
    
    public IEnumerator FadeOut(float duration)
    {
        fadeCanvas.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(0f, 1f, duration));
    }
    
    public IEnumerator FadeIn(float duration)
    {
        fadeCanvas.gameObject.SetActive(true);
        yield return StartCoroutine(Fade(1f, 0f, duration));
        fadeCanvas.gameObject.SetActive(false);
    }
    
    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color c = fadeImage.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            c.a = alpha;
            fadeImage.color = c;
            yield return null;
        }
        
        c.a = endAlpha;
        fadeImage.color = c;
    }
}