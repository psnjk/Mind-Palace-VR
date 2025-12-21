using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Canvas))]
public class UIScaleOut : MonoBehaviour
{
    RectTransform rect;
    CanvasGroup canvasGroup;
    Canvas canvas;

    Vector3 originalScale;

    [Header("Animation")]
    public float showDuration = 0.25f;
    public float hideDuration = 0.2f;

    [Tooltip("Scale value when hidden")]
    public float hiddenScale = 0.08f;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponent<Canvas>();

        // Save the scale set in the scene
        originalScale = rect.localScale;

        // Start hidden
        rect.localScale = Vector3.one * hiddenScale;
        canvasGroup.blocksRaycasts = false;
        canvas.sortingOrder = -1;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();

        rect.localScale = Vector3.one * hiddenScale;
        canvasGroup.blocksRaycasts = false;
        canvas.sortingOrder = -1;

        StartCoroutine(ScaleIn());
    }

    public void Hide()
    {
        StopAllCoroutines();
        canvasGroup.blocksRaycasts = false;
        canvas.sortingOrder = -1;

        StartCoroutine(ScaleOut());
    }

    IEnumerator ScaleIn()
    {
        float t = 0f;

        while (t < showDuration)
        {
            t += Time.deltaTime;
            float p = EaseOutBack(t / showDuration);

            rect.localScale = Vector3.LerpUnclamped(
                Vector3.one * hiddenScale,
                originalScale,
                p
            );

            yield return null;
        }

        rect.localScale = originalScale;
        canvasGroup.blocksRaycasts = true;
        canvas.sortingOrder = 1;
    }

    IEnumerator ScaleOut()
    {
        float t = 0f;

        while (t < hideDuration)
        {
            t += Time.deltaTime;
            float p = EaseInCubic(t / hideDuration);

            rect.localScale = Vector3.Lerp(
                originalScale,
                Vector3.one * hiddenScale,
                p
            );

            yield return null;
        }

        rect.localScale = Vector3.one * hiddenScale;
        gameObject.SetActive(false);
    }

    float EaseOutBack(float x)
    {
        x = Mathf.Clamp01(x);
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    float EaseInCubic(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * x;
    }
}
