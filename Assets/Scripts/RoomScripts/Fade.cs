using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class XRFade : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 0.5f;

    public event Action OnFadeOutComplete;
    public event Action OnFadeInComplete;

    void Awake()
    {
        SetupCanvas();
        SetAlpha(1);
    }

    void SetupCanvas()
    {
        if (fadeImage == null)
        {
            Transform background = transform.Find("Background");
            if (background != null)
            {
                fadeImage = background.GetComponent<Image>();
                if (fadeImage == null)
                {
                    Debug.LogError("Background found but no Image component attached!");
                }
            }
            else
            {
                Debug.LogError("No Background child found on canvas!");
            }
        }
    }

    void Start()
    {
        FadeIn();
    }

    public void FadeIn()
    {
        StartCoroutine(Fade(1, 0, () => OnFadeInComplete?.Invoke()));
    }

    public void FadeOut()
    {
        StartCoroutine(Fade(0, 1, () => OnFadeOutComplete?.Invoke()));
    }

    IEnumerator Fade(float from, float to, Action onComplete)
    {
        float t = 0;
        Color c = fadeImage.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }

        SetAlpha(to);
        onComplete?.Invoke();
    }

    void SetAlpha(float a)
    {
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}
